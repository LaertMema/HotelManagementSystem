namespace HotelManagementApp.Models.DTOs.Room
{
    public class RoomAvailabilityDto
    {
        public int Id { get; set; }
        public string RoomNumber { get; set; }
        public string RoomTypeName { get; set; }
        public decimal BasePrice { get; set; }
        public int Capacity { get; set; }
        public bool IsAvailable { get; set; }
        public DateTime? NextAvailableDate { get; set; }
        public string[] Amenities { get; set; }
        public string ImageUrl { get; set; }
    }
}
