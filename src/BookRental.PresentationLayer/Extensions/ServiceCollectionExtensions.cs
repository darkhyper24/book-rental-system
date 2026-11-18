public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<TokenService>();
        services.AddScoped<AuthService>();
        services.AddScoped<BookRepository>();
        services.AddScoped<BookService>();
        services.AddScoped<RentRepository>();
        services.AddScoped<RentService>();
        services.AddValidation();

        return services;
    }
}
