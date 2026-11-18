using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookRental.DataAccessLayer.Data.Migrations
{
    /// <summary>
    /// Seeds the 3 roles and the single SuperAdmin user (US-0).
    /// Credentials are read from .env (SUPERADMIN_NAME, SUPERADMIN_EMAIL, SUPERADMIN_PASSWORD)
    /// when `dotnet ef database update` runs, so nothing secret is stored in this file.
    /// </summary>
    public partial class SeedRolesAndSuperAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var name = Environment.GetEnvironmentVariable("SUPERADMIN_NAME") ?? "Super Admin";
            var email = Environment.GetEnvironmentVariable("SUPERADMIN_EMAIL");
            var password = Environment.GetEnvironmentVariable("SUPERADMIN_PASSWORD");

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    "SUPERADMIN_EMAIL and SUPERADMIN_PASSWORD must be set in .env before running this migration.");
            }

            var userRoleId = Guid.NewGuid();
            var adminRoleId = Guid.NewGuid();
            var superAdminRoleId = Guid.NewGuid();
            var superAdminId = Guid.NewGuid();

            // Roles
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "Name", "NormalizedName", "ConcurrencyStamp" },
                values: new object[,]
                {
                    { userRoleId, RoleNames.User, RoleNames.User.ToUpperInvariant(), Guid.NewGuid().ToString() },
                    { adminRoleId, RoleNames.Admin, RoleNames.Admin.ToUpperInvariant(), Guid.NewGuid().ToString() },
                    { superAdminRoleId, RoleNames.SuperAdmin, RoleNames.SuperAdmin.ToUpperInvariant(), Guid.NewGuid().ToString() }
                });

            // Hash the password exactly like Identity does on register
            var user = new ApplicationUser(name) { Id = superAdminId, UserName = email, Email = email };
            var passwordHash = new PasswordHasher<ApplicationUser>().HashPassword(user, password);

            // SuperAdmin user
            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[]
                {
                    "Id", "Name", "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
                    "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
                    "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled", "AccessFailedCount"
                },
                values: new object[]
                {
                    superAdminId, name, email, email.ToUpperInvariant(), email, email.ToUpperInvariant(),
                    true, passwordHash, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(),
                    false, false, true, 0
                });

            // Link SuperAdmin user -> SuperAdmin role
            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "UserId", "RoleId" },
                values: new object[] { superAdminId, superAdminRoleId });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Delete the SuperAdmin user(s); their AspNetUserRoles rows cascade
            migrationBuilder.Sql("""
                DELETE FROM "AspNetUsers"
                WHERE "Id" IN (
                    SELECT ur."UserId"
                    FROM "AspNetUserRoles" ur
                    JOIN "AspNetRoles" r ON r."Id" = ur."RoleId"
                    WHERE r."NormalizedName" = 'SUPERADMIN');
                """);

            // Delete the 3 seeded roles
            migrationBuilder.Sql("""
                DELETE FROM "AspNetRoles"
                WHERE "NormalizedName" IN ('USER', 'ADMIN', 'SUPERADMIN');
                """);
        }
    }
}
