using CMMS.Domain.Entities;
using CMMS.Infrastructure.Persistence;
using CMMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Services.Implementations;

public class SparePartService(AppDbContext db) : ISparePartService
{
    public async Task<List<SparePart>> GetAllAsync(string? search = null)
    {
        var query = db.SpareParts.Include(s => s.Equipment).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(s => s.Name.Contains(search) || s.PartNumber.Contains(search));

        return await query.OrderBy(s => s.Name).ToListAsync();
    }

    public Task<SparePart?> GetByIdAsync(Guid id) =>
        db.SpareParts
            .Include(s => s.Equipment)
            .FirstOrDefaultAsync(s => s.Id == id);

    public Task<List<SparePart>> GetLowStockAsync(int threshold = 5) =>
        db.SpareParts
            .Include(s => s.Equipment)
            .Where(s => s.Quantity < threshold)
            .OrderBy(s => s.Quantity)
            .ToListAsync();

    public async Task<SparePart> CreateAsync(SparePart part)
    {
        if (part.Id == Guid.Empty) part.Id = Guid.NewGuid();
        db.SpareParts.Add(part);
        await db.SaveChangesAsync();
        return part;
    }

    public async Task<bool> UpdateAsync(SparePart part)
    {
        var existing = await db.SpareParts.FirstOrDefaultAsync(s => s.Id == part.Id);
        if (existing is null) return false;

        existing.Name = part.Name;
        existing.PartNumber = part.PartNumber;
        existing.Quantity = part.Quantity;
        existing.EquipmentId = part.EquipmentId;

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await db.SpareParts.FindAsync(id);
        if (entity is null) return false;
        db.SpareParts.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }
}
