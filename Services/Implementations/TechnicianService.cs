using CMMS.Domain.Entities;
using CMMS.Infrastructure.Persistence;
using CMMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Services.Implementations;

public class TechnicianService(AppDbContext db) : ITechnicianService
{
    public async Task<List<Technician>> GetAllAsync(string? search = null)
    {
        var query = db.Technicians.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t =>
                t.Name.Contains(search) ||
                t.Email.Contains(search) ||
                t.Specialty.Contains(search));

        return await query.OrderBy(t => t.Name).ToListAsync();
    }

    public Task<Technician?> GetByIdAsync(Guid id) =>
        db.Technicians.FirstOrDefaultAsync(t => t.Id == id);

    public Task<Technician?> GetByEmailAsync(string email) =>
        db.Technicians.FirstOrDefaultAsync(t => t.Email == email);

    public async Task<Technician> CreateAsync(Technician technician)
    {
        if (technician.Id == Guid.Empty) technician.Id = Guid.NewGuid();
        db.Technicians.Add(technician);
        await db.SaveChangesAsync();
        return technician;
    }

    public async Task<bool> UpdateAsync(Technician technician)
    {
        var existing = await db.Technicians.FirstOrDefaultAsync(t => t.Id == technician.Id);
        if (existing is null) return false;

        existing.Name = technician.Name;
        existing.Specialty = technician.Specialty;
        existing.Phone = technician.Phone;
        existing.Email = technician.Email;

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await db.Technicians.FindAsync(id);
        if (entity is null) return false;
        db.Technicians.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    public Task<bool> EmailExistsAsync(string email, Guid? excludeId = null)
    {
        var normalized = email.Trim();
        return db.Technicians.AnyAsync(t =>
            t.Email == normalized &&
            (excludeId == null || t.Id != excludeId));
    }

    public Task<bool> HasInterventionsAsync(Guid id) =>
        db.Interventions.AnyAsync(i => i.TechnicianId == id);
}
