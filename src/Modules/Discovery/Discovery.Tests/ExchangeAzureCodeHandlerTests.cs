using C4.Modules.Discovery.Application.ExchangeAzureCode;
using C4.Modules.Discovery.Application.Ports;

namespace C4.Modules.Discovery.Tests;

public sealed class ExchangeAzureCodeHandlerTests
{
    [Fact]
    public async Task Handle_ValidStateAndCode_ReturnsSubscriptions()
    {
        var stateStore = new FakeOAuthStateStore();
        var identityService = new FakeAzureIdentityService();
        var tokenStore = new FakeAzureTokenStore();
        var handler = new ExchangeAzureCodeHandler(identityService, tokenStore, stateStore);

        const string state = "valid-state";
        await stateStore.StoreAsync(state, CancellationToken.None);
        var command = new ExchangeAzureCodeCommand("auth-code", "https://localhost/callback", state);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Subscriptions.Should().HaveCount(1);
        result.Value.Subscriptions[0].SubscriptionId.Should().Be("sub-001");
    }

    [Fact]
    public async Task Handle_NullState_ReturnsInvalidStateError()
    {
        var stateStore = new FakeOAuthStateStore();
        var identityService = new FakeAzureIdentityService();
        var tokenStore = new FakeAzureTokenStore();
        var handler = new ExchangeAzureCodeHandler(identityService, tokenStore, stateStore);

        var command = new ExchangeAzureCodeCommand("auth-code", "https://localhost/callback", null!);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AzureAuth.InvalidState");
    }

    [Fact]
    public async Task Handle_EmptyState_ReturnsInvalidStateError()
    {
        var stateStore = new FakeOAuthStateStore();
        var identityService = new FakeAzureIdentityService();
        var tokenStore = new FakeAzureTokenStore();
        var handler = new ExchangeAzureCodeHandler(identityService, tokenStore, stateStore);

        var command = new ExchangeAzureCodeCommand("auth-code", "https://localhost/callback", "");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AzureAuth.InvalidState");
    }

    [Fact]
    public async Task Handle_UnknownState_ReturnsInvalidStateError()
    {
        var stateStore = new FakeOAuthStateStore();
        var identityService = new FakeAzureIdentityService();
        var tokenStore = new FakeAzureTokenStore();
        var handler = new ExchangeAzureCodeHandler(identityService, tokenStore, stateStore);

        await stateStore.StoreAsync("stored-state", CancellationToken.None);
        var command = new ExchangeAzureCodeCommand("auth-code", "https://localhost/callback", "different-state");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AzureAuth.InvalidState");
    }

    [Fact]
    public async Task Handle_StateConsumedOnFirstUse_SecondCallFails()
    {
        var stateStore = new FakeOAuthStateStore();
        var identityService = new FakeAzureIdentityService();
        var tokenStore = new FakeAzureTokenStore();
        var handler = new ExchangeAzureCodeHandler(identityService, tokenStore, stateStore);

        const string state = "one-time-state";
        await stateStore.StoreAsync(state, CancellationToken.None);
        var command = new ExchangeAzureCodeCommand("auth-code", "https://localhost/callback", state);

        var firstResult = await handler.Handle(command, CancellationToken.None);
        var secondResult = await handler.Handle(command, CancellationToken.None);

        firstResult.IsSuccess.Should().BeTrue();
        secondResult.IsFailure.Should().BeTrue();
        secondResult.Error.Code.Should().Be("AzureAuth.InvalidState");
    }

    [Fact]
    public async Task Handle_ValidState_StoresTokesForEachSubscription()
    {
        var stateStore = new FakeOAuthStateStore();
        var identityService = new FakeAzureIdentityService();
        var tokenStore = new FakeAzureTokenStore();
        var handler = new ExchangeAzureCodeHandler(identityService, tokenStore, stateStore);

        const string state = "valid-state";
        await stateStore.StoreAsync(state, CancellationToken.None);
        var command = new ExchangeAzureCodeCommand("auth-code", "https://localhost/callback", state);

        await handler.Handle(command, CancellationToken.None);

        AzureTokenInfo? storedToken = await tokenStore.GetAsync("sub-001", CancellationToken.None);
        storedToken.Should().NotBeNull();
        storedToken!.AccessToken.Should().Be("access-token");
    }

    private sealed class FakeOAuthStateStore : IOAuthStateStore
    {
        private readonly HashSet<string> _states = [];

        public Task StoreAsync(string state, CancellationToken cancellationToken)
        {
            _states.Add(state);
            return Task.CompletedTask;
        }

        public Task<bool> ValidateAndConsumeAsync(string state, CancellationToken cancellationToken)
        {
            bool removed = _states.Remove(state);
            return Task.FromResult(removed);
        }
    }

    private sealed class FakeAzureIdentityService : IAzureIdentityService
    {
        public string BuildAuthorizationUrl(string redirectUri, string state) => $"https://auth.example.com?state={state}";

        public Task<AzureTokenResponse> ExchangeAuthorizationCodeAsync(string code, string redirectUri, CancellationToken cancellationToken)
            => Task.FromResult(new AzureTokenResponse("access-token", 3600, "refresh-token"));

        public Task<IReadOnlyList<AzureSubscriptionInfo>> ListSubscriptionsAsync(string accessToken, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<AzureSubscriptionInfo>>([new AzureSubscriptionInfo("sub-001", "Production", "Enabled")]);

        public Task<AzureTokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
            => Task.FromResult(new AzureTokenResponse("new-access-token", 3600, "new-refresh-token"));
    }

    private sealed class FakeAzureTokenStore : IAzureTokenStore
    {
        private readonly Dictionary<string, AzureTokenInfo> _tokens = new();

        public Task StoreAsync(string externalSubscriptionId, AzureTokenInfo tokenInfo, CancellationToken cancellationToken)
        {
            _tokens[externalSubscriptionId] = tokenInfo;
            return Task.CompletedTask;
        }

        public Task<AzureTokenInfo?> GetAsync(string externalSubscriptionId, CancellationToken cancellationToken)
        {
            _tokens.TryGetValue(externalSubscriptionId, out AzureTokenInfo? token);
            return Task.FromResult(token);
        }
    }
}
