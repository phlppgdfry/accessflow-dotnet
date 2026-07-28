using AccessFlow.Domain;

namespace AccessFlow.Application;

public sealed record CreateContentItemRequest(
    string Title,
    string SourceUrl,
    string Html,
    string SubmittedBy,
    bool AiConsent);

public sealed record CreateReviewDecisionRequest(
    ReviewStatus Status,
    string Reviewer,
    string? Note);

public sealed record DashboardSummary(
    int TotalItems,
    int NeedsReview,
    int Approved,
    int ChangesRequested,
    int HighPriorityFindings);

public interface IContentRepository
{
    Task AddAsync(ContentItem item, CancellationToken cancellationToken);
    Task<ContentItem?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ContentItem>> ListAsync(CancellationToken cancellationToken);
    Task UpdateAsync(ContentItem item, CancellationToken cancellationToken);
}
