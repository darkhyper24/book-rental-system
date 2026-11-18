using Microsoft.EntityFrameworkCore;

DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentityServices();

builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddApplicationServices();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();

app.Map("/api/user", branch =>
{
    branch.UseRequireRole(RoleNames.User);
    branch.UseRouting();
    branch.UseEndpoints(_ => { });
});

app.Map("/api/admin", branch =>
{
    branch.UseRequireRole(RoleNames.Admin);
    branch.UseRouting();
    branch.UseEndpoints(_ => { });
});

app.Map("/api/superadmin", branch =>
{
    branch.UseRequireRole(RoleNames.SuperAdmin);
    branch.UseRouting();
    branch.UseEndpoints(_ => { });
});

app.MapAuthEndpoints();
app.MapBookEndpoints();
app.MapUserEndpoints();
app.MapAdminEndpoints();
app.MapSuperAdminEndpoints();

app.Run();
