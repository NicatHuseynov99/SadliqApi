using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos.Audit;
using Entities.Dtos.Common;
using Microsoft.EntityFrameworkCore;

namespace Business.Concrete
{
    public class AuditManager : IAuditService
    {
        private readonly IAuditLogDal _auditLogDal;

        public AuditManager(IAuditLogDal auditLogDal)
        {
            _auditLogDal = auditLogDal;
        }

        // Jurnal siyahısı. Dəstək admini SuperAdmin-in əməliyyatlarını görmür —
        // bu, interfeysdə yox, sorğunun özündə süzülür
        public async Task<IDataResult<PagedResult<AuditLog>>> SearchAsync(AuditSearchDto dto, UserRole viewerRole)
        {
            var page = dto.Page < 1 ? 1 : dto.Page;
            var pageSize = dto.PageSize is < 1 or > 100 ? 30 : dto.PageSize;

            var query = await _auditLogDal.GetListAsync();
            query = ApplyVisibility(query, viewerRole);

            if (!string.IsNullOrWhiteSpace(dto.Q))
            {
                var term = $"%{dto.Q.Trim()}%";
                query = query.Where(a =>
                    EF.Functions.ILike(a.Description, term) || EF.Functions.ILike(a.UserFullName, term));
            }

            if (!string.IsNullOrWhiteSpace(dto.Action))
                query = query.Where(a => a.Action == dto.Action);

            if (!string.IsNullOrWhiteSpace(dto.Role) && Enum.TryParse<UserRole>(dto.Role, true, out var role))
                query = query.Where(a => a.UserRole == role);

            if (dto.UserId.HasValue)
                query = query.Where(a => a.UserId == dto.UserId);

            if (dto.VenueId.HasValue)
                query = query.Where(a => a.VenueId == dto.VenueId);

            if (!string.IsNullOrWhiteSpace(dto.EntityType))
                query = query.Where(a => a.EntityType == dto.EntityType);

            if (dto.From.HasValue)
                query = query.Where(a => a.CreatedAt >= dto.From.Value.ToUniversalTime());

            if (dto.To.HasValue)
                query = query.Where(a => a.CreatedAt <= dto.To.Value.ToUniversalTime());

            if (dto.OnlyFailed == true)
                query = query.Where(a => !a.IsSuccess);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new PagedResult<AuditLog>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return new SuccessDataResult<PagedResult<AuditLog>>(result);
        }

        // Süzgəc siyahısı bazadakı real əməliyyatlardan qurulur
        public async Task<IDataResult<AuditFilterOptionsDto>> GetFilterOptionsAsync(UserRole viewerRole)
        {
            var query = await _auditLogDal.GetListAsync();
            query = ApplyVisibility(query, viewerRole);

            var grouped = await query
                .GroupBy(a => a.Action)
                .Select(g => new { Code = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ToListAsync();

            var options = new AuditFilterOptionsDto
            {
                Actions = grouped.Select(g => new AuditActionOption
                {
                    Code = g.Code,
                    Label = AuditLabels.Describe(g.Code),
                    Count = g.Count
                }).ToList()
            };

            return new SuccessDataResult<AuditFilterOptionsDto>(options);
        }

        private static IQueryable<AuditLog> ApplyVisibility(IQueryable<AuditLog> query, UserRole viewerRole) =>
            viewerRole == UserRole.SuperAdmin
                ? query
                : query.Where(a => a.UserRole != UserRole.SuperAdmin);
    }

    // Əməliyyat kodlarının Azərbaycanca adları (süzgəc siyahısı üçün)
    public static class AuditLabels
    {
        private static readonly Dictionary<string, string> Map = new()
        {
            ["UserRegistered"] = "Qeydiyyat",
            ["UserLoggedIn"] = "Sistemə giriş",
            ["ProfileUpdated"] = "Profil yeniləndi",
            ["PasswordChanged"] = "Parol dəyişdirildi",
            ["PasswordResetRequested"] = "Parol sıfırlama sorğusu",
            ["PasswordReset"] = "Parol bərpa edildi",
            ["VenueCreated"] = "Saray yaradıldı",
            ["VenueUpdated"] = "Saray yeniləndi",
            ["VenueDeleted"] = "Saray silindi",
            ["VenueApproved"] = "Saray təsdiqləndi",
            ["HallCreated"] = "Zal yaradıldı",
            ["HallUpdated"] = "Zal yeniləndi",
            ["HallDeleted"] = "Zal silindi",
            ["HallAvailabilityChanged"] = "Zal təqvimi dəyişdi",
            ["MenuCreated"] = "Menyu yaradıldı",
            ["MenuUpdated"] = "Menyu yeniləndi",
            ["MenuDeleted"] = "Menyu silindi",
            ["ReservationCreated"] = "Rezerv sorğusu",
            ["ReservationStatusChanged"] = "Rezerv statusu dəyişdi",
            ["ReservationCancelled"] = "Rezerv ləğv edildi",
            ["ReviewCreated"] = "Rəy yazıldı",
            ["ReviewUpdated"] = "Rəy redaktə edildi",
            ["ReviewDeleted"] = "Rəy silindi",
            ["ReservationStatusChangedByAdmin"] = "Rezerv statusunu admin dəyişdi",
            ["ReviewReported"] = "Rəy şikayət edildi",
            ["ReviewModerated"] = "Rəyin görünüşü dəyişdi",
            ["ReviewHidden"] = "Rəy gizlədildi",
            ["ReviewShown"] = "Rəy yenidən göstərildi",
            ["ReviewReportsResolved"] = "Şikayətlər bağlandı",
            ["ReviewDeletedByAdmin"] = "Rəy admin tərəfindən silindi",
            ["FavoriteToggled"] = "Seçilmişlər dəyişdi",
            ["StaffCreated"] = "Administrator əlavə edildi",
            ["StaffUpdated"] = "Administrator yeniləndi",
            ["StaffActivationChanged"] = "Administratorun girişi dəyişdi",
            ["StaffHallsChanged"] = "Administratorun zalları dəyişdi",
            ["StaffPasswordReset"] = "Administratorun parolu sıfırlandı",
            ["StaffDeleted"] = "Administrator silindi",
            ["UserRoleChanged"] = "İstifadəçinin rolu dəyişdi",
            ["AdminViewedUsers"] = "İstifadəçi siyahısına baxış",
            ["AdminViewedUser"] = "İstifadəçi kartına baxış",
            ["UserBlocked"] = "İstifadəçi bloklandı",
            ["UserUnblocked"] = "İstifadəçinin bloku götürüldü",
            ["UserPasswordReset"] = "Parol admin tərəfindən sıfırlandı",
            ["BlockedLoginAttempt"] = "Bloklanmış hesabla giriş cəhdi",
            ["AdminViewedStats"] = "Göstəricilərə baxış",
            ["AuditViewed"] = "Jurnala baxış",
        };

        public static string Describe(string code) => Map.TryGetValue(code, out var label) ? label : code;
    }
}
