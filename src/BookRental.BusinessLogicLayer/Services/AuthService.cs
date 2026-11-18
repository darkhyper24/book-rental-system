using Microsoft.AspNetCore.Identity;

public class AuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TokenService _tokenService;

    public AuthService(UserManager<ApplicationUser> userManager, TokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    public async Task<List<string>> RegisterUserAsync(RegisterDto dto)
    {
        return await CreateUserAsync(dto.Name, dto.Email, dto.Password, RoleNames.User);
    }

    public async Task<List<string>> CreateAdminAsync(CreateAdminDto dto)
    {
        return await CreateUserAsync(dto.Name, dto.Email, dto.Password, RoleNames.Admin);
    }

    public async Task<AuthResponseDto?> LoginUserAsync(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
        {
            return null;
        }

        var passwordIsCorrect = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!passwordIsCorrect)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);
        var (token, expiresAt) = _tokenService.CreateToken(user, roles);

        return new AuthResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            Name = user.Name,
            Email = user.Email!,
            Roles = roles
        };
    }

    private async Task<List<string>> CreateUserAsync(string name, string email, string password, string role)
    {
        var user = new ApplicationUser(name)
        {
            Email = email,
            UserName = email
        };

        var createResult = await _userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            return GetErrorMessages(createResult);
        }

        var roleResult = await _userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return GetErrorMessages(roleResult);
        }

        return new List<string>();
    }

    private List<string> GetErrorMessages(IdentityResult result)
    {
        var messages = new List<string>();
        foreach (var error in result.Errors)
        {
            messages.Add(error.Description);
        }
        return messages;
    }
}
