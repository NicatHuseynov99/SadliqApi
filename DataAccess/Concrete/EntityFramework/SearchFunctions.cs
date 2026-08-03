namespace DataAccess.Concrete.EntityFramework
{
    // Bazadakı az_normalize() funksiyası üçün körpü. Gövdəsi heç vaxt icra olunmur —
    // EF bu çağırışı SQL-ə çevirir.
    //
    // Funksiya Azərbaycan hərflərini sadələşdirir və kiçik hərfə salır:
    // «Zümrüd» → «zumrud», «Gəncə» → «ganca», «Şəlalə» → «salala».
    // Nəticədə müştəri «Zumrud» və ya «Gence» yazanda da axtardığını tapır.
    //
    // Baza tərəfdə tələb olunur:
    //   CREATE EXTENSION unaccent;
    //   CREATE FUNCTION az_normalize(text) ...
    public static class SearchFunctions
    {
        public static string Normalize(string input) =>
            throw new NotSupportedException("Bu metod yalnız EF sorğularında işlədilir.");
    }
}
