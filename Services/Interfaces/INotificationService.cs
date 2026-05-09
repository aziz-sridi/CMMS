using CMMS.Domain.Entities;

namespace CMMS.Services.Interfaces;

public interface INotificationService
{
    Task<List<Notification>> GetAllAsync();
    Task<List<Notification>> GetForTechnicianAsync(Guid technicianId);
    Task<Notification?> GetByIdAsync(Guid id);
    Task<Notification> CreateAsync(Notification notification);
    Task<bool> MarkAsReadAsync(Guid id);
    Task<bool> DeleteAsync(Guid id);
    Task<int> CountUnreadAsync(Guid technicianId);
}
