namespace C4.Modules.Discovery.Application.ConnectAzureSubscription;

public sealed record ConnectAzureSubscriptionResponse(Guid SubscriptionId, string ExternalSubscriptionId, string DisplayName);
