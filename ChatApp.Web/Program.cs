using ChatApp.Application.Services;
using ChatApp.Core.Entities;
using ChatApp.Core.Interfaces.Repositories;
using ChatApp.Core.Interfaces.Services;
using ChatApp.Infrastructure.Identity;
using ChatApp.Infrastructure.Persistence;
using ChatApp.Infrastructure.Persistence.Repositories;
using ChatApp.Infrastructure.SignalR;
using ChatApp.Web.Services;
using Microsoft.AspNetCore.Identity;
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

app.Run();
