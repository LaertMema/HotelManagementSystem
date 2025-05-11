namespace HotelManagementApp.Models.Enums
{
    public enum ReservationStatus
    {
        Confirmed, //Keep either this or reserved
        CheckedIn,
        CheckedOut,
        Cancelled,
        Pending,
        Completed
    }
}
