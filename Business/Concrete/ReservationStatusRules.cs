using Entities.Concrete;

namespace Business.Concrete
{
    // Rezervin hansı vəziyyətdən hansına keçə biləcəyi bir yerdə saxlanılır.
    //
    // Əvvəllər yoxlama yox idi: sahib təsdiqlənmiş rezervi istənilən vəziyyətə,
    // hətta ləğv edilmiş sorğunu yenidən «gözləyir» vəziyyətinə qaytara bilirdi.
    // Toy bir dəfə olur — təsdiqdən sonrakı dəyişiklik izsiz qalmamalıdır.
    public static class ReservationStatusRules
    {
        // Son vəziyyətlər: bunlardan geri qayıdış yalnız admin müdaxiləsi ilə olur
        private static readonly ReservationStatus[] Final =
        {
            ReservationStatus.Rejected,
            ReservationStatus.Cancelled,
            ReservationStatus.Expired
        };

        public static bool IsFinal(ReservationStatus status) => Final.Contains(status);

        // Saray tərəfi (sahib və ya sarayın administratoru) nə edə bilər
        public static bool OwnerCanMove(ReservationStatus from, ReservationStatus to) =>
            (from, to) switch
            {
                // Cavabsız sorğuya cavab
                (ReservationStatus.Pending, ReservationStatus.Confirmed) => true,
                (ReservationStatus.Pending, ReservationStatus.Rejected) => true,

                // Əks-təklif göndərildikdən sonra da cavab verə bilər
                (ReservationStatus.CounterOffered, ReservationStatus.Confirmed) => true,
                (ReservationStatus.CounterOffered, ReservationStatus.Rejected) => true,

                // Təsdiqlənmiş tədbirin ləğvi — mümkündür, amma səbəb tələb olunur
                (ReservationStatus.Confirmed, ReservationStatus.Cancelled) => true,

                _ => false
            };

        // Müştəri yalnız ləğv edə bilər — özü də son vəziyyətdə olmayan rezervi
        public static bool CustomerCanCancel(ReservationStatus from) => !IsFinal(from);

        // Səbəb yalnız təsdiqlənmiş tədbir ləğv olunanda məcburidir.
        // Cavablanmamış sorğunu geri götürmək üçün izahat istəmək artıq yükdür.
        public static bool RequiresReason(ReservationStatus from, ReservationStatus to) =>
            from == ReservationStatus.Confirmed && to == ReservationStatus.Cancelled;

        // İcazə verilməyən keçid üçün insana anlaşılan izah
        public static string Explain(ReservationStatus from, ReservationStatus to)
        {
            if (IsFinal(from))
                return $"Rezerv artıq «{Label(from)}» vəziyyətindədir — dəyişmək olmaz.";

            return $"«{Label(from)}» vəziyyətindən «{Label(to)}» vəziyyətinə keçmək olmaz.";
        }

        public static string Label(ReservationStatus status) => status switch
        {
            ReservationStatus.Pending => "Gözləyir",
            ReservationStatus.Confirmed => "Təsdiqlənib",
            ReservationStatus.Rejected => "Rədd edilib",
            ReservationStatus.Cancelled => "Ləğv edilib",
            ReservationStatus.Expired => "Vaxtı bitib",
            _ => "Əks-təklif göndərilib"
        };
    }
}
