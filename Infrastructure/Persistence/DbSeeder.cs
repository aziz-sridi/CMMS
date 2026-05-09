using CMMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db = services.GetRequiredService<AppDbContext>();

        foreach (var role in new[] { "Manager", "Technician" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }

        const string adminEmail = "admin@leoni.tn";
        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Sami Ben Ali",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(admin, "Admin123!");
            await userManager.AddToRoleAsync(admin, "Manager");
        }

        if (await db.Locations.AnyAsync()) return;

        // Locations — Leoni Tunisia plants & lines
        var mateur = new Location { Id = Guid.NewGuid(), Name = "Leoni Mateur Plant", Type = LocationType.Building };
        var mateurFloor = new Location { Id = Guid.NewGuid(), Name = "Mateur — Production Floor A", Type = LocationType.Area, ParentLocationId = mateur.Id };
        var mateurLine1 = new Location { Id = Guid.NewGuid(), Name = "Mateur — Line 1 (Renault)", Type = LocationType.Line, ParentLocationId = mateurFloor.Id };
        var mateurLine2 = new Location { Id = Guid.NewGuid(), Name = "Mateur — Line 2 (Peugeot)", Type = LocationType.Line, ParentLocationId = mateurFloor.Id };

        var mb = new Location { Id = Guid.NewGuid(), Name = "Leoni Menzel Bourguiba Plant", Type = LocationType.Building };
        var mbFloor = new Location { Id = Guid.NewGuid(), Name = "Menzel Bourguiba — Floor B", Type = LocationType.Area, ParentLocationId = mb.Id };
        var mbLine = new Location { Id = Guid.NewGuid(), Name = "MB — Line 3 (BMW)", Type = LocationType.Line, ParentLocationId = mbFloor.Id };

        var sousse = new Location { Id = Guid.NewGuid(), Name = "Leoni Sousse Plant", Type = LocationType.Building };
        var sousseLine = new Location { Id = Guid.NewGuid(), Name = "Sousse — Line 4 (VW)", Type = LocationType.Line, ParentLocationId = sousse.Id };

        db.Locations.AddRange(mateur, mateurFloor, mateurLine1, mateurLine2, mb, mbFloor, mbLine, sousse, sousseLine);

        // Technicians — Tunisian names (also create matching login accounts)
        var techSeed = new[]
        {
            new { Name = "Mohamed Trabelsi",  Specialty = "Electrical",    Phone = "+216 22 345 678", Email = "mohamed.trabelsi@leoni.tn"  },
            new { Name = "Amina Jelassi",     Specialty = "Mechanical",    Phone = "+216 24 112 334", Email = "amina.jelassi@leoni.tn"     },
            new { Name = "Karim Bouazizi",    Specialty = "Hydraulics",    Phone = "+216 98 776 221", Email = "karim.bouazizi@leoni.tn"    },
            new { Name = "Sonia Gharbi",      Specialty = "Automation",    Phone = "+216 55 443 109", Email = "sonia.gharbi@leoni.tn"      },
            new { Name = "Youssef Mansouri",  Specialty = "Pneumatics",    Phone = "+216 27 889 002", Email = "youssef.mansouri@leoni.tn"  },
            new { Name = "Nadia Hammami",     Specialty = "Quality / PLC", Phone = "+216 50 221 887", Email = "nadia.hammami@leoni.tn"     }
        };

        var techs = new List<Technician>();
        foreach (var t in techSeed)
        {
            var tech = new Technician
            {
                Id = Guid.NewGuid(),
                Name = t.Name,
                Specialty = t.Specialty,
                Phone = t.Phone,
                Email = t.Email
            };
            techs.Add(tech);

            if (await userManager.FindByEmailAsync(t.Email) is null)
            {
                var u = new ApplicationUser
                {
                    UserName = t.Email,
                    Email = t.Email,
                    FullName = t.Name,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(u, "Tech123!");
                await userManager.AddToRoleAsync(u, "Technician");
            }
        }
        db.Technicians.AddRange(techs);

        // Equipment — cabling / harness machines typical at Leoni
        Equipment Eq(string name, string serial, int crit, EquipmentStatus status, Location loc, DateTime purchased, double cost)
            => new()
            {
                Id = Guid.NewGuid(),
                Name = name,
                SerialNumber = serial,
                Criticality = crit,
                Status = status,
                LocationId = loc.Id,
                PurchaseDate = DateTime.SpecifyKind(purchased, DateTimeKind.Utc),
                PurchaseCost = cost,
                ExpectedLifetimeMonths = 120,
                CreatedAt = DateTime.UtcNow
            };

        var eq1 = Eq("Komax Alpha 488S — Cut & Strip",     "KMX-488-001", 5, EquipmentStatus.Active,           mateurLine1, new DateTime(2021, 03, 15), 78500);
        var eq2 = Eq("Schleuniger CrimpCenter 67",          "SCH-CC67-014", 4, EquipmentStatus.UnderMaintenance, mateurLine1, new DateTime(2020, 11, 02), 62000);
        var eq3 = Eq("TE MK-III Crimp Press",                "TE-MK3-007",   4, EquipmentStatus.Active,           mateurLine2, new DateTime(2019, 06, 10), 45000);
        var eq4 = Eq("Zoller Smile Inspection Bench",        "ZLR-SM-002",   3, EquipmentStatus.Active,           mbLine,      new DateTime(2022, 01, 18), 30000);
        var eq5 = Eq("Atlas Copco GA 75 Compressor",         "AC-GA75-011",  5, EquipmentStatus.Active,           mb,          new DateTime(2018, 09, 25), 28000);
        var eq6 = Eq("Komax Gamma 333 PC",                   "KMX-G333-021", 5, EquipmentStatus.OutOfService,     sousseLine,  new DateTime(2017, 04, 09), 91000);
        var eq7 = Eq("Mecal MS-25 Crimping Machine",         "MCL-MS25-005", 3, EquipmentStatus.Active,           sousseLine,  new DateTime(2021, 12, 03), 22000);
        var eq8 = Eq("Schleuniger UniStrip 2700",            "SCH-US27-009", 2, EquipmentStatus.Active,           mateurLine2, new DateTime(2023, 02, 21), 15000);

        db.Equipments.AddRange(eq1, eq2, eq3, eq4, eq5, eq6, eq7, eq8);

        // Failures
        var now = DateTime.UtcNow;
        var f1 = new Failure { Id = Guid.NewGuid(), EquipmentId = eq2.Id, Description = "Crimp force out of tolerance on lot 4488",   ReportDate = now.AddDays(-9),  Severity = FailureSeverity.High,   Status = FailureStatus.InProgress };
        var f2 = new Failure { Id = Guid.NewGuid(), EquipmentId = eq6.Id, Description = "Servo drive fault E-301, machine offline",   ReportDate = now.AddDays(-21), Severity = FailureSeverity.High,   Status = FailureStatus.Open       };
        var f3 = new Failure { Id = Guid.NewGuid(), EquipmentId = eq1.Id, Description = "Blade wear — strip length drift +0.3 mm",     ReportDate = now.AddDays(-4),  Severity = FailureSeverity.Medium, Status = FailureStatus.Open       };
        var f4 = new Failure { Id = Guid.NewGuid(), EquipmentId = eq5.Id, Description = "Compressor oil temperature alarm intermittent", ReportDate = now.AddDays(-14), Severity = FailureSeverity.Medium, Status = FailureStatus.Closed     };
        var f5 = new Failure { Id = Guid.NewGuid(), EquipmentId = eq3.Id, Description = "Press cycle counter resets at 9999",          ReportDate = now.AddDays(-2),  Severity = FailureSeverity.Low,    Status = FailureStatus.Open       };
        var f6 = new Failure { Id = Guid.NewGuid(), EquipmentId = eq7.Id, Description = "Air leak near applicator head",                ReportDate = now.AddDays(-30), Severity = FailureSeverity.Low,    Status = FailureStatus.Closed     };

        db.Failures.AddRange(f1, f2, f3, f4, f5, f6);

        // Interventions
        Intervention It(Failure f, Technician t, int startDaysAgo, int? endDaysAgo, double cost, string notes)
            => new()
            {
                Id = Guid.NewGuid(),
                FailureId = f.Id,
                TechnicianId = t.Id,
                StartDate = DateTime.SpecifyKind(now.AddDays(-startDaysAgo), DateTimeKind.Utc),
                EndDate = endDaysAgo.HasValue ? DateTime.SpecifyKind(now.AddDays(-endDaysAgo.Value), DateTimeKind.Utc) : null,
                Cost = cost,
                Notes = notes
            };

        db.Interventions.AddRange(
            It(f1, techs[0], 8,  null, 420, "Recalibrated crimp height, awaiting QA sample"),
            It(f2, techs[3], 20, null, 980, "Drive replacement ordered from Stuttgart"),
            It(f3, techs[1], 3,  null, 110, "Replacing blade, monitoring drift"),
            It(f4, techs[4], 13, 12,   260, "Cleaned oil cooler, alarm cleared"),
            It(f5, techs[2], 1,  null, 60,  "Firmware patch scheduled with maker"),
            It(f6, techs[5], 28, 27,   145, "Replaced gasket on applicator")
        );

        // Spare parts
        db.SpareParts.AddRange(
            new SparePart { Id = Guid.NewGuid(), Name = "Crimp Applicator Tip 1.5mm",  PartNumber = "AP-15-TN", Quantity = 12, EquipmentId = eq2.Id },
            new SparePart { Id = Guid.NewGuid(), Name = "Strip Blade Set",             PartNumber = "BL-KMX-488", Quantity = 3, EquipmentId = eq1.Id },
            new SparePart { Id = Guid.NewGuid(), Name = "Servo Drive Module",          PartNumber = "SRV-G333",   Quantity = 1, EquipmentId = eq6.Id },
            new SparePart { Id = Guid.NewGuid(), Name = "Compressor Oil Filter",       PartNumber = "FLT-GA75",   Quantity = 4, EquipmentId = eq5.Id },
            new SparePart { Id = Guid.NewGuid(), Name = "Pneumatic Hose 8mm (per m)",  PartNumber = "PN-H8",      Quantity = 22, EquipmentId = null },
            new SparePart { Id = Guid.NewGuid(), Name = "Inspection Camera Lens",      PartNumber = "ZLR-LN-02",  Quantity = 0, EquipmentId = eq4.Id },
            new SparePart { Id = Guid.NewGuid(), Name = "Crimp Die — TE MK-III",       PartNumber = "DIE-MK3",    Quantity = 2, EquipmentId = eq3.Id }
        );

        // Notifications
        db.Notifications.AddRange(
            new Notification { Id = Guid.NewGuid(), Message = "High severity failure reported on Komax Gamma 333 PC", CreatedAt = now.AddDays(-21), IsRead = false, TechnicianId = techs[3].Id },
            new Notification { Id = Guid.NewGuid(), Message = "Low stock: Servo Drive Module (qty 1)",                CreatedAt = now.AddDays(-7),  IsRead = false, TechnicianId = null },
            new Notification { Id = Guid.NewGuid(), Message = "Out of stock: Inspection Camera Lens",                  CreatedAt = now.AddDays(-1),  IsRead = false, TechnicianId = null }
        );

        await db.SaveChangesAsync();
    }
}
