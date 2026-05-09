# CMMS — Leoni Tunisia

A small **Computerized Maintenance Management System** built as a class project for the .NET track. It models the kind of work a maintenance team handles inside a wiring-harness plant (think Leoni's Mateur, Menzel Bourguiba, and Sousse sites): tracking equipment, recording failures, planning interventions, and watching spare-part stock.

The app is intentionally simple — no microservices, no DTO layers, no front-end framework. Just Blazor pages talking to EF Core, with ASP.NET Core Identity for auth.

---

## Stack

| Layer        | Tech                                                       |
|--------------|------------------------------------------------------------|
| Runtime      | .NET 10 / ASP.NET Core 10                                  |
| UI           | Blazor (Static SSR via `MapRazorComponents<App>`)          |
| Data         | EF Core 10 + SQLite (`cmms.db` in project root)            |
| Identity     | ASP.NET Core Identity with custom Blazor login pages       |
| Styling      | Bootstrap 5 + Bootstrap Icons + Inter font (CDN)           |

The project targets `net10.0` and uses **only Blazor `.razor` pages** — no MVC views, no Razor Pages `.cshtml`. Auth form posts go to minimal API endpoints declared in `Program.cs`.

---

## Features

- **Dashboard** — open failures, interventions this month with cost in TND, equipment status breakdown, low-stock spare parts.
- **Equipment** — CRUD, with criticality (1–5), serial number, location, status.
- **Failures** — per-equipment failure log with severity and status.
- **Interventions** — per-failure interventions assigned to a technician, with start/end dates and cost.
- **Technicians** — Managers can create technicians **with login credentials in one step** (technician account gets the `Technician` role automatically).
- **Locations** — hierarchical (Building → Area → Line → Row).
- **Spare parts** — stock tracking with low-stock alert.
- **Users** — Manager can promote/demote any registered user to Manager or Technician.

### Role rules

| Role        | Can do                                                                 |
|-------------|------------------------------------------------------------------------|
| Manager     | Full CRUD on every entity + user role management.                      |
| Technician  | Read-only on every entity; can edit interventions assigned to them.    |
| Anonymous   | Only the sign-in / register pages.                                     |

---

## Running it

Requires the **.NET 10 SDK**.

```bash
# from the project folder
dotnet run --project MaintenanceSystem.csproj
```

The app:

1. Creates `cmms.db` next to the executable if it doesn't exist.
2. Seeds the database with Leoni Tunisia-themed data (plants, equipment, technicians, failures, interventions, spare parts).
3. Listens on the URL printed in the console (typically `http://localhost:5xxx`).

### Default accounts (seeded on first run)

| Role        | Email                          | Password    |
|-------------|--------------------------------|-------------|
| Manager     | `admin@leoni.tn`               | `Admin123!` |
| Technician  | `mohamed.trabelsi@leoni.tn`    | `Tech123!`  |
| Technician  | `amina.jelassi@leoni.tn`       | `Tech123!`  |
| Technician  | `karim.bouazizi@leoni.tn`      | `Tech123!`  |
| Technician  | `sonia.gharbi@leoni.tn`        | `Tech123!`  |
| Technician  | `youssef.mansouri@leoni.tn`    | `Tech123!`  |
| Technician  | `nadia.hammami@leoni.tn`       | `Tech123!`  |

To reset all data, stop the app and delete `cmms.db` — it will be recreated on next launch.

---

## Project layout

```
CMMS/
├── Components/                     # Blazor UI (Static SSR)
│   ├── App.razor                   # HTML shell + global styles
│   ├── Routes.razor
│   ├── Layout/MainLayout.razor
│   └── Pages/
│       ├── Home.razor              # Dashboard with KPIs
│       ├── Equipment/              # CRUD pages
│       ├── Failures/
│       ├── Interventions/
│       ├── Locations/
│       ├── SpareParts/
│       ├── Technicians/
│       ├── Users/                  # Role management (Manager only)
│       └── Account/                # Login, Register, AccessDenied
├── Infrastructure/
│   └── Persistence/
│       ├── AppDbContext.cs         # IdentityDbContext + DbSets + relations
│       └── DbSeeder.cs             # Seeds roles, admin, Leoni data
├── Models/                         # Domain entities
│   ├── ApplicationUser.cs
│   ├── Equipment.cs
│   ├── Failure.cs
│   ├── Intervention.cs
│   ├── Location.cs
│   ├── Notification.cs
│   ├── SparePart.cs
│   └── Technician.cs
├── Program.cs                      # Composition root + auth endpoints
└── appsettings.json
```

---

## Entity model

```
Location (self-referencing: Building → Area → Line → Row)
   └── Equipment
        ├── Failure
        │     └── Intervention ── Technician ── Notification
        └── SparePart

ApplicationUser : IdentityUser<Guid>  (separate from Technician — see below)
```

`Technician` and `ApplicationUser` are two **separate** entities:

- `ApplicationUser` is the identity used to sign in.
- `Technician` is the domain entity that gets assigned to interventions.

When a manager creates a technician through the UI, the app creates **both** records and links them by email. This keeps the domain model clean while still allowing the technician to log in.

---

## UI

The UI is deliberately minimal — flat surfaces, subtle borders, no gradients, no big colored hero cards. Bootstrap 5 is used for the grid and form primitives, but most colors are overridden in `Components/App.razor` to a small neutral palette built around `#1f2937` (charcoal) plus a few status colors. The Inter font is loaded from Google Fonts.

---

## Notes & known limitations

- **SQLite only.** No migrations — the app uses `EnsureCreated()` so changing a model requires deleting `cmms.db`.
- **No anti-forgery on auth endpoints.** Acceptable for a class project; the rest of the app uses Blazor's standard anti-forgery.
- **No background jobs.** Notifications are seeded but not raised in real time.
- The `Technician` entity has a unique-email index. If you create a technician with an email already used by *any* `ApplicationUser`, the create page surfaces the conflict before writing.

---

## Authors

- **aziz** (`medazizsridi@gmail.com`)
- **Mariem Bouhlel** (`mariem.bouhlel@polytechnicien.tn`)

Class project — INSAT, .NET module.
