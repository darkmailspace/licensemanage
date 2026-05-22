using LicenseManager.Domain.Enums;

namespace LicenseManager.Application.Common.Interfaces;

/// <summary>
/// Resolves an <see cref="IPaymentGateway"/> for a requested provider.
/// Implementation lives in Infrastructure (uses the DI container).
/// </summary>
public interface IPaymentGatewayFactory
{
    /// <summary>Returns the gateway for <paramref name="provider"/>, or throws if not registered.</summary>
    IPaymentGateway Get(PaymentProvider provider);
}
