namespace Bilito.Identity.Application.Abstractions;

public sealed class IdentityDeliveryUnavailableException(string message) : Exception(message);
