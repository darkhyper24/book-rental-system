public static class SuperAdminEndpoints
{
    public static void MapSuperAdminEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/superadmin");

        group.MapPost("/admins", CreateAdmin);
    }

    private static async Task<IResult> CreateAdmin(CreateAdminDto dto, AuthService authService)
    {
        var errors = await authService.CreateAdminAsync(dto);
        if (errors.Count > 0)
        {
            return Results.BadRequest(errors);
        }
        return Results.Created();
    }
}
