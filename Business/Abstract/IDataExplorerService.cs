using Core.Utilities.Results;
using Entities.Concrete;
using Entities.Dtos.Data;

namespace Business.Abstract
{
    // Bazanın birbaşa idarəsi: hər cədvəldə oxu, əlavə, redaktə və silmə.
    // Cədvəl və sütun adları EF modelindən götürülür — kənardan gələn ad heç vaxt
    // sorğuya birbaşa yazılmır, ona görə SQL injection mümkün deyil.
    public interface IDataExplorerService
    {
        // Bəzi cədvəllər (rəy düzəliş tarixçəsi) yalnız platforma sahibinə görünür,
        // ona görə hər çağırışda baxanın rolu ötürülür.
        Task<IDataResult<List<TableInfoDto>>> GetTablesAsync(UserRole viewerRole);

        Task<IDataResult<TableRowsDto>> GetRowsAsync(
            string table, string? q, int page, int pageSize, string? sort, bool desc, bool includeDeleted,
            UserRole viewerRole);

        Task<IResult> CreateRowAsync(string table, TableRowDto dto, UserRole viewerRole);
        Task<IResult> UpdateRowAsync(string table, string id, TableRowDto dto, UserRole viewerRole);

        // Silmək = işarələmək; geri qaytarmaq həmişə mümkündür
        Task<IResult> DeleteRowAsync(string table, string id, UserRole viewerRole);
        Task<IResult> RestoreRowAsync(string table, string id, UserRole viewerRole);
    }
}
