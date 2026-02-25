Generate comprehensive tests for an existing unit, handler, domain object, or vertical slice.

## Arguments
- `$ARGUMENTS` – target to test (e.g., "PlaceOrderHandler", "Order domain aggregate", "POST /orders endpoint")

## Instructions

1. Locate the target from `$ARGUMENTS` in the codebase.
2. Read `docs/standards/testing-standards.md`.
3. Analyze the target:
   - Identify all public methods / behaviors to test
   - Identify dependencies (ports/interfaces to fake)
   - Identify edge cases, validation boundaries, failure paths

4. Generate tests in the appropriate test project:

### For Application Handlers (unit tests)
```csharp
public sealed class <Handler>Tests
{
    private readonly <Port>Fake _<portName> = new();
    private readonly <Handler> _sut;

    public <Handler>Tests()
    {
        _sut = new <Handler>(_<portName>);
    }

    [Fact]
    public async Task Handle_<Scenario>_<ExpectedOutcome>()
    {
        // arrange
        ...

        // act
        var result = await _sut.Handle(..., CancellationToken.None);

        // assert
        result.Should()...;
    }
}
```

### For Domain Objects (unit tests)
- Test each behavior method
- Use the `Should()` style from FluentAssertions
- Cover invariant violations (expected failures as `Result.Failure`)

### For Endpoints (acceptance tests)
```csharp
public sealed class <Feature>EndpointTests(WebAppFixture fixture) : IClassFixture<WebAppFixture>
{
    [Fact]
    public async Task <Method>_<Scenario>_Returns<StatusCode>()
    {
        // arrange
        var client = fixture.CreateClient();
        ...

        // act
        var response = await client.PostAsJsonAsync("/path", ...);

        // assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ...
    }
}
```

### For SK Plugins (unit tests with kernel mock)
- Use `Kernel.CreateBuilder().Build()` with mocked services
- Test that `KernelFunction` returns expected results for given inputs

5. Ensure:
   - Test class name: `<SUT>Tests`
   - Method name: `<Method>_<Scenario>_<ExpectedOutcome>`
   - No comments labeling Arrange/Act/Assert sections (blank lines are enough)
   - Fakes over mocks for ports with multiple interactions
   - One logical assertion per test method

6. Run the new tests: `dotnet test --filter "FullyQualifiedName~<TestClass>"`

7. Report which tests pass and which need further work.
