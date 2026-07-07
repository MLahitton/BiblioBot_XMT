using System;
using System.Linq;
using System.Threading.Tasks;
using Infrastructure.Persistence;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Domain.Entities;
using Domain.Constants;
using Application.Common.Interfaces;

var email = "e2e.admin@bibliobot.test";
var newPassword = "E2E_Admin_Reset_123!";
var appSettingsPath = Path.Combine("c:", "Users", "mlahi", "Desktop", "BiblioBot", "bibliobot_Backend", "Api", "appsettings.Development.json");

var cfg = new ConfigurationBuilder()
    .AddJsonFile(appSettingsPath, optional: false, reloadOnChange: false)
    .Build();

var connectionString = cfg.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.WriteLine("ERROR_NO_CONNECTION_STRING");
    return;
}

await using var ctx = new BiblioBotDbContext(
    new DbContextOptionsBuilder<BiblioBotDbContext>()
        .UseNpgsql(connectionString)
        .Options);

var user = await ctx.Users.AsTracking().FirstOrDefaultAsync(u => u.Email == email);
if (user is null)
{
    Console.WriteLine("ERROR_USER_NOT_FOUND");
    return;
}

var hasher = new PasswordHasher();
user.PasswordHash = hasher.HashPassword(user, newPassword);
user.UpdatedAt = DateTimeOffset.UtcNow;

var adminRoleId = new Guid("10000000-0000-0000-0000-000000000003");
var exists = await ctx.UserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == adminRoleId);
if (!exists)
{
    ctx.UserRoles.Add(new UserRole
    {
        UserId = user.Id,
        RoleId = adminRoleId,
        CreatedAt = DateTimeOffset.UtcNow
    });
}

var workerRoleId = new Guid("10000000-0000-0000-0000-000000000002");
var existsWorker = await ctx.UserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == workerRoleId);
if (!existsWorker)
{
    ctx.UserRoles.Add(new UserRole
    {
        UserId = user.Id,
        RoleId = workerRoleId,
        CreatedAt = DateTimeOffset.UtcNow
    });
}

await ctx.SaveChangesAsync();
Console.WriteLine("OK:USER_ID=" + user.Id);
Console.WriteLine("OK:NEW_PASSWORD=" + newPassword);
Console.WriteLine("OK:ROLE_ADMIN=" + (!exists ? "added" : "already"));
Console.WriteLine("OK:ROLE_WORKER=" + (!existsWorker ? "added" : "already"));
