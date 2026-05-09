# CMMS — Project Review Q&A

Prep document for the oral review. Each question is followed by a short answer and the file(s) that prove it.

---

## 1. Project framing

### Q1.1 — What does this application do?
It is a **CMMS** (Computerized Maintenance Management System) for an industrial site (modeled on Leoni Tunisia plants). It tracks **equipment** in a hierarchical **location** structure, records **failures** reported against that equipment, manages the **interventions** that technicians perform to fix those failures, and keeps an inventory of **spare parts** with low-stock alerts. A dashboard surfaces the operational KPIs.

- Dashboard KPIs: [Components/Pages/Home.razor](Components/Pages/Home.razor)
- Seeded business data (Leoni plants, Tunisian technicians, harness-machine equipment): [Infrastructure/Persistence/DbSeeder.cs](Infrastructure/Persistence/DbSeeder.cs)

### Q1.2 — Why Blazor SSR and not MVC / Razor Pages?
Blazor Server-Side Rendering gives us a single component model (`.razor`) for both UI rendering and form handling, without shipping a SignalR pipeline or WebAssembly bundle. It matches a CRUD app well: each page is one component, server-rendered on every navigation. We deliberately did **not** mix in `.cshtml` Razor Pages — the auth UI is also Blazor.

- Entry point and component mapping: [Program.cs:35](Program.cs#L35), [Program.cs:66](Program.cs#L66)
- Root component: [Components/App.razor](Components/App.razor)
- No `.cshtml` exists anywhere in the project.

---

## 2. Architecture

### Q2.1 — Walk me through the layers.
```
Models/                       ← Domain entities (POCOs)
Infrastructure/Persistence/   ← AppDbContext + DbSeeder
Services/Interfaces/          ← One interface per entity
Services/Implementations/     ← EF Core implementations, registered Scoped
Components/Pages/             ← Blazor pages (one folder per entity, CRUD)
Components/Layout/            ← MainLayout (nav, auth-aware menu)
Program.cs                    ← DI wiring + Identity + minimal auth endpoints
```
DI registration of every service: [Program.cs:37-43](Program.cs#L37-L43).

### Q2.2 — Why a service layer if Blazor pages can use the DbContext directly?
Three reasons:
1. **Reusability** — `InterventionService.GetInMonthAsync` is called from both the dashboard and the interventions list.
2. **Testability** — the page depends on an interface, not on EF Core.
3. **Authorization clarity** — domain logic (e.g. "technician can only edit their own interventions") lives in one place we can reason about.

The one exception is [Components/Pages/Users/UserList.razor](Components/Pages/Users/UserList.razor), which injects `AppDbContext` and `UserManager<ApplicationUser>` directly because it manipulates Identity users — that responsibility doesn't belong in a domain service.

### Q2.3 — Why no DTOs?
This is a class project with a single Blazor frontend talking to the same domain model. Adding DTOs would duplicate every entity with no boundary to defend (no external API, no contract versioning). The trade-off is documented honestly: entities flow from the service straight into `EditForm` models.

- Example: the edit page builds a local `InputModel` to bind the form, then maps to `Intervention` before calling the service: [Components/Pages/Interventions/InterventionEdit.razor:115](Components/Pages/Interventions/InterventionEdit.razor#L115).

---

## 3. Data model

### Q3.1 — What are the entities and how are they related?
| Entity | Related to | Cascade rule |
|---|---|---|
| `Location` | self (parent), `Equipment` | Self-FK is **Restrict** (no orphaning a sub-location) |
| `Equipment` | `Location`, `Failure`, `SparePart` | `Location → Equipment` is **Restrict** |
| `Failure` | `Equipment`, `Intervention` | `Equipment → Failure` is **Cascade** |
| `Intervention` | `Failure`, `Technician` | `Failure → Intervention` is **Cascade**, `Technician → Intervention` is **Restrict** |
| `SparePart` | `Equipment` (optional) | `Equipment → SparePart` is **SetNull** |
| `Notification` | `Technician` (optional) | **SetNull** |
| `Technician` | `Intervention`, `Notification` | Unique index on `Email` |

All these rules are declared in [Infrastructure/Persistence/AppDbContext.cs:19-73](Infrastructure/Persistence/AppDbContext.cs#L19-L73).

### Q3.2 — Why is `Technician` a separate entity from `ApplicationUser`?
A `Technician` is a **domain concept** (someone who can be assigned interventions), while an `ApplicationUser` is an **authentication concept** (someone who can log in). They are linked by **email** (see `Intervention.Technician.Email == User.Identity.Name` check in [Components/Pages/Interventions/InterventionEdit.razor:86](Components/Pages/Interventions/InterventionEdit.razor#L86)). This lets us:
- Seed technicians without forcing every one of them to have an account.
- Keep the auth schema clean (no domain fields on `IdentityUser`).

- Entity definition: [Models/Technician.cs](Models/Technician.cs)
- Linked accounts created in seed: [Infrastructure/Persistence/DbSeeder.cs:76-87](Infrastructure/Persistence/DbSeeder.cs#L76-L87)

### Q3.3 — Why `Guid` primary keys instead of `int`?
- No collisions when seeding fixed data.
- ASP.NET Identity is also using `Guid` (`IdentityRole<Guid>`, `IdentityUser<Guid>`), so the whole schema is consistent.
- Trade-off acknowledged: slightly larger index pages — not a concern at this scale.

Identity configured with `Guid`: [Infrastructure/Persistence/AppDbContext.cs:9](Infrastructure/Persistence/AppDbContext.cs#L9), [Program.cs:15](Program.cs#L15).

---

## 4. Authentication & Authorization

### Q4.1 — How does login work without `.cshtml` pages?
We use ASP.NET Core Identity for the **storage and password hashing**, but the **UI is custom Blazor**. The login form posts to a minimal endpoint (`/Identity/Account/Login`) defined in [Program.cs:71-86](Program.cs#L71-L86) which calls `SignInManager.PasswordSignInAsync`. Same pattern for Logout, Register, and SetRole.

- We chose `AddIdentity` (not `AddDefaultIdentity`) precisely to avoid the framework looking for `.cshtml` partials: [Program.cs:14-24](Program.cs#L14-L24).
- Custom UI: [Components/Pages/Account/Login.razor](Components/Pages/Account/Login.razor), [Register.razor](Components/Pages/Account/Register.razor), [AccessDenied.razor](Components/Pages/Account/AccessDenied.razor).

### Q4.2 — What roles exist and what can each do?
| Role | Permissions |
|---|---|
| **Manager** | Full CRUD on every entity + user management (`/users`) |
| **Technician** | Read-only on every entity, **except** they can edit interventions where `Intervention.Technician.Email == their email` |

- Role enforcement at the page level uses `[Authorize(Roles = "Manager")]` and inline `AuthorizeView`. Example mixed view: [Components/Pages/Interventions/InterventionEdit.razor:21-34](Components/Pages/Interventions/InterventionEdit.razor#L21-L34).
- Ownership check on edit: [Components/Pages/Interventions/InterventionEdit.razor:82-88](Components/Pages/Interventions/InterventionEdit.razor#L82-L88).

### Q4.3 — Why is `DisableAntiforgery()` on the auth endpoints?
The login/logout/register endpoints are **minimal API** form posts, not Blazor components — they sit outside the antiforgery pipeline that Blazor sets up. For a class project this is an acceptable simplification; in production we would issue an antiforgery token from the login page and validate it on the endpoint. The user-management form **does** use `<AntiforgeryToken />` because it is rendered inside a Blazor page: [Components/Pages/Users/UserList.razor:36](Components/Pages/Users/UserList.razor#L36).

---

## 5. EF Core & Database

### Q5.1 — Why SQLite?
Zero setup for evaluation — the database file (`cmms.db`) is created on first run via `db.Database.EnsureCreated()` ([Program.cs:50](Program.cs#L50)), and the seed runs on every startup if the `Locations` table is empty ([Infrastructure/Persistence/DbSeeder.cs:35](Infrastructure/Persistence/DbSeeder.cs#L35)). Switching to SQL Server is a one-line change in `Program.cs` (`UseSqlServer` instead of `UseSqlite`).

### Q5.2 — Why `EnsureCreated()` instead of migrations?
For a class project the schema is finalized before the first run, so we don't need the migration history. If we needed to ship updates to an existing database we'd switch to `Database.Migrate()` and add an EF Core migrations folder. Trade-off is explicit, not accidental.

### Q5.3 — Walk me through a typical CRUD operation.
Take "edit an intervention":
1. User navigates to `/interventions/edit/{id}` → [Components/Pages/Interventions/InterventionEdit.razor](Components/Pages/Interventions/InterventionEdit.razor).
2. `OnInitializedAsync` calls `InterventionService.GetByIdAsync(Id)` which `Include`s `Technician`, `Failure`, and `Failure.Equipment`: [Services/Implementations/InterventionService.cs:26-30](Services/Implementations/InterventionService.cs#L26-L30).
3. The page builds an `InputModel` (a flat form-binding class) from the entity.
4. On submit, the page validates the date range, maps `InputModel` back to an `Intervention`, and calls `InterventionService.UpdateAsync`.
5. The service loads the existing tracked entity, copies the editable fields, and calls `SaveChangesAsync`: [Services/Implementations/InterventionService.cs:55-69](Services/Implementations/InterventionService.cs#L55-L69).

### Q5.4 — Why load the existing entity in `UpdateAsync` instead of attaching the incoming one?
Two reasons:
1. We only want to update **editable** columns (`FailureId`, `TechnicianId`, dates, cost, notes) — not whatever the client sent for the rest.
2. Attaching a detached entity with `db.Update(...)` would mark every property as modified, including any nav properties EF might lazy-shadow, which has been a source of subtle bugs.

The "load then copy" pattern at [Services/Implementations/InterventionService.cs:57-67](Services/Implementations/InterventionService.cs#L57-L67) is explicit and safe.

---

## 6. Specific design decisions a reviewer might probe

### Q6.1 — Why is the user-management page the only one using `AppDbContext` directly?
Because it manages **Identity users**, not domain entities. Wrapping `UserManager<ApplicationUser>` in a custom service would add an abstraction with no second consumer. We accept the inconsistency because the boundary is clear and documented in code: [Components/Pages/Users/UserList.razor:1-5](Components/Pages/Users/UserList.razor#L1-L5).

### Q6.2 — Why a plain HTML `<form>` in `UserList.razor` instead of `<EditForm>`?
The row contains an `<InputSelect>` bound to an indexer expression (`row.Roles`). Blazor SSR's expression-tree formatter cannot handle indexer-with-runtime-key in `@bind-Value` and throws at render time. Plain HTML `<form>` posting to the `/Identity/Account/SetRole` minimal endpoint sidesteps the limitation entirely: [Components/Pages/Users/UserList.razor:35-44](Components/Pages/Users/UserList.razor#L35-L44), endpoint at [Program.cs:96-107](Program.cs#L96-L107).

### Q6.3 — Why is the dashboard's "this month" computed against `DateTime.UtcNow`?
Because seeded `Intervention.StartDate` and `EndDate` values are explicitly `DateTime.SpecifyKind(..., Utc)` ([Infrastructure/Persistence/DbSeeder.cs:136-137](Infrastructure/Persistence/DbSeeder.cs#L136-L137)) and the form handler also `.ToUniversalTime()`s the input ([Components/Pages/Interventions/InterventionEdit.razor:120-121](Components/Pages/Interventions/InterventionEdit.razor#L120-L121)). Storing everything as UTC avoids the SQLite "kind unspecified" issue and timezone drift.

### Q6.4 — How do you guarantee a serial number is unique?
A unique index on `Equipment.SerialNumber` at the database level: [Infrastructure/Persistence/AppDbContext.cs:31](Infrastructure/Persistence/AppDbContext.cs#L31). A second technician with the same email is also blocked by a unique index ([Infrastructure/Persistence/AppDbContext.cs:72](Infrastructure/Persistence/AppDbContext.cs#L72)). The constraint lives in the schema, not in C# validation — which means concurrent inserts cannot race past it.

### Q6.5 — What happens if I delete a piece of equipment that has open failures?
The cascade rule says `Equipment → Failure` is **Cascade**, so failures and their interventions are removed transactionally. Conversely, deleting a `Location` that still has equipment is **blocked** (`Restrict`) because losing the location would orphan the equipment. The cascade matrix is in [Infrastructure/Persistence/AppDbContext.cs:19-73](Infrastructure/Persistence/AppDbContext.cs#L19-L73).

### Q6.6 — Spare parts can have `EquipmentId = null` — why?
Some parts are generic (e.g. the seeded "Pneumatic Hose 8mm" at [Infrastructure/Persistence/DbSeeder.cs:157](Infrastructure/Persistence/DbSeeder.cs#L157)). When equipment is decommissioned, parts that referenced it are not lost — the FK is set to `null` (`OnDelete(DeleteBehavior.SetNull)` at [Infrastructure/Persistence/AppDbContext.cs:69](Infrastructure/Persistence/AppDbContext.cs#L69)).

---

## 7. Likely live-demo asks

### Q7.1 — Show me the login flow.
1. Run `dotnet run`.
2. Visit `/` → unauthenticated, redirected to `/Identity/Account/Login`.
3. Log in as `admin@leoni.tn` / `Admin123!` (Manager — see seed at [Infrastructure/Persistence/DbSeeder.cs:21-33](Infrastructure/Persistence/DbSeeder.cs#L21-L33)).
4. Dashboard loads with KPIs from real seeded data.

### Q7.2 — Show the role difference.
Log out, log in as a technician: e.g. `mohamed.trabelsi@leoni.tn` / `Tech123!` ([Infrastructure/Persistence/DbSeeder.cs:53-87](Infrastructure/Persistence/DbSeeder.cs#L53-L87)). The Equipment/Failures/Locations pages show no "Add/Edit/Delete" buttons. Open `/interventions` — they can only edit the interventions where they are the assigned technician.

### Q7.3 — Show that the cascade rules actually work.
- Delete a failure with interventions → its interventions disappear (Cascade).
- Try to delete a location that still has equipment → blocked with an FK violation (Restrict).
- Delete a piece of equipment that has spare parts → the spare parts remain, with `EquipmentId = null` (SetNull).

---

## 8. Stack summary (cheat sheet)

| Concern | Choice | Where |
|---|---|---|
| Runtime | .NET 10 | [MaintenanceSystem.csproj](MaintenanceSystem.csproj) |
| UI | Blazor SSR (`.razor` only) | [Components/](Components/) |
| ORM | EF Core 10 | [Infrastructure/Persistence/AppDbContext.cs](Infrastructure/Persistence/AppDbContext.cs) |
| Database | SQLite (`cmms.db`) | [Program.cs:11-12](Program.cs#L11-L12) |
| Auth | ASP.NET Core Identity (`Guid` keys, custom Blazor UI) | [Program.cs:14-24](Program.cs#L14-L24) |
| Styling | Bootstrap 5 + Bootstrap Icons (CDN) | [Components/App.razor](Components/App.razor) |
| Service registration | All Scoped | [Program.cs:37-43](Program.cs#L37-L43) |

---

## 9. Honest weaknesses (be ready, don't dodge)

- **No automated tests.** Out of scope for the deliverable; would start with service-layer integration tests against an in-memory SQLite.
- **`EnsureCreated()` not `Migrate()`.** Fine for a fresh class project; would not survive a schema change to an existing deployed database.
- **Antiforgery disabled on minimal endpoints.** Documented above (Q4.3).
- **No DTOs.** Documented above (Q2.3) — accepted trade-off.
- **Server-side validation only.** No client-side enrichment; acceptable for SSR Blazor.
