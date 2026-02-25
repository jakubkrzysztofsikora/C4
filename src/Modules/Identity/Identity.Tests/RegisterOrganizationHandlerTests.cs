using C4.Modules.Identity.Application.Ports;
using C4.Modules.Identity.Application.RegisterOrganization;
using C4.Modules.Identity.Domain.Organization;
using C4.Shared.Kernel;

namespace C4.Modules.Identity.Tests;

public sealed class RegisterOrganizationHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_CreatesOrganization()
    {
        var organizationRepository = new FakeOrganizationRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RegisterOrganizationHandler(organizationRepository, unitOfWork);

        var result = await handler.Handle(new RegisterOrganizationCommand("Acme"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Acme");
        unitOfWork.SaveChangesCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_DuplicateName_ReturnsError()
    {
        var organizationRepository = new FakeOrganizationRepository();
        await organizationRepository.AddAsync(Organization.Create("Acme").Value, CancellationToken.None);

        var handler = new RegisterOrganizationHandler(organizationRepository, new FakeUnitOfWork());

        var result = await handler.Handle(new RegisterOrganizationCommand("Acme"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("identity.organization.duplicate_name");
    }

    private sealed class FakeOrganizationRepository : IOrganizationRepository
    {
        private readonly Dictionary<OrganizationId, Organization> _organizations = [];

        public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken)
            => Task.FromResult(_organizations.Values.Any(organization => string.Equals(organization.Name, name, StringComparison.OrdinalIgnoreCase)));

        public Task AddAsync(Organization organization, CancellationToken cancellationToken)
        {
            _organizations[organization.Id] = organization;
            return Task.CompletedTask;
        }

        public Task<Organization?> GetByIdAsync(OrganizationId organizationId, CancellationToken cancellationToken)
        {
            _organizations.TryGetValue(organizationId, out var organization);
            return Task.FromResult(organization);
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCount++;
            return Task.FromResult(1);
        }
    }
}
