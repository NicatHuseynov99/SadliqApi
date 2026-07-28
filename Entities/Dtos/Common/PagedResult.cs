namespace Entities.Dtos.Common
{
    // Səhifələnmiş siyahı cavabı — sonsuz scroll üçün istifadə olunur
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }

        // Frontend növbəti səhifəni istəyib-istəməyəcəyini bununla bilir
        public bool HasMore => Page * PageSize < TotalCount;
    }
}
