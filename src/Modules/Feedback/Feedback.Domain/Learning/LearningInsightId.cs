using C4.Shared.Kernel;

namespace C4.Modules.Feedback.Domain.Learning;

public sealed record LearningInsightId(Guid Value) : StronglyTypedGuidId(Value)
{
    public static LearningInsightId New() => new(Guid.NewGuid());
}
