namespace Core.Entities
{
    // Bu interfeysi qoyan sətir bazadan silinmir, sadəcə silinmiş kimi işarələnir.
    // Səbəb: tarixçə dağılmasın və səhvən silinən məlumat geri qaytarıla bilsin.
    public interface ISoftDelete
    {
        bool IsDeleted { get; set; }
        DateTime? DeletedAt { get; set; }
    }
}
