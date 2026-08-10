namespace Entities.Dtos.Staff
{
    // Rəhbər administratorun səlahiyyətlərini təyin edir.
    // Hamısı söndürülsə administrator yalnız sorğuları görür — cavabı rəhbər verir.
    public class SetStaffPermissionsDto
    {
        public bool CanConfirmReservations { get; set; }
        public bool CanEditCalendar { get; set; }
        public bool CanSendOffers { get; set; }
    }
}
