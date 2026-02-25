using C4.Modules.Identity.Domain.Project;
using C4.Shared.Kernel;

namespace C4.Modules.Identity.Domain.Member;

public sealed class Member : Entity<MemberId>
{
    private Member(MemberId id, ProjectId projectId, string externalUserId, Role role) : base(id)
    {
        ProjectId = projectId;
        ExternalUserId = externalUserId;
        Role = role;
    }

    public ProjectId ProjectId { get; }

    public string ExternalUserId { get; }

    public Role Role { get; private set; }

    public static Member Invite(ProjectId projectId, string externalUserId, Role role) =>
        new(MemberId.New(), projectId, externalUserId.Trim(), role);

    public void UpdateRole(Role role) => Role = role;
}
