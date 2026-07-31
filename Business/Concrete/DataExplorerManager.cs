using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;
using Entities.Dtos.Data;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Text;
using System.Text.Json;

namespace Business.Concrete
{
    // Bazanın birbaşa idarəsi. Burada repozitoriya işləmir: cədvəl əvvəlcədən məlum
    // deyil, ona görə EF modelinin metadatası oxunub sorğu dinamik qurulur.
    // TƏHLÜKƏSİZLİK: cədvəl və sütun adları yalnız modeldən götürülür, istifadəçinin
    // yazdığı ad birbaşa sorğuya düşmür; dəyərlər isə həmişə parametrlə gedir.
    public class DataExplorerManager : IDataExplorerService
    {
        private readonly SadliqDbContext _context;
        private readonly IAuditContext _audit;

        public DataExplorerManager(SadliqDbContext context, IAuditContext audit)
        {
            _context = context;
            _audit = audit;
        }

        // Parol hashları heç vaxt paneldə görünməməli və dəyişdirilməməlidir
        private static readonly HashSet<string> HiddenColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            "PasswordHash", "PasswordSalt"
        };

        // Cədvəllərin oxunaqlı adları
        private static readonly Dictionary<string, string> Labels = new()
        {
            ["Users"] = "İstifadəçilər",
            ["Venues"] = "Saraylar",
            ["VenueImages"] = "Saray şəkilləri",
            ["VenueFeatures"] = "Saray özəllikləri",
            ["Halls"] = "Zallar",
            ["HallMedia"] = "Zal şəkilləri",
            ["HallAvailabilities"] = "Zal təqvimi",
            ["Menus"] = "Menyular",
            ["MenuItems"] = "Menyu tərkibi",
            ["Reservations"] = "Rezervlər",
            ["Reviews"] = "Rəylər",
            ["ReviewReports"] = "Rəy şikayətləri",
            ["Favorites"] = "Seçilmişlər",
            ["VenueStaff"] = "Saray administratorları",
            ["VenueStaffHalls"] = "Administrator zalları",
            ["AuditLogs"] = "Fəaliyyət jurnalı",
            ["PageViews"] = "Ziyarət qeydləri",
            ["PasswordResetTokens"] = "Parol bərpa açarları",
            ["PlatformSettings"] = "Platforma parametrləri",
            ["ReviewEdits"] = "Rəy düzəlişləri"
        };

        public async Task<IDataResult<List<TableInfoDto>>> GetTablesAsync(UserRole viewerRole)
        {
            var list = new List<TableInfoDto>();

            foreach (var table in TableNames())
            {
                if (IsRestricted(table, viewerRole)) continue;

                var count = await ScalarIntAsync($"SELECT COUNT(*) FROM \"{table}\"");
                list.Add(new TableInfoDto
                {
                    Name = table,
                    Label = Labels.TryGetValue(table, out var label) ? label : table,
                    RowCount = count
                });
            }

            return new SuccessDataResult<List<TableInfoDto>>(list.OrderBy(t => t.Label).ToList());
        }

        public async Task<IDataResult<TableRowsDto>> GetRowsAsync(
            string table, string? q, int page, int pageSize, string? sort, bool desc, bool includeDeleted,
            UserRole viewerRole)
        {
            var meta = FindTable(table, viewerRole);
            if (meta is null)
                return new ErrorDataResult<TableRowsDto>("Belə cədvəl yoxdur.");

            var columns = Columns(meta);
            var key = KeyColumn(meta);

            page = page < 1 ? 1 : page;
            pageSize = pageSize is < 1 or > 200 ? 25 : pageSize;

            var where = new StringBuilder();
            var parameters = new List<object?>();
            var conditions = new List<string>();

            // Axtarış yalnız mətn sütunlarında aparılır
            if (!string.IsNullOrWhiteSpace(q))
            {
                var textColumns = columns.Where(c => c.Type == "text").Select(c => c.Name).ToList();
                if (textColumns.Count > 0)
                {
                    var parts = textColumns.Select(c => $"\"{c}\"::text ILIKE @p{parameters.Count}");
                    conditions.Add($"({string.Join(" OR ", parts)})");
                    parameters.Add($"%{q.Trim()}%");
                }
            }

            // Silinmiş sətirlər ayrıca istənilməsə göstərilmir
            if (!includeDeleted && columns.Any(c => c.Name == "IsDeleted"))
                conditions.Add("\"IsDeleted\" = false");

            if (conditions.Count > 0)
                where.Append(" WHERE ").Append(string.Join(" AND ", conditions));

            var total = await ScalarIntAsync($"SELECT COUNT(*) FROM \"{meta.Name}\"{where}", parameters);

            var sortColumn = columns.Any(c => c.Name == sort) ? sort! : key;
            var direction = desc ? "DESC" : "ASC";

            var selectList = string.Join(", ", columns.Select(c => $"\"{c.Name}\""));
            var sql = $"SELECT {selectList} FROM \"{meta.Name}\"{where} " +
                      $"ORDER BY \"{sortColumn}\" {direction} " +
                      $"LIMIT {pageSize} OFFSET {(page - 1) * pageSize}";

            var rows = await QueryAsync(sql, parameters, columns);

            return new SuccessDataResult<TableRowsDto>(new TableRowsDto
            {
                Table = meta.Name,
                KeyColumn = key,
                Columns = columns,
                Rows = rows,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            });
        }

        public async Task<IResult> CreateRowAsync(string table, TableRowDto dto, UserRole viewerRole)
        {
            var meta = FindTable(table, viewerRole);
            if (meta is null)
                return new ErrorResult("Belə cədvəl yoxdur.");

            var columns = Columns(meta);

            // Baza özü doldurduğu sütunlar (Id) əl ilə verilmir
            var writable = columns.Where(c => !c.IsGenerated).ToList();
            var used = writable.Where(c => dto.Values.ContainsKey(c.Name)).ToList();

            if (used.Count == 0)
                return new ErrorResult("Heç bir sahə doldurulmayıb.");

            var missing = writable
                .Where(c => c.IsRequired && !dto.Values.ContainsKey(c.Name))
                .Select(c => c.Name)
                .ToList();

            if (missing.Count > 0)
                return new ErrorResult($"Bu sahələr məcburidir: {string.Join(", ", missing)}");

            var names = string.Join(", ", used.Select(c => $"\"{c.Name}\""));
            var holders = string.Join(", ", used.Select((_, i) => $"@p{i}"));
            var values = used.Select(c => Convert(dto.Values[c.Name], c)).ToList();

            try
            {
                await ExecuteAsync($"INSERT INTO \"{meta.Name}\" ({names}) VALUES ({holders})", values);
            }
            catch (DbException ex)
            {
                return new ErrorResult(Explain(ex));
            }

            _audit.Set("DataRowCreated", $"«{Label(meta.Name)}» cədvəlinə yeni sətir əlavə etdi", meta.Name);
            return new SuccessResult("Sətir əlavə olundu.");
        }

        public async Task<IResult> UpdateRowAsync(string table, string id, TableRowDto dto, UserRole viewerRole)
        {
            var meta = FindTable(table, viewerRole);
            if (meta is null)
                return new ErrorResult("Belə cədvəl yoxdur.");

            var columns = Columns(meta);
            var key = KeyColumn(meta);
            var keyColumn = columns.First(c => c.Name == key);

            // Açar sütunu dəyişdirilmir — sətirin şəxsiyyətidir
            var used = columns
                .Where(c => c.Name != key && !c.IsGenerated && dto.Values.ContainsKey(c.Name))
                .ToList();

            if (used.Count == 0)
                return new ErrorResult("Dəyişiklik yoxdur.");

            var sets = string.Join(", ", used.Select((c, i) => $"\"{c.Name}\" = @p{i}"));
            var values = used.Select(c => Convert(dto.Values[c.Name], c)).ToList();
            values.Add(Convert(id, keyColumn));

            int affected;
            try
            {
                affected = await ExecuteAsync(
                    $"UPDATE \"{meta.Name}\" SET {sets} WHERE \"{key}\" = @p{used.Count}", values);
            }
            catch (DbException ex)
            {
                return new ErrorResult(Explain(ex));
            }

            if (affected == 0)
                return new ErrorResult("Sətir tapılmadı.");

            _audit.Set("DataRowUpdated", $"«{Label(meta.Name)}» cədvəlində sətri dəyişdi", meta.Name);
            return new SuccessResult("Sətir yeniləndi.");
        }

        // Silmək = IsDeleted işarəsi qoymaq. Sətir bazada qalır, saytda görünmür və
        // geri qaytarıla bilir. Yalnız bu sütunu olmayan köməkçi cədvəllərdə
        // (əlaqə cədvəlləri, jurnal) sətir həqiqətən silinir.
        public async Task<IResult> DeleteRowAsync(string table, string id, UserRole viewerRole)
            => await SetDeletedAsync(table, id, true, viewerRole);

        public async Task<IResult> RestoreRowAsync(string table, string id, UserRole viewerRole)
            => await SetDeletedAsync(table, id, false, viewerRole);

        private async Task<IResult> SetDeletedAsync(string table, string id, bool deleted, UserRole viewerRole)
        {
            var meta = FindTable(table, viewerRole);
            if (meta is null)
                return new ErrorResult("Belə cədvəl yoxdur.");

            var columns = Columns(meta);
            var key = KeyColumn(meta);
            var keyColumn = columns.First(c => c.Name == key);
            var keyValue = Convert(id, keyColumn);

            var soft = columns.Any(c => c.Name == "IsDeleted");

            if (!soft)
            {
                if (!deleted)
                    return new ErrorResult("Bu cədvəldə geri qaytarma dəstəklənmir.");

                try
                {
                    var removed = await ExecuteAsync(
                        $"DELETE FROM \"{meta.Name}\" WHERE \"{key}\" = @p0",
                        new List<object?> { keyValue });

                    if (removed == 0)
                        return new ErrorResult("Sətir tapılmadı.");
                }
                catch (DbException ex)
                {
                    return new ErrorResult(Explain(ex));
                }

                _audit.Set("DataRowDeleted", $"«{Label(meta.Name)}» cədvəlindən sətir sildi", meta.Name);
                return new SuccessResult("Sətir silindi.");
            }

            var affected = await ExecuteAsync(
                $"UPDATE \"{meta.Name}\" SET \"IsDeleted\" = @p0, \"DeletedAt\" = @p1 WHERE \"{key}\" = @p2",
                new List<object?> { deleted, deleted ? DateTime.UtcNow : null, keyValue });

            if (affected == 0)
                return new ErrorResult("Sətir tapılmadı.");

            // Valideyn işarələnəndə uşaqları da eyni vəziyyətə gətiririk
            var cascaded = await CascadeAsync(meta.Name, keyValue, deleted);

            _audit.Set(
                deleted ? "DataRowDeleted" : "DataRowRestored",
                deleted
                    ? $"«{Label(meta.Name)}» cədvəlində sətri silinmiş kimi işarələdi"
                    : $"«{Label(meta.Name)}» cədvəlində silinmiş sətri geri qaytardı",
                meta.Name);

            var suffix = cascaded > 0 ? $" Bağlı {cascaded} sətir də yeniləndi." : string.Empty;

            return new SuccessResult(deleted
                ? $"Sətir silindi (bazada qalır, geri qaytarmaq olar).{suffix}"
                : $"Sətir geri qaytarıldı.{suffix}");
        }

        // Valideyn–uşaq zənciri: saray → zal/menyu/şəkil/özəllik/rəy, zal → media, menyu → tərkib
        private async Task<int> CascadeAsync(string table, object? keyValue, bool deleted)
        {
            var children = table switch
            {
                "Venues" => new[]
                {
                    ("Halls", "VenueId"), ("Menus", "VenueId"), ("VenueImages", "VenueId"),
                    ("VenueFeatures", "VenueId"), ("Reviews", "VenueId")
                },
                "Halls" => new[] { ("HallMedia", "HallId") },
                "Menus" => new[] { ("MenuItems", "MenuId") },
                _ => Array.Empty<(string, string)>()
            };

            var total = 0;

            foreach (var (child, column) in children)
            {
                total += await ExecuteAsync(
                    $"UPDATE \"{child}\" SET \"IsDeleted\" = @p0, \"DeletedAt\" = @p1 " +
                    $"WHERE \"{column}\" = @p2 AND \"IsDeleted\" <> @p0",
                    new List<object?> { deleted, deleted ? DateTime.UtcNow : null, keyValue });
            }

            // Zalların mediası saray silinəndə də bağlanmalıdır
            if (table == "Venues")
            {
                total += await ExecuteAsync(
                    "UPDATE \"HallMedia\" SET \"IsDeleted\" = @p0, \"DeletedAt\" = @p1 " +
                    "WHERE \"HallId\" IN (SELECT \"Id\" FROM \"Halls\" WHERE \"VenueId\" = @p2) " +
                    "AND \"IsDeleted\" <> @p0",
                    new List<object?> { deleted, deleted ? DateTime.UtcNow : null, keyValue });

                total += await ExecuteAsync(
                    "UPDATE \"MenuItems\" SET \"IsDeleted\" = @p0, \"DeletedAt\" = @p1 " +
                    "WHERE \"MenuId\" IN (SELECT \"Id\" FROM \"Menus\" WHERE \"VenueId\" = @p2) " +
                    "AND \"IsDeleted\" <> @p0",
                    new List<object?> { deleted, deleted ? DateTime.UtcNow : null, keyValue });
            }

            return total;
        }

        // ---------- metadata ----------

        private record TableMeta(string Name, Microsoft.EntityFrameworkCore.Metadata.IEntityType Entity);

        private IEnumerable<string> TableNames() =>
            _context.Model.GetEntityTypes()
                .Select(e => e.GetTableName())
                .Where(n => !string.IsNullOrEmpty(n))
                .Distinct()!
                .Cast<string>();

        private TableMeta? FindTable(string table, UserRole viewerRole)
        {
            if (IsRestricted(table, viewerRole))
                return null;

            var entity = _context.Model.GetEntityTypes()
                .FirstOrDefault(e => string.Equals(e.GetTableName(), table, StringComparison.OrdinalIgnoreCase));

            return entity is null ? null : new TableMeta(entity.GetTableName()!, entity);
        }

        // Rəy düzəliş tarixçəsi yalnız platforma sahibinə açıqdır — dəstək admini
        // öz düzəlişinin izini nə görə, nə də silə bilməlidir.
        private static bool IsRestricted(string table, UserRole viewerRole) =>
            string.Equals(table, "ReviewEdits", StringComparison.OrdinalIgnoreCase)
            && viewerRole != UserRole.SuperAdmin;

        private static string Label(string table) => Labels.TryGetValue(table, out var l) ? l : table;

        private static List<ColumnInfoDto> Columns(TableMeta meta)
        {
            var keyNames = meta.Entity.FindPrimaryKey()?.Properties.Select(p => p.Name).ToHashSet() ?? new HashSet<string>();

            return meta.Entity.GetProperties()
                .Where(p => !HiddenColumns.Contains(p.Name))
                .Select(p => new ColumnInfoDto
                {
                    Name = p.GetColumnName(),
                    Type = TypeOf(p.ClrType),
                    IsKey = keyNames.Contains(p.Name),
                    IsRequired = !p.IsNullable,
                    IsGenerated = p.ValueGenerated != Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never
                })
                .ToList();
        }

        private static string KeyColumn(TableMeta meta) =>
            meta.Entity.FindPrimaryKey()?.Properties.FirstOrDefault()?.GetColumnName() ?? "Id";

        private static string TypeOf(Type clr)
        {
            var type = Nullable.GetUnderlyingType(clr) ?? clr;

            if (type == typeof(bool)) return "bool";
            if (type == typeof(int) || type == typeof(long) || type == typeof(short)) return "number";
            if (type == typeof(decimal) || type == typeof(double) || type == typeof(float)) return "decimal";
            if (type == typeof(DateOnly)) return "date";
            if (type == typeof(DateTime) || type == typeof(DateTimeOffset)) return "datetime";
            if (type.IsEnum) return "number";

            return "text";
        }

        // ---------- sorğular ----------

        private async Task<int> ScalarIntAsync(string sql, List<object?>? parameters = null)
        {
            await using var command = Command(sql, parameters);
            var result = await command.ExecuteScalarAsync();
            return System.Convert.ToInt32(result);
        }

        private async Task<int> ExecuteAsync(string sql, List<object?> parameters)
        {
            await using var command = Command(sql, parameters);
            return await command.ExecuteNonQueryAsync();
        }

        private async Task<List<Dictionary<string, object?>>> QueryAsync(
            string sql, List<object?> parameters, List<ColumnInfoDto> columns)
        {
            await using var command = Command(sql, parameters);
            await using var reader = await command.ExecuteReaderAsync();

            var rows = new List<Dictionary<string, object?>>();

            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>();
                for (var i = 0; i < columns.Count; i++)
                {
                    var value = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
                    row[columns[i].Name] = value;
                }
                rows.Add(row);
            }

            return rows;
        }

        private DbCommand Command(string sql, List<object?>? parameters)
        {
            var connection = _context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = sql;

            for (var i = 0; i < (parameters?.Count ?? 0); i++)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = $"p{i}";
                parameter.Value = parameters![i] ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }

            return command;
        }

        // ---------- dəyər çevrilməsi ----------

        // Paneldən gələn dəyər JSON-dur; sütunun tipinə uyğun CLR dəyərinə çeviririk.
        private static object? Convert(object? raw, ColumnInfoDto column)
        {
            var text = raw switch
            {
                null => null,
                JsonElement json => json.ValueKind switch
                {
                    JsonValueKind.Null => null,
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.String => json.GetString(),
                    _ => json.ToString()
                },
                _ => raw.ToString()
            };

            if (string.IsNullOrWhiteSpace(text))
                return null;

            text = text.Trim();

            return column.Type switch
            {
                "bool" => bool.TryParse(text, out var b) ? b : text == "1",
                "number" => long.TryParse(text, out var n) ? (int)n : (object?)null,
                "decimal" => decimal.TryParse(text, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : null,
                "date" => DateOnly.TryParse(text, out var date) ? date : null,
                "datetime" => DateTime.TryParse(text, null,
                    System.Globalization.DateTimeStyles.AdjustToUniversal |
                    System.Globalization.DateTimeStyles.AssumeUniversal, out var dt) ? dt : null,
                _ => text
            };
        }

        // Baza xətalarını insan dilinə çeviririk
        private static string Explain(DbException ex)
        {
            var message = ex.Message;

            if (message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase))
                return "Bu dəyər artıq mövcuddur (təkrarlana bilməz).";

            if (message.Contains("violates foreign key", StringComparison.OrdinalIgnoreCase))
                return "Bağlı sətir var və ya göstərilən əlaqə mövcud deyil.";

            if (message.Contains("not-null", StringComparison.OrdinalIgnoreCase))
                return "Məcburi sahə boş qala bilməz.";

            return $"Baza xətası: {message}";
        }
    }
}
