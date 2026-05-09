using CMMS.Domain.Entities;
using CMMS.Infrastructure.Persistence;
using CMMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Services.Implementations;

public class EquipmentService(AppDbContext db) : IEquipmentService
{
    public async Task<List<Equipment>> GetAllAsync(string? search = null)
    {
        var query = db.Equipments.Include(e => e.Location).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(e => e.Name.Contains(search) || e.SerialNumber.Contains(search));

        return await query.OrderBy(e => e.Name).ToListAsync();
    }

    public Task<Equipment?> GetByIdAsync(Guid id) =>
        db.Equipments
            .Include(e => e.Location)
            .Include(e => e.Failures)
            .Include(e => e.SpareParts)
            .FirstOrDefaultAsync(e => e.Id == id);

    public async Task<Equipment> CreateAsync(Equipment equipment)
    {
        if (equipment.Id == Guid.Empty) equipment.Id = Guid.NewGuid();
        equipment.CreatedAt = DateTime.UtcNow;
        db.Equipments.Add(equipment);
        await db.SaveChangesAsync();
        return equipment;
    }

    public async Task<bool> UpdateAsync(Equipment equipment)
    {
        var existing = await db.Equipments.FirstOrDefaultAsync(e => e.Id == equipment.Id);
        if (existing is null) return false;

        existing.Name = equipment.Name;
        existing.SerialNumber = equipment.SerialNumber;
        existing.Criticality = equipment.Criticality;
        existing.PurchaseDate = equipment.PurchaseDate;
        existing.PurchaseCost = equipment.PurchaseCost;
        existing.ExpectedLifetimeMonths = equipment.ExpectedLifetimeMonths;
        existing.Status = equipment.Status;
        existing.LocationId = equipment.LocationId;
        existing.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await db.Equipments.FindAsync(id);
        if (entity is null) return false;
        db.Equipments.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    public Task<bool> SerialNumberExistsAsync(string serialNumber, Guid? excludeId = null)
    {
        var normalized = serialNumber.Trim();
        return db.Equipments.AnyAsync(e =>
            e.SerialNumber == normalized &&
            (excludeId == null || e.Id != excludeId));
    }

    public async Task<Dictionary<EquipmentStatus, int>> GetStatusBreakdownAsync()
    {
        return await db.Equipments
            .GroupBy(e => e.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);
    }
}
