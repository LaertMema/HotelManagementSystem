namespace HotelManagementApp.Models
{
    public class Service
    {
        public int Id { get; set; }
        public string ServiceName { get; set; }

        public string ServiceType { get; set; } // e.g., "Room Service", "Spa", "Laundry"
        public string Description { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }

        public ICollection<ServiceOrder> ServiceOrders { get; set; }
    }
}
