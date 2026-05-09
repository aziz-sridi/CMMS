using CMMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Equipment> Equipments => Set<Equipment>();
    public DbSet<Failure> Failures => Set<Failure>();
    public DbSet<Intervention> Interventions => Set<Intervention>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<SparePart> SpareParts => Set<SparePart>();
    public DbSet<Technician> Technicians => Set<Technician>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Location>()
            .HasOne(l => l.ParentLocation)
            .WithMany()
            .HasForeignKey(l => l.ParentLocationId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Equipment>()
            .HasIndex(e => e.SerialNumber).IsUnique();

        builder.Entity<Equipment>()
            .HasOne(e => e.Location)
            .WithMany(l => l.Equipments)
            .HasForeignKey(e => e.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Failure>()
            .HasOne(f => f.Equipment)
            .WithMany(e => e.Failures)
            .HasForeignKey(f => f.EquipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Intervention>()
            .HasOne(i => i.Failure)
            .WithMany(f => f.Interventions)
            .HasForeignKey(i => i.FailureId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Intervention>()
            .HasOne(i => i.Technician)
            .WithMany(t => t.Interventions)
            .HasForeignKey(i => i.TechnicianId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Notification>()
            .HasOne(n => n.Technician)
            .WithMany(t => t.Notifications)
            .HasForeignKey(n => n.TechnicianId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<SparePart>()
            .HasOne(s => s.Equipment)
            .WithMany(e => e.SpareParts)
            .HasForeignKey(s => s.EquipmentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Technician>()
            .HasIndex(t => t.Email).IsUnique();
    }
}
