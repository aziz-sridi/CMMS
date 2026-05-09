using CMMS.Domain.Entities;
using CMMS.Infrastructure.Persistence;
using CMMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Services.Implementations;

public class NotificationService(AppDbContext db) : INotificationService
{
    public Task<List<Notification>> GetAllAsync() =>
        db.Notifications
            .Include(n => n.Technician)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

    public Task<List<Notification>> GetForTechnicianAsync(Guid technicianId) =>
        db.Notifications
            .Where(n => n.TechnicianId == technicianId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

    public Task<Notification?> GetByIdAsync(Guid id) =>
        db.Notifications
            .Include(n => n.Technician)
            .FirstOrDefaultAsync(n => n.Id == id);

    public async Task<Notification> CreateAsync(Notification notification)
    {
        if (notification.Id == Guid.Empty) notification.Id = Guid.NewGuid();
        if (notification.CreatedAt == default) notification.CreatedAt = DateTime.UtcNow;
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();
        return notification;
    }

    public async Task<bool> MarkAsReadAsync(Guid id)
    {
        var entity = await db.Notifications.FindAsync(id);
        if (entity is null) return false;
        entity.IsRead = true;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await db.Notifications.FindAsync(id);
        if (entity is null) return false;
        db.Notifications.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    public Task<int> CountUnreadAsync(Guid technicianId) =>
        db.Notifications.CountAsync(n => n.TechnicianId == technicianId && !n.IsRead);
}
