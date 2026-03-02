using C4.Modules.Discovery.Application.Ports;
using C4.Modules.Discovery.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace C4.Modules.Discovery.Infrastructure.Persistence.Repositories;

public sealed class ArchitectureContextRepository(DiscoveryDbContext dbContext) : IArchitectureContextRepository
{
    public async Task<ProjectArchitectureProfileRecord?> GetProfileAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var profile = await dbContext.ProjectArchitectureProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProjectId == projectId, cancellationToken);
        return profile is null ? null : ToRecord(profile);
    }

    public async Task UpsertProfileAsync(ProjectArchitectureProfileRecord profile, CancellationToken cancellationToken)
    {
        var existing = await dbContext.ProjectArchitectureProfiles
            .FirstOrDefaultAsync(p => p.ProjectId == profile.ProjectId, cancellationToken);
        if (existing is null)
        {
            await dbContext.ProjectArchitectureProfiles.AddAsync(ToEntity(profile), cancellationToken);
            return;
        }

        existing.ProjectDescription = profile.ProjectDescription;
        existing.SystemBoundaries = profile.SystemBoundaries;
        existing.CoreDomains = profile.CoreDomains;
        existing.ExternalDependencies = profile.ExternalDependencies;
        existing.DataSensitivity = profile.DataSensitivity;
        existing.IsApproved = profile.IsApproved;
        existing.LastUpdatedAtUtc = profile.LastUpdatedAtUtc;
        existing.LastQuestionGenerationAtUtc = profile.LastQuestionGenerationAtUtc;
        existing.LastResourceCount = profile.LastResourceCount;
    }

    public async Task<IReadOnlyCollection<ProjectArchitectureQuestionRecord>> GetQuestionsAsync(Guid projectId, CancellationToken cancellationToken) =>
        await dbContext.ProjectArchitectureQuestions
            .AsNoTracking()
            .Where(q => q.ProjectId == projectId)
            .OrderBy(q => q.CreatedAtUtc)
            .Select(q => new ProjectArchitectureQuestionRecord(
                q.Id,
                q.ProjectId,
                q.Question,
                q.Answer,
                q.IsApproved,
                q.CreatedAtUtc,
                q.AnsweredAtUtc))
            .ToListAsync(cancellationToken);

    public async Task ReplaceQuestionsAsync(Guid projectId, IReadOnlyCollection<ProjectArchitectureQuestionRecord> questions, CancellationToken cancellationToken)
    {
        var existing = await dbContext.ProjectArchitectureQuestions
            .Where(q => q.ProjectId == projectId)
            .ToListAsync(cancellationToken);
        dbContext.ProjectArchitectureQuestions.RemoveRange(existing);

        foreach (var question in questions)
        {
            await dbContext.ProjectArchitectureQuestions.AddAsync(
                new ProjectArchitectureQuestionEntity
                {
                    Id = question.Id,
                    ProjectId = question.ProjectId,
                    Question = question.Question,
                    Answer = question.Answer,
                    IsApproved = question.IsApproved,
                    CreatedAtUtc = question.CreatedAtUtc,
                    AnsweredAtUtc = question.AnsweredAtUtc
                },
                cancellationToken);
        }
    }

    public async Task UpdateQuestionAnswerAsync(Guid projectId, Guid questionId, string answer, CancellationToken cancellationToken)
    {
        var question = await dbContext.ProjectArchitectureQuestions
            .FirstOrDefaultAsync(q => q.ProjectId == projectId && q.Id == questionId, cancellationToken);
        if (question is null)
            return;

        question.Answer = answer;
        question.AnsweredAtUtc = DateTime.UtcNow;
        question.IsApproved = !string.IsNullOrWhiteSpace(answer);
    }

    private static ProjectArchitectureProfileRecord ToRecord(ProjectArchitectureProfileEntity profile) =>
        new(
            profile.ProjectId,
            profile.ProjectDescription,
            profile.SystemBoundaries,
            profile.CoreDomains,
            profile.ExternalDependencies,
            profile.DataSensitivity,
            profile.IsApproved,
            profile.LastUpdatedAtUtc,
            profile.LastQuestionGenerationAtUtc,
            profile.LastResourceCount);

    private static ProjectArchitectureProfileEntity ToEntity(ProjectArchitectureProfileRecord profile) =>
        new()
        {
            ProjectId = profile.ProjectId,
            ProjectDescription = profile.ProjectDescription,
            SystemBoundaries = profile.SystemBoundaries,
            CoreDomains = profile.CoreDomains,
            ExternalDependencies = profile.ExternalDependencies,
            DataSensitivity = profile.DataSensitivity,
            IsApproved = profile.IsApproved,
            LastUpdatedAtUtc = profile.LastUpdatedAtUtc,
            LastQuestionGenerationAtUtc = profile.LastQuestionGenerationAtUtc,
            LastResourceCount = profile.LastResourceCount
        };
}
