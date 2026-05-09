using CMMS.Domain.Entities;
using CMMS.Infrastructure.Persistence;
using CMMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Services.Implementations;

public class FailureService(AppDbContext db) : IFailureService
{
    public async Task<List<Failure>> GetAllAsync(string? search = null, FailureStatus? status = null)
    {
        var query = db.Failures.Include(f => f.Equipment).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(f => f.Description.Contains(search) || f.Equipment.Name.Contains(search));

        if (status.HasValue)
            query = query.Where(f => f.Status == status.Value);

        return await query.OrderByDescending(f => f.ReportDate).ToListAsync();
    }

    public Task<Failure?> GetByIdAsync(Guid id) =>
        db.Failures
            .Include(f => f.Equipment)
            .Include(f => f.Interventions)
            .FirstOrDefaultAsync(f => f.Id == id);

    public Task<List<Failure>> GetByEquipmentAsync(Guid equipmentId) =>
        db.Failures
            .Where(f => f.EquipmentId == equipmentId)
            .OrderByDescending(f => f.ReportDate)
            .ToListAsync();

    public async Task<Failure> CreateAsync(Failure failure)
    {
        if (failure.Id == Guid.Empty) failure.Id = Guid.NewGuid();
        if (failure.ReportDate == default) failure.ReportDate = DateTime.UtcNow;
        db.Failures.Add(failure);
        await db.SaveChangesAsync();
        return failure;
    }

    public async Task<bool> UpdateAsync(Failure failure)
    {
        var existing = await db.Failures.FirstOrDefaultAsync(f => f.Id == failure.Id);
        if (existing is null) return false;

        existing.Description = failure.Description;
        existing.Severity = failure.Severity;
        existing.Status = failure.Status;
        existing.EquipmentId = failure.EquipmentId;
        existing.ReportDate = failure.ReportDate;

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await db.Failures.FindAsync(id);
        if (entity is null) return false;
        db.Failures.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    public Task<int> CountOpenAsync() =>
        db.Failures.CountAsync(f => f.Status == FailureStatus.Open || f.Status == FailureStatus.InProgress);
}
