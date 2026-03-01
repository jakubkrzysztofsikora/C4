using C4.Modules.Feedback.Application.Ports;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Feedback.Application.GetFeedbackByProject;

public sealed class GetFeedbackByProjectHandler(
    IFeedbackEntryRepository repository,
    IProjectAuthorizationService authorizationService)
    : IRequestHandler<GetFeedbackByProjectQuery, Result<GetFeedbackByProjectResponse>>
{
    public async Task<Result<GetFeedbackByProjectResponse>> Handle(GetFeedbackByProjectQuery request, CancellationToken cancellationToken)
    {
        var authCheck = await authorizationService.AuthorizeAsync(request.ProjectId, cancellationToken);
        if (!authCheck.IsSuccess) return Result<GetFeedbackByProjectResponse>.Failure(authCheck.Error);

        var entries = await repository.GetByProjectAsync(request.ProjectId, request.Skip, request.Take, request.Category, cancellationToken);

        var dtos = entries.Select(e => new FeedbackEntryDto(
            e.Id.Value,
            e.Target.TargetType,
            e.Target.TargetId,
            e.Category,
            e.Rating.Score,
            e.Comment,
            e.SubmittedAtUtc,
            e.UserId)).ToList();

        return Result<GetFeedbackByProjectResponse>.Success(new GetFeedbackByProjectResponse(dtos, dtos.Count));
    }
}
