using C4.Modules.Discovery.Infrastructure.Security;

namespace C4.Modules.Discovery.Tests;

public sealed class InMemoryOAuthStateStoreTests
{
    [Fact]
    public async Task StoreAndValidate_ValidState_ReturnsTrue()
    {
        var store = new InMemoryOAuthStateStore();
        await store.StoreAsync("test-state", CancellationToken.None);

        bool isValid = await store.ValidateAndConsumeAsync("test-state", CancellationToken.None);

        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAndConsume_UnknownState_ReturnsFalse()
    {
        var store = new InMemoryOAuthStateStore();

        bool isValid = await store.ValidateAndConsumeAsync("nonexistent-state", CancellationToken.None);

        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAndConsume_StateConsumedOnce_SecondCallReturnsFalse()
    {
        var store = new InMemoryOAuthStateStore();
        await store.StoreAsync("one-time-state", CancellationToken.None);

        bool firstValidation = await store.ValidateAndConsumeAsync("one-time-state", CancellationToken.None);
        bool secondValidation = await store.ValidateAndConsumeAsync("one-time-state", CancellationToken.None);

        firstValidation.Should().BeTrue();
        secondValidation.Should().BeFalse();
    }

    [Fact]
    public async Task Store_MultipleStates_EachValidatesIndependently()
    {
        var store = new InMemoryOAuthStateStore();
        await store.StoreAsync("state-a", CancellationToken.None);
        await store.StoreAsync("state-b", CancellationToken.None);

        bool validA = await store.ValidateAndConsumeAsync("state-a", CancellationToken.None);
        bool validB = await store.ValidateAndConsumeAsync("state-b", CancellationToken.None);

        validA.Should().BeTrue();
        validB.Should().BeTrue();
    }
}
