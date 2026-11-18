public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/register", Register);
        group.MapPost("/login", Login);
    }

    private static async Task<IResult> Register(RegisterDto dto, AuthService authService)
    {
        var errors = await authService.RegisterUserAsync(dto);
        if (errors.Count > 0)
        {
            return Results.BadRequest(errors);
        }
        return Results.Created();
    }

    private static async Task<IResult> Login(LoginDto dto, AuthService authService)
    {
        var response = await authService.LoginUserAsync(dto);
        if (response == null)
        {
            return Results.Unauthorized();
        }
        return Results.Ok(response);
    }
}
