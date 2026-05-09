using CMMS.Domain.Entities;
using CMMS.Infrastructure.Persistence;
using CMMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Services.Implementations;

public class LocationService(AppDbContext db) : ILocationService
{
    public async Task<List<Location>> GetAllAsync(string? search = null)
    {
        var query = db.Locations.Include(l => l.ParentLocation).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(l => l.Name.Contains(search));

        return await query.OrderBy(l => l.Name).ToListAsync();
    }

    public Task<Location?> GetByIdAsync(Guid id) =>
        db.Locations
            .Include(l => l.ParentLocation)
            .Include(l => l.Equipments)
            .FirstOrDefaultAsync(l => l.Id == id);

    public async Task<Location> CreateAsync(Location location)
    {
        if (location.Id == Guid.Empty) location.Id = Guid.NewGuid();
        db.Locations.Add(location);
        await db.SaveChangesAsync();
        return location;
    }

    public async Task<bool> UpdateAsync(Location location)
    {
        var existing = await db.Locations.FirstOrDefaultAsync(l => l.Id == location.Id);
        if (existing is null) return false;

        existing.Name = location.Name;
        existing.Type = location.Type;
        existing.ParentLocationId = location.ParentLocationId;

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await db.Locations.FindAsync(id);
        if (entity is null) return false;
        db.Locations.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }
}
