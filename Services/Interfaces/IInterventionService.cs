using CMMS.Domain.Entities;

namespace CMMS.Services.Interfaces;

public interface IInterventionService
{
    Task<List<Intervention>> GetAllAsync(string? search = null);
    Task<Intervention?> GetByIdAsync(Guid id);
    Task<List<Intervention>> GetByTechnicianEmailAsync(string email);
    Task<List<Intervention>> GetInMonthAsync(int year, int month);
    Task<Intervention> CreateAsync(Intervention intervention);
    Task<bool> UpdateAsync(Intervention intervention);
    Task<bool> DeleteAsync(Guid id);
    Task<double> GetTotalCostInMonthAsync(int year, int month);
}
