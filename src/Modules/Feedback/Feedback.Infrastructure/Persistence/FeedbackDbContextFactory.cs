using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace C4.Modules.Feedback.Infrastructure.Persistence;

public sealed class FeedbackDbContextFactory : IDesignTimeDbContextFactory<FeedbackDbContext>
{
    public FeedbackDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FeedbackDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=c4_feedback;Username=c4;Password=c4dev");
        return new FeedbackDbContext(optionsBuilder.Options);
    }
}
