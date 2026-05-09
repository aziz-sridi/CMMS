using CMMS.Domain.Entities;
using CMMS.Infrastructure.Persistence;
using CMMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Services.Implementations;

public class InterventionService(AppDbContext db) : IInterventionService
{
    public async Task<List<Intervention>> GetAllAsync(string? search = null)
    {
        var query = db.Interventions
            .Include(i => i.Technician)
            .Include(i => i.Failure).ThenInclude(f => f.Equipment)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(i =>
                i.Technician.Name.Contains(search) ||
                i.Failure.Equipment.Name.Contains(search) ||
                i.Notes.Contains(search));

        return await query.OrderByDescending(i => i.StartDate).ToListAsync();
    }

    public Task<Intervention?> GetByIdAsync(Guid id) =>
        db.Interventions
            .Include(i => i.Technician)
            .Include(i => i.Failure).ThenInclude(f => f.Equipment)
            .FirstOrDefaultAsync(i => i.Id == id);

    public Task<List<Intervention>> GetByTechnicianEmailAsync(string email) =>
        db.Interventions
            .Include(i => i.Technician)
            .Include(i => i.Failure).ThenInclude(f => f.Equipment)
            .Where(i => i.Technician.Email == email)
            .OrderByDescending(i => i.StartDate)
            .ToListAsync();

    public Task<List<Intervention>> GetInMonthAsync(int year, int month) =>
        db.Interventions
            .Include(i => i.Technician)
            .Include(i => i.Failure).ThenInclude(f => f.Equipment)
            .Where(i => i.StartDate.Year == year && i.StartDate.Month == month)
            .ToListAsync();

    public async Task<Intervention> CreateAsync(Intervention intervention)
    {
        if (intervention.Id == Guid.Empty) intervention.Id = Guid.NewGuid();
        db.Interventions.Add(intervention);
        await db.SaveChangesAsync();
        return intervention;
    }

    public async Task<bool> UpdateAsync(Intervention intervention)
    {
        var existing = await db.Interventions.FirstOrDefaultAsync(i => i.Id == intervention.Id);
        if (existing is null) return false;

        existing.FailureId = intervention.FailureId;
        existing.TechnicianId = intervention.TechnicianId;
        existing.StartDate = intervention.StartDate;
        existing.EndDate = intervention.EndDate;
        existing.Cost = intervention.Cost;
        existing.Notes = intervention.Notes;

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await db.Interventions.FindAsync(id);
        if (entity is null) return false;
        db.Interventions.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    public Task<double> GetTotalCostInMonthAsync(int year, int month) =>
        db.Interventions
            .Where(i => i.StartDate.Year == year && i.StartDate.Month == month)
            .SumAsync(i => i.Cost);
}
