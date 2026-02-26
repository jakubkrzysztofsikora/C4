using C4.Shared.Kernel;

namespace C4.Modules.Identity.Domain.Errors;

public static class IdentityErrors
{
    public static Error DuplicateOrganizationName(string name) =>
        new("identity.organization.duplicate_name", $"Organization '{name}' already exists.");

    public static Error OrganizationNotFound(Guid organizationId) =>
        new("identity.organization.not_found", $"Organization '{organizationId}' was not found.");

    public static Error DuplicateProjectName(string name) =>
        new("identity.project.duplicate_name", $"Project '{name}' already exists in this organization.");

    public static Error ProjectNotFound(Guid projectId) =>
        new("identity.project.not_found", $"Project '{projectId}' was not found.");

    public static Error MemberAlreadyExists(string externalUserId) =>
        new("identity.member.already_exists", $"Member '{externalUserId}' already exists in this project.");

    public static Error MemberNotFound(Guid memberId) =>
        new("identity.member.not_found", $"Member '{memberId}' was not found.");

    public static Error CannotDemoteLastOwner(Guid projectId) =>
        new("identity.member.last_owner", $"Cannot demote last owner in project '{projectId}'.");

    public static Error EmptyName(string field) =>
        new("identity.validation.empty_name", $"{field} cannot be empty.");

    public static Error DuplicateEmail(string email) =>
        new("identity.user.duplicate_email", $"Email '{email}' is already registered.");

    public static Error InvalidCredentials() =>
        new("identity.user.invalid_credentials", "Invalid email or password.");

    public static Error WeakPassword() =>
        new("identity.user.weak_password", "Password must be at least 8 characters.");
}
