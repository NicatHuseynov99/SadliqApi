namespace SadliqApi.Audit
{
    // Servis kodu ayrıca izah yazmasa, endpoint adına görə Azərbaycanca mətn buradan götürülür.
    // Açar: "Controller.Action"
    public static class AuditDictionary
    {
        private static readonly Dictionary<string, (string Action, string Text)> Map = new()
        {
            // Hesab
            ["Auth.Register"] = ("UserRegistered", "yeni hesab yaratdı"),
            ["Auth.Login"] = ("UserLoggedIn", "sistemə daxil oldu"),
            ["Auth.UpdateProfile"] = ("ProfileUpdated", "profil məlumatlarını yenilədi"),
            ["Auth.ChangePassword"] = ("PasswordChanged", "parolunu dəyişdi"),
            ["Auth.ForgotPassword"] = ("PasswordResetRequested", "parol sıfırlama sorğusu göndərdi"),
            ["Auth.ResetPassword"] = ("PasswordReset", "parolunu link vasitəsilə yenilədi"),

            // Saray
            ["Venues.Create"] = ("VenueCreated", "yeni saray əlavə etdi"),
            ["Venues.Update"] = ("VenueUpdated", "sarayın məlumatlarını yenilədi"),
            ["Venues.Delete"] = ("VenueDeleted", "sarayı sildi"),
            ["Venues.Approve"] = ("VenueApproved", "sarayı təsdiqlədi"),

            // Zal
            ["Halls.Create"] = ("HallCreated", "yeni zal əlavə etdi"),
            ["Halls.Update"] = ("HallUpdated", "zalın məlumatlarını yenilədi"),
            ["Halls.Delete"] = ("HallDeleted", "zalı sildi"),
            ["Halls.SetAvailability"] = ("HallAvailabilityChanged", "zalın təqvimini dəyişdi"),

            // Menyu
            ["Menus.Create"] = ("MenuCreated", "yeni menyu əlavə etdi"),
            ["Menus.Update"] = ("MenuUpdated", "menyunu yenilədi"),
            ["Menus.Delete"] = ("MenuDeleted", "menyunu sildi"),

            // Rezerv
            ["Reservations.Create"] = ("ReservationCreated", "rezerv sorğusu göndərdi"),
            ["Reservations.UpdateStatus"] = ("ReservationStatusChanged", "rezervin statusunu dəyişdi"),
            ["Reservations.Cancel"] = ("ReservationCancelled", "rezervi ləğv etdi"),

            // Rəy və seçilmişlər
            ["Reviews.Create"] = ("ReviewCreated", "rəy yazdı"),
            ["Reviews.Update"] = ("ReviewUpdated", "rəyini redaktə etdi"),
            ["Reviews.Delete"] = ("ReviewDeleted", "rəyini sildi"),
            ["Reviews.Report"] = ("ReviewReported", "rəyi şikayət etdi"),
            ["Favorites.Toggle"] = ("FavoriteToggled", "seçilmişlər siyahısını dəyişdi"),

            // Platforma üzrə rezervlər
            ["AdminReservations.UpdateStatus"] = ("ReservationStatusChangedByAdmin", "rezervin statusunu admin olaraq dəyişdi"),

            // Rəy moderasiyası
            ["ReviewModeration.SetHidden"] = ("ReviewModerated", "rəyin görünüşünü dəyişdi"),
            ["ReviewModeration.ResolveReports"] = ("ReviewReportsResolved", "rəyin şikayətlərini bağladı"),
            ["ReviewModeration.Delete"] = ("ReviewDeletedByAdmin", "rəyi tamamilə sildi"),

            // Administratorlar
            ["VenueStaff.Create"] = ("StaffCreated", "yeni saray administratoru əlavə etdi"),
            ["VenueStaff.Update"] = ("StaffUpdated", "administratorun məlumatlarını yenilədi"),
            ["VenueStaff.SetActive"] = ("StaffActivationChanged", "administratorun girişini dəyişdi"),
            ["VenueStaff.SetHalls"] = ("StaffHallsChanged", "administratorun zal icazələrini dəyişdi"),
            ["VenueStaff.ResetPassword"] = ("StaffPasswordReset", "administratorun parolunu sıfırladı"),
            ["VenueStaff.Delete"] = ("StaffDeleted", "administratoru siyahıdan sildi"),

            // Platforma
            ["Admin.ChangeRole"] = ("UserRoleChanged", "istifadəçinin rolunu dəyişdi"),
            ["Admin.GetUsers"] = ("AdminViewedUsers", "istifadəçi siyahısına baxdı"),
            ["Admin.GetStats"] = ("AdminViewedStats", "platforma göstəricilərinə baxdı"),
            ["Admin.GetUserDetail"] = ("AdminViewedUser", "istifadəçi kartına baxdı"),
            ["Admin.SetUserActive"] = ("UserBlocked", "istifadəçi hesabının statusunu dəyişdi"),
            ["Admin.ResetUserPassword"] = ("UserPasswordReset", "istifadəçinin parolunu sıfırladı"),
            ["Audit.Search"] = ("AuditViewed", "fəaliyyət jurnalına baxdı"),
        };

        public static (string Action, string Text) Resolve(string controller, string action)
        {
            if (Map.TryGetValue($"{controller}.{action}", out var found))
                return found;

            // Lüğətdə yoxdursa ümumi mətn qururuq
            return ($"{controller}{action}", $"{controller} bölməsində «{action}» əməliyyatını icra etdi");
        }
    }
}
