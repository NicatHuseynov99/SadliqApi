using Business.Abstract;

namespace SadliqApi.Audit
{
    // Servis kodu ayrıca izah yazmasa, endpoint adına görə Azərbaycanca mətn götürülür.
    // Mətnlər artıq kodda deyil, bazadadır ("audit_endpoint" kateqoriyası):
    // açar "Controller.Action", dəyər izah, əlavə sahə isə əməliyyat kodudur.
    public static class AuditDictionary
    {
        public static (string Action, string Text) Resolve(ITextCatalog catalog, string controller, string action)
        {
            var key = $"{controller}.{action}";

            if (catalog.TryGetPair(TextCategories.AuditEndpoint, key, out var text, out var code)
                && !string.IsNullOrWhiteSpace(code))
                return (code!, text);

            // Bazada yoxdursa ümumi mətn qururuq — jurnal boş qalmasın.
            // Mətni admin paneldəki «İnterfeys mətnləri» bölməsindən əlavə etmək olar.
            return ($"{controller}{action}", $"{controller} bölməsində «{action}» əməliyyatını icra etdi");
        }
    }
}
