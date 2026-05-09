# CMMS Project Overview (Code-Oriented)

This document is written for a code review or class defense. It focuses on how the system is built, where key logic lives, and answers likely questions.

## Architecture in 60 seconds

- ASP.NET Core 10 minimal hosting in `Program.cs`.
- Blazor static SSR pages for UI under `Components/Pages`.
- EF Core 10 + SQLite for persistence (single `cmms.db` file).
- Identity for auth, with custom Blazor login/register pages and minimal API POST endpoints.

## How the app boots

1. `Program.cs` configures `AppDbContext`, Identity, cookies, auth.
2. It calls `db.Database.EnsureCreated()` then `DbSeeder.SeedAsync(...)`.
3. Blazor components are mapped with `MapRazorComponents<App>()`.

## Models (what they represent)

- `ApplicationUser` (identity user) adds `FullName` to the Identity user object.
- `Equipment` is the main asset (serial, criticality, status, purchase info).
- `Failure` is a report tied to equipment (severity, status, report date).
- `Intervention` is a repair action for a failure (technician, dates, cost, notes).
- `Technician` is a domain entity (name, specialty, email, phone).
- `Location` is hierarchical (Building -> Area -> Line -> Row).
- `SparePart` is stock tied to equipment or general inventory.
- `Notification` is a simple message (optional technician link).

Important modeling choice:
`Technician` and `ApplicationUser` are separate. `ApplicationUser` is for login, while `Technician` is the domain entity used by interventions. The UI creates both records and links them by email.

## Data relationships (EF Core)

Defined in `Infrastructure/Persistence/AppDbContext.cs`:

- `Location` has a self-reference (`ParentLocationId`) with `DeleteBehavior.Restrict`.
- `Equipment` -> `Location` is required and uses `Restrict`.
- `Failure` -> `Equipment` is required and cascades delete.
- `Intervention` -> `Failure` is required and cascades delete.
- `Intervention` -> `Technician` is required and uses `Restrict`.
- `Notification` -> `Technician` is optional and uses `SetNull`.
- `SparePart` -> `Equipment` is optional and uses `SetNull`.
- Unique indexes: `Equipment.SerialNumber`, `Technician.Email`.

## CRUD flow (how we did it)

The CRUD pattern is consistent across entities:

1. **List page**: loads data with EF Core (often `Include(...)` for navigation props), renders table.
2. **Create page**: form binds to a new model, on submit it calls `db.Add(...)` + `SaveChangesAsync()`.
3. **Edit page**: loads by ID, updates tracked entity, then `SaveChangesAsync()`.
4. **Delete page**: confirmation UI, then `db.Remove(...)` + `SaveChangesAsync()`.

All CRUD pages are in `Components/Pages/<Entity>/` and are plain Razor components (no MVC controllers, no API layer).

## Search bar (where it is and how it works)

Search is implemented as a **GET form** with a query string, then filtered in `OnInitializedAsync()` using `Contains(...)`. The parameter is bound via `[SupplyParameterFromQuery]`.

Files that contain search bars:

- `Components/Pages/Equipment/EquipmentList.razor` (search by name or serial).
- `Components/Pages/Failures/FailureList.razor` (search + status filter).
- `Components/Pages/Interventions/InterventionList.razor` (search by technician, equipment, notes).
- `Components/Pages/Locations/LocationList.razor` (search by name).
- `Components/Pages/Technicians/TechnicianList.razor` (search by name, specialty, email).
- `Components/Pages/SpareParts/SparePartList.razor` (search by name or part number).

Example pattern (from Equipment list):

```csharp
[SupplyParameterFromQuery]
public string? Search { get; set; }

protected override async Task OnInitializedAsync()
{
    var query = db.Equipments.Include(e => e.Location).AsQueryable();
    if (!string.IsNullOrWhiteSpace(Search))
        query = query.Where(e => e.Name.Contains(Search) || e.SerialNumber.Contains(Search));
    equipment = await query.OrderBy(e => e.Name).ToListAsync();
}
```

## Auth and roles (where it lives)

- Identity setup and cookie paths in `Program.cs`.
- Login, logout, register, set-role are implemented as minimal API POST endpoints in `Program.cs`.
- UI uses `AuthorizeView` and `[Authorize]` to restrict pages.

## Seeding strategy

`Infrastructure/Persistence/DbSeeder.cs` seeds:

- Roles (`Manager`, `Technician`).
- Admin user and technician accounts.
- Locations, equipment, failures, interventions, spare parts, notifications.

On first run, `EnsureCreated()` makes the database and then the seeder fills it. To reset data, delete `cmms.db`.

## Likely class questions (and short answers)

**Q: Why split `Technician` and `ApplicationUser`?**
Because auth and domain are different concerns. `ApplicationUser` is the login identity, while `Technician` is used for maintenance operations. This keeps the domain model clean and still allows technicians to log in.

**Q: Where are the CRUD operations?**
In Blazor pages under `Components/Pages/<Entity>/` (list, create, edit, delete components). They directly use EF Core via injected `AppDbContext`.

**Q: How does search work?**
Each list page uses a GET form and query string binding (`[SupplyParameterFromQuery]`). Filtering is done in `OnInitializedAsync()` with `Contains(...)` on the relevant columns.

**Q: Why no controllers or APIs?**
This is a Blazor SSR project. Pages are Razor components and interact directly with EF Core in the same app for simplicity.

**Q: Why `EnsureCreated()` and not migrations?**
Fast setup for a class project. If the model changes, we delete `cmms.db` and re-run to recreate it.

**Q: How is authorization enforced?**
Pages are marked with `[Authorize]`. The UI shows or hides actions via `AuthorizeView` and role checks. The identity system handles cookies.

**Q: Where is the database configured?**
`appsettings.json` stores the SQLite connection string, and `Program.cs` wires it to `UseSqlite(...)`.

**Q: What about role-based access to edit interventions?**
In `InterventionList.razor`, a technician can edit only their own interventions; managers can edit/delete all.

## Run commands

```bash
dotnet run --project MaintenanceSystem.csproj
```

This launches the app and seeds the database on first run.
