using C4.Modules.Discovery.Application.Ports;

namespace C4.Modules.Discovery.Infrastructure.Persistence.Repositories;

public sealed class InMemoryArchitectureContextRepository : IArchitectureContextRepository
{
    private readonly Dictionary<Guid, ProjectArchitectureProfileRecord> _profiles = [];
    private readonly Dictionary<Guid, List<ProjectArchitectureQuestionRecord>> _questions = [];

    public Task<ProjectArchitectureProfileRecord?> GetProfileAsync(Guid projectId, CancellationToken cancellationToken) =>
        Task.FromResult(_profiles.GetValueOrDefault(projectId));

    public Task UpsertProfileAsync(ProjectArchitectureProfileRecord profile, CancellationToken cancellationToken)
    {
        _profiles[profile.ProjectId] = profile;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<ProjectArchitectureQuestionRecord>> GetQuestionsAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var values = _questions.GetValueOrDefault(projectId) ?? [];
        return Task.FromResult<IReadOnlyCollection<ProjectArchitectureQuestionRecord>>(values.OrderBy(q => q.CreatedAtUtc).ToArray());
    }

    public Task ReplaceQuestionsAsync(Guid projectId, IReadOnlyCollection<ProjectArchitectureQuestionRecord> questions, CancellationToken cancellationToken)
    {
        _questions[projectId] = questions.ToList();
        return Task.CompletedTask;
    }

    public Task UpdateQuestionAnswerAsync(Guid projectId, Guid questionId, string answer, CancellationToken cancellationToken)
    {
        if (!_questions.TryGetValue(projectId, out var list))
            return Task.CompletedTask;

        var index = list.FindIndex(q => q.Id == questionId);
        if (index < 0)
            return Task.CompletedTask;

        var current = list[index];
        list[index] = current with
        {
            Answer = answer,
            IsApproved = !string.IsNullOrWhiteSpace(answer),
            AnsweredAtUtc = DateTime.UtcNow
        };

        return Task.CompletedTask;
    }
}
