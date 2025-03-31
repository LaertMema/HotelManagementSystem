using Microsoft.AspNetCore.Identity;

namespace HotelManagementApp.Models
{
    public class ApplicationRole : IdentityRole<int>
    {
        public string RoleName { get; set; }
        public string Description { get; set; }
    }
}
