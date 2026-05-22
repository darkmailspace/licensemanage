using LicenseManager.Application.Common.Interfaces;
using LicenseManager.Domain.Enums;

namespace LicenseManager.Infrastructure.Payments;

/// <summary>
/// Trivial factory: every <see cref="IPaymentGateway"/> registered in DI is
/// keyed by its <see cref="IPaymentGateway.Provider"/> on construction and
/// looked up O(1) per request.
/// </summary>
public sealed class PaymentGatewayFactory : IPaymentGatewayFactory
{
    private readonly IReadOnlyDictionary<PaymentProvider, IPaymentGateway> _gateways;

    public PaymentGatewayFactory(IEnumerable<IPaymentGateway> gateways)
    {
        _gateways = gateways.ToDictionary(g => g.Provider);
    }

    public IPaymentGateway Get(PaymentProvider provider)
    {
        if (_gateways.TryGetValue(provider, out var gateway))
        {
            return gateway;
        }

        throw new InvalidOperationException(
            $"No payment gateway is registered for provider '{provider}'. " +
            $"Configure the corresponding section under 'Payments' in appsettings.");
    }
}
