public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<TokenService>();
        services.AddScoped<AuthService>();
        services.AddScoped<IBookRepository,BookRepository>();
        services.AddScoped<BookService>();
        services.AddScoped<IRentRepository,RentRepository>();
        services.AddScoped<RentService>();
        services.AddValidation();

        return services;
    }
}
