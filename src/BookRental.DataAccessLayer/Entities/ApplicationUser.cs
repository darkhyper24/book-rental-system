using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public ApplicationUser(string name)
    {
        Name = name;
    }

    public string Name { get; set; }
}
