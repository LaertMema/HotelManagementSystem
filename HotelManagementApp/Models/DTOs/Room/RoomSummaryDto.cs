namespace HotelManagementApp.Models.DTOs.Room
{
    public class RoomSummaryDto
    {
        public int Id { get; set; }
        public string RoomNumber { get; set; }
        public int Floor { get; set; }
        public string RoomTypeName { get; set; }
        public decimal BasePrice { get; set; }
        public string Status { get; set; }
        public bool NeedsCleaning { get; set; }
        public int Capacity { get; set; }
    }
}
