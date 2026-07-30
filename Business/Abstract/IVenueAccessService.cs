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

        // İstifadəçinin idarə edə bildiyi bütün sarayların Id-ləri (sahib olduqları + təyinatları)
        Task<List<int>> GetManagedVenueIdsAsync(int userId);

        // Konkret zalda iş görə bilirmi (administratora yalnız seçilmiş zallar verilir)
        Task<bool> CanManageHallAsync(int hallId, int userId);

        // Administratorun icazəli zalları. null qayıdarsa məhdudiyyət yoxdur (sahib/dəstək)
        Task<List<int>?> GetAllowedHallIdsAsync(int userId);
    }
}
