using System.Net.Http.Headers;
using System.Text;
using LicenseManager.Application.Common.Interfaces;
using LicenseManager.Infrastructure.Data;
using LicenseManager.Infrastructure.Payments;
using LicenseManager.Infrastructure.Payments.Razorpay;
using LicenseManager.Infrastructure.Payments.Stripe;
using LicenseManager.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Stripe;

namespace LicenseManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        // Core domain services
        services.AddScoped<ILicenseService, LicenseService>();

        // Security services
        services.AddSingleton<ICryptographyService, CryptographyService>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IMfaService, MfaService>();

        // Current user (HttpContext-based)
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Payments (Phase 4C)
        AddPayments(services, configuration);

        // Redis cache (optional)
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "LicenseManager:";
            });
        }

        return services;
    }

    private static void AddPayments(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PaymentOptions>(configuration.GetSection(PaymentOptions.SectionName));

        // ---- Stripe ----------------------------------------------------------
        // IStripeClient is a thread-safe singleton; one instance per process is
        // both correct and recommended by Stripe.net.
        services.AddSingleton<IStripeClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<PaymentOptions>>().Value.Stripe;
            // Use placeholder during boot if not configured; the gateway will
            // throw at first use unless EnsureConfigured() passes.
            var apiKey = string.IsNullOrWhiteSpace(opts.SecretKey) ? "sk_test_placeholder" : opts.SecretKey;
            return new StripeClient(apiKey);
        });

        services.AddScoped<IPaymentGateway, StripePaymentGateway>();

        // ---- Razorpay -------------------------------------------------------
        // Typed HttpClient: BaseAddress + HTTP Basic auth header configured up
        // front so the gateway code stays focused on payload concerns.
        services.AddHttpClient<RazorpayPaymentGateway>((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<PaymentOptions>>().Value.Razorpay;
            client.BaseAddress = new Uri(string.IsNullOrWhiteSpace(opts.BaseUrl)
                ? "https://api.razorpay.com"
                : opts.BaseUrl);

            if (!string.IsNullOrEmpty(opts.KeyId) && !string.IsNullOrEmpty(opts.KeySecret))
            {
                var creds = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{opts.KeyId}:{opts.KeySecret}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", creds);
            }

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Bridge the typed-client registration to the IPaymentGateway abstraction.
        // AddHttpClient<RazorpayPaymentGateway>(...) only registers the concrete
        // type, not the interface - so we add a keyed forwarder.
        services.AddScoped<IPaymentGateway>(sp => sp.GetRequiredService<RazorpayPaymentGateway>());

        // ---- Factory + service ---------------------------------------------
        services.AddScoped<IPaymentGatewayFactory, PaymentGatewayFactory>();
        services.AddScoped<IPaymentService, PaymentService>();
    }
}
