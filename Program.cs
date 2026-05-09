using CMMS.Components;
using CMMS.Domain.Entities;
using CMMS.Infrastructure.Persistence;
using CMMS.Services.Implementations;
using CMMS.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=cmms.db"));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole<Guid>>(o =>
    {
        o.Password.RequireDigit = false;
        o.Password.RequiredLength = 6;
        o.Password.RequireNonAlphanumeric = false;
        o.Password.RequireUppercase = false;
        o.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Identity/Account/Login";
    o.LogoutPath = "/Identity/Account/Logout";
    o.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddRazorComponents();

builder.Services.AddScoped<IEquipmentService, EquipmentService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IFailureService, FailureService>();
builder.Services.AddScoped<IInterventionService, InterventionService>();
builder.Services.AddScoped<ISparePartService, SparePartService>();
builder.Services.AddScoped<ITechnicianService, TechnicianService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>();

// Auth POST endpoints (Blazor-only UI; these handle cookie sign-in/out)
var auth = app.MapGroup("/Identity/Account");

auth.MapPost("/Login", async (
    HttpContext ctx,
    SignInManager<ApplicationUser> signInManager,
    [Microsoft.AspNetCore.Mvc.FromForm] string email,
    [Microsoft.AspNetCore.Mvc.FromForm] string password,
    [Microsoft.AspNetCore.Mvc.FromForm] string? returnUrl) =>
{
    var result = await signInManager.PasswordSignInAsync(email, password, isPersistent: true, lockoutOnFailure: false);
    if (!result.Succeeded)
    {
        var url = "/Identity/Account/Login?error=1";
        if (!string.IsNullOrEmpty(returnUrl)) url += $"&ReturnUrl={Uri.EscapeDataString(returnUrl)}";
        return Results.Redirect(url);
    }
    return Results.Redirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
}).DisableAntiforgery();

auth.MapPost("/Logout", async (
    SignInManager<ApplicationUser> signInManager,
    [Microsoft.AspNetCore.Mvc.FromForm] string? returnUrl) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
}).DisableAntiforgery();

auth.MapPost("/SetRole", async (
    UserManager<ApplicationUser> userManager,
    [Microsoft.AspNetCore.Mvc.FromForm] Guid userId,
    [Microsoft.AspNetCore.Mvc.FromForm] string? role) =>
{
    var user = await userManager.FindByIdAsync(userId.ToString());
    if (user is null) return Results.Redirect("/users");
    var current = await userManager.GetRolesAsync(user);
    if (current.Count > 0) await userManager.RemoveFromRolesAsync(user, current);
    if (!string.IsNullOrWhiteSpace(role)) await userManager.AddToRoleAsync(user, role);
    return Results.Redirect("/users");
}).DisableAntiforgery();

auth.MapPost("/Register", async (
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    [Microsoft.AspNetCore.Mvc.FromForm] string email,
    [Microsoft.AspNetCore.Mvc.FromForm] string password,
    [Microsoft.AspNetCore.Mvc.FromForm] string? fullName) =>
{
    var user = new ApplicationUser
    {
        UserName = email,
        Email = email,
        FullName = fullName ?? string.Empty,
        EmailConfirmed = true
    };
    var result = await userManager.CreateAsync(user, password);
    if (!result.Succeeded)
    {
        var msg = Uri.EscapeDataString(string.Join("; ", result.Errors.Select(e => e.Description)));
        return Results.Redirect($"/Identity/Account/Register?error={msg}");
    }
    await signInManager.SignInAsync(user, isPersistent: true);
    return Results.Redirect("/");
}).DisableAntiforgery();

app.Run();
