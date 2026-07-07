using ChatApp.Application.Services;
using ChatApp.Core.DTOs;
using ChatApp.Core.Entities;
using ChatApp.Core.Enums;
using ChatApp.Core.Interfaces.Repositories;
using ChatApp.Core.Interfaces.Services;
using ChatApp.Infrastructure.Identity;
using ChatApp.Infrastructure.Persistence;
using ChatApp.Infrastructure.Persistence.Repositories;
using ChatApp.Infrastructure.SignalR;
using ChatApp.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Identity ──────────────────────────────────────────────────────────────────
builder.Services.AddIdentity<AppUser, IdentityRole<Guid>>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = false;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});

// ── Repositories ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IVideoCallRepository, VideoCallRepository>();

// ── Application services ──────────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IMessageService, MessageService>();

// ── Video call service ────────────────────────────────────────────────────────
// V1: pure WebRTC P2P signaling
// V2: swap this line to: builder.Services.AddScoped<IVideoCallService, LiveKitService>();
builder.Services.AddScoped<IVideoCallService, VideoCallService>();

// ── Blazor + SignalR ──────────────────────────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
});

// ── Blazor scoped state ───────────────────────────────────────────────────────
builder.Services.AddScoped<ChatStateService>();
builder.Services.AddScoped<VideoCallStateService>();

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Seed roles ────────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    await DbSeeder.SeedRolesAsync(roleManager);

    // Auto-migrate in development
    if (app.Environment.IsDevelopment())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// ── SignalR hubs ──────────────────────────────────────────────────────────────
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<VideoHub>("/hubs/video");

app.MapRazorComponents<ChatApp.Web.Components.App>()
   .AddInteractiveServerRenderMode();

// ── Auth Http Request ─────────────────────────────────────────────────────────

app.MapPost("/auth/login", async (
    HttpContext context,
    SignInManager<AppUser> signInManager,
    UserManager<AppUser> userManager,
    [FromForm] string email,
    [FromForm] string password,
    [FromForm] string? returnUrl) =>
{
    var user = await userManager.FindByEmailAsync(email.ToLower().Trim());
    if (user is null)
        return Results.Redirect("/login?error=Invalid email or password");

    var result = await signInManager.PasswordSignInAsync(user, password, isPersistent: true, lockoutOnFailure: false);

    if (!result.Succeeded)
        return Results.Redirect("/login?error=Invalid email or password");

    if (returnUrl == "") returnUrl = "/";

    return Results.Redirect(returnUrl ?? "/");
})
.DisableAntiforgery(); // or properly configure antiforgery for forms

app.MapPost("/auth/logout", async (SignInManager<AppUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/login");
});

app.MapPost("/auth/register", async (
    HttpContext context,
    IAuthService authService,
    [FromForm] string username,
    [FromForm] string displayName,
    [FromForm] string email,
    [FromForm] string password,
    [FromForm] string role) =>
{
    if (!Enum.TryParse<UserRole>(role, ignoreCase: true, out var parsedRole))
        return Results.Redirect("/register?error=Invalid role");

    var result = await authService.RegisterAsync(new RegisterRequest(
        username,
        displayName,
        email,
        password,
        parsedRole
    ));

    if (!result.Succeeded)
    {
        var errorMsg = Uri.EscapeDataString(string.Join("; ", result.Errors));
        return Results.Redirect($"/register?error={errorMsg}");
    }

    return Results.Redirect("/");
})
.DisableAntiforgery();
app.Run();
