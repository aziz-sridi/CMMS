using CMMS.Domain.Entities;

namespace CMMS.Services.Interfaces;

public interface ILocationService
{
    Task<List<Location>> GetAllAsync(string? search = null);
    Task<Location?> GetByIdAsync(Guid id);
    Task<Location> CreateAsync(Location location);
    Task<bool> UpdateAsync(Location location);
    Task<bool> DeleteAsync(Guid id);
}
