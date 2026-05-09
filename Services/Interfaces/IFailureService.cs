using CMMS.Domain.Entities;

namespace CMMS.Services.Interfaces;

public interface IFailureService
{
    Task<List<Failure>> GetAllAsync(string? search = null, FailureStatus? status = null);
    Task<Failure?> GetByIdAsync(Guid id);
    Task<List<Failure>> GetByEquipmentAsync(Guid equipmentId);
    Task<Failure> CreateAsync(Failure failure);
    Task<bool> UpdateAsync(Failure failure);
    Task<bool> DeleteAsync(Guid id);
    Task<int> CountOpenAsync();
}
