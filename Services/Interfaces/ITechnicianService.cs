using CMMS.Domain.Entities;

namespace CMMS.Services.Interfaces;

public interface ITechnicianService
{
    Task<List<Technician>> GetAllAsync(string? search = null);
    Task<Technician?> GetByIdAsync(Guid id);
    Task<Technician?> GetByEmailAsync(string email);
    Task<Technician> CreateAsync(Technician technician);
    Task<bool> UpdateAsync(Technician technician);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> EmailExistsAsync(string email, Guid? excludeId = null);
    Task<bool> HasInterventionsAsync(Guid id);
}
