public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<TokenService>();
        services.AddScoped<AuthService>();
        services.AddScoped<IBookRepository,BookRepository>();
        services.AddScoped<IBookService,BookService>();
        services.AddScoped<IRentRepository,RentRepository>();
        services.AddScoped<IRentService,RentService>();
        services.AddValidation();

        return services;
    }
}
