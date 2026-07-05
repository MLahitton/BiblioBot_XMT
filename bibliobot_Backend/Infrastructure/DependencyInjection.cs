using Application.Common.Interfaces;
using Infrastructure.Persistence;
using Infrastructure.Persistence.SeedData;
using Infrastructure.Security;
using Infrastructure.Chatbot;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");
        }

        services.AddDbContext<BiblioBotDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<BiblioBotDbContext>());
        services.AddScoped<IDatabaseSeeder, BiblioBotDatabaseSeeder>();
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<ChatbotOptions>(configuration.GetSection("Chatbot"));
        services.AddHttpContextAccessor();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddHttpClient<IChatbotClient, FastApiChatbotClient>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<ChatbotOptions>>().Value;

            if (string.IsNullOrWhiteSpace(options.BaseUrl))
            {
                throw new InvalidOperationException("Chatbot:BaseUrl is required.");
            }

            var timeoutSeconds = options.TimeoutSeconds > 0 ? options.TimeoutSeconds : 30;
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        });

        return services;
    }
}
