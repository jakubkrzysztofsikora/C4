using C4.Shared.Kernel;

namespace C4.Modules.Feedback.Domain.FeedbackEntry;

public sealed record FeedbackEntryId(Guid Value) : StronglyTypedGuidId(Value)
{
    public static FeedbackEntryId New() => new(Guid.NewGuid());
}
