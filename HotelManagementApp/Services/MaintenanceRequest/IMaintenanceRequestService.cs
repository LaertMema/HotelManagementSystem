using HotelManagementApp.Models.DTOs.MaintenanceRequest;
using HotelManagementApp.Models.Enums;


namespace HotelManagementApp.Services.MaintenanceRequest
{
  
        public interface IMaintenanceRequestService
        {
            Task<MaintenanceRequestDto> CreateMaintenanceRequestAsync(CreateMaintenanceRequestDto dto, int reportedById);
            Task<MaintenanceRequestDto> GetMaintenanceRequestByIdAsync(int id);
            Task<IEnumerable<MaintenanceRequestDto>> GetAllMaintenanceRequestsAsync();
            Task<IEnumerable<MaintenanceRequestDto>> GetMaintenanceRequestsByStatusAsync(MaintenanceRequestStatus status);
            Task<IEnumerable<MaintenanceRequestDto>> GetMaintenanceRequestsByRoomAsync(int roomId);
            Task<IEnumerable<MaintenanceRequestDto>> GetMaintenanceRequestsAssignedToUserAsync(int userId);
            Task<MaintenanceRequestDto> UpdateMaintenanceRequestAsync(int id, UpdateMaintenanceRequestDto dto);
            Task<bool> DeleteMaintenanceRequestAsync(int id);
            Task<bool> AssignMaintenanceRequestAsync(int id, int assignedToId);
            Task<bool> CompleteMaintenanceRequestAsync(int id, string resolutionNotes, decimal? costOfRepair = null);
        }
    

}
