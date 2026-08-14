using Entities.Concrete;

namespace Business.Abstract
{
    // Saray səviyyəsində səlahiyyət yoxlaması — bütün servislər eyni məntiqdən istifadə etsin deyə
    // bir yerdə saxlanılır. Bir yerdə unudulsa, bütün sistem açıq qalar.
    public interface IVenueAccessService
    {
        // İstifadəçi bu sarayın sahibidirmi
        Task<bool> IsOwnerAsync(int venueId, int userId);

        // Sahib və ya həmin saraya təyin olunmuş aktiv administratordurmu
        Task<bool> CanManageAsync(int venueId, int userId);

        // Dəyişiklik etmək icazəsi. İdarəetmədən fərqi: təsdiqlənməmiş saray
        // yalnız baxış üçün açıqdır — sahibkar təsdiqi gözləyərkən heç nəyi dəyişə
        // bilmir. Platforma admini və dəstək bu məhdudiyyətdən azaddır.
        Task<bool> CanModifyAsync(int venueId, int userId);

        // Zal səviyyəsində eyni qayda
        Task<bool> CanModifyHallAsync(int hallId, int userId);

        // İstifadəçinin idarə edə bildiyi bütün sarayların Id-ləri (sahib olduqları + təyinatları)
        Task<List<int>> GetManagedVenueIdsAsync(int userId);

        // Administratorun konkret səlahiyyəti — sahib üçün həmişə true
        Task<bool> HasStaffPermissionAsync(int venueId, int userId, StaffPermission permission);

        // Konkret zalda iş görə bilirmi (administratora yalnız seçilmiş zallar verilir)
        Task<bool> CanManageHallAsync(int hallId, int userId);

        // Administratorun icazəli zalları. null qayıdarsa məhdudiyyət yoxdur (sahib/dəstək)
        Task<List<int>?> GetAllowedHallIdsAsync(int userId);

        // Sarayda baş verən hadisədən kimin xəbəri olmalıdır: sahib və həmin
        // işi görməyə səlahiyyəti olan aktiv administratorlar.
        //
        // Səlahiyyət yoxlamasından fərqli olaraq bura SuperAdmin və dəstəyi
        // qatmır — onlar hər sarayda sahib sayılır, qatsaq platformadakı bütün
        // sarayların hər rezervi onların bildiriş lentinə düşərdi.
        // Sahibin Id-si parametr kimi ötürülür: çağıran onsuz da sarayı əlində
        // tutur, üstəlik sarayı burada yenidən yükləsək yumşaq silinmiş saray
        // süzgəcə düşər və bildiriş tamamilə itərdi.
        Task<List<int>> GetNotifyUserIdsAsync(int venueId, int ownerId, StaffPermission permission);
    }
}
