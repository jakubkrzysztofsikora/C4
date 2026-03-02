using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Graph.Application.GetThreatAssessment;

public sealed record GetThreatAssessmentQuery(Guid ProjectId, string? View = null) : IRequest<Result<ThreatAssessmentResponse>>;
public sealed record ThreatAssessmentResponse(Guid ProjectId, string RiskLevel, IReadOnlyCollection<ThreatItem> Threats);
public sealed record ThreatItem(string Component, string ThreatType, string Severity, string Mitigation);
