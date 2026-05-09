using CMMS.Domain.Entities;

namespace CMMS.Services.Interfaces;

public interface IEquipmentService
{
    Task<List<Equipment>> GetAllAsync(string? search = null);
    Task<Equipment?> GetByIdAsync(Guid id);
    Task<Equipment> CreateAsync(Equipment equipment);
    Task<bool> UpdateAsync(Equipment equipment);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> SerialNumberExistsAsync(string serialNumber, Guid? excludeId = null);
    Task<Dictionary<EquipmentStatus, int>> GetStatusBreakdownAsync();
}
