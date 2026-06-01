using DeliveryWeb.Data;
using DeliveryWeb.Hubs;
using DeliveryWeb.Models;
using DeliveryWeb.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── BASE DE DONNÉES ──
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── IDENTITÉ ──
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// ── REDIRECTION AUTH ──
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
});

// ── SERVICES ──
builder.Services.AddHttpClient<PrixService>(); // ← correcte
builder.Services.AddSignalR();
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<SimulationService>();
builder.Services.AddHttpClient();

var app = builder.Build();

// ── CRÉATION DES RÔLES AU DÉMARRAGE ──
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    foreach (var role in new[] { "Client", "Livreur", "Admin" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }
    const string adminEmail = "admin@pickme.ci";
    const string adminTel = "0000000000";
    const string adminMdp = "Admin@123456";

    if (await userManager.FindByEmailAsync(adminEmail) == null)
    {
        var admin = new IdentityUser
        {
            UserName = adminTel,      // ← connexion par téléphone
            Email = adminEmail,    // ← connexion par email
            EmailConfirmed = true,
            PhoneNumber = adminTel
        };

        var result = await userManager.CreateAsync(admin, adminMdp);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(admin, "Admin");
    }
}

// ── MIDDLEWARE ──
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication(); // ← IMPORTANT : avant UseAuthorization
app.UseAuthorization();

// ── ROUTES ──
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<TrackingHub>("/trackingHub");

app.Run();