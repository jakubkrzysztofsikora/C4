using C4.Modules.Feedback.Domain.FeedbackEntry;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Feedback.Application.GetFeedbackByProject;

public sealed record GetFeedbackByProjectQuery(
    Guid ProjectId,
    int Skip,
    int Take,
    FeedbackCategory? Category) : IRequest<Result<GetFeedbackByProjectResponse>>;

public sealed record GetFeedbackByProjectResponse(IReadOnlyCollection<FeedbackEntryDto> Entries, int TotalCount);
