using CMMS.Domain.Entities;

namespace CMMS.Services.Interfaces;

public interface ISparePartService
{
    Task<List<SparePart>> GetAllAsync(string? search = null);
    Task<SparePart?> GetByIdAsync(Guid id);
    Task<List<SparePart>> GetLowStockAsync(int threshold = 5);
    Task<SparePart> CreateAsync(SparePart part);
    Task<bool> UpdateAsync(SparePart part);
    Task<bool> DeleteAsync(Guid id);
}
