using AccessFlow.Domain;

namespace AccessFlow.Application;

public sealed class ReviewWorkflowService(
    IContentRepository repository,
    ContentInspectionService inspector,
    TimeProvider clock)
{
    public async Task<ContentItem> SubmitAsync(CreateContentItemRequest request, CancellationToken cancellationToken)
    {
        ValidateRequest(request);

        var item = new ContentItem(
            Guid.NewGuid(),
            request.Title.Trim(),
            request.SourceUrl.Trim(),
            request.Html,
            request.SubmittedBy.Trim(),
            request.AiConsent,
            clock.GetUtcNow(),
            inspector.Inspect(request.Html));

        await repository.AddAsync(item, cancellationToken);
        return item;
    }

    public async Task<ContentItem?> DecideAsync(
        Guid id,
        CreateReviewDecisionRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Status is ReviewStatus.NeedsReview)
        {
            throw new ArgumentException("Een reviewbeslissing moet goedkeuren of wijzigingen vragen.");
        }

        if (string.IsNullOrWhiteSpace(request.Reviewer))
        {
            throw new ArgumentException("Een reviewer is verplicht.");
        }

        var item = await repository.GetAsync(id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        item.ApplyDecision(new ReviewDecision(
            request.Status,
            request.Reviewer.Trim(),
            request.Note?.Trim(),
            clock.GetUtcNow()));

        await repository.UpdateAsync(item, cancellationToken);
        return item;
    }

    public async Task<DashboardSummary> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var items = await repository.ListAsync(cancellationToken);
        return new DashboardSummary(
            items.Count,
            items.Count(item => item.Status is ReviewStatus.NeedsReview),
            items.Count(item => item.Status is ReviewStatus.Approved),
            items.Count(item => item.Status is ReviewStatus.ChangesRequested),
            items.Sum(item => item.Findings.Count(finding => finding.Severity is FindingSeverity.Critical or FindingSeverity.High)));
    }

    public AiSuggestion CreateSuggestion(ContentItem item)
    {
        if (!item.AiConsent)
        {
            throw new InvalidOperationException("AI-suggesties vereisen expliciete toestemming voor deze inhoud.");
        }

        var priority = item.Findings.FirstOrDefault(finding => finding.Severity is FindingSeverity.Critical or FindingSeverity.High);
        var summary = priority is null
            ? "Geen kritieke suggestie. Laat een reviewer de inhoud en context alsnog beoordelen."
            : $"Begin met ‘{priority.Title.ToLowerInvariant()}’. {priority.Recommendation}";

        return new AiSuggestion(
            "Deterministic local safety adapter",
            summary,
            "Geen inhoud werd doorgestuurd naar een extern model; een mens beslist altijd over publicatie.",
            clock.GetUtcNow());
    }

    private static void ValidateRequest(CreateContentItemRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.SourceUrl) ||
            string.IsNullOrWhiteSpace(request.Html) || string.IsNullOrWhiteSpace(request.SubmittedBy))
        {
            throw new ArgumentException("Titel, bron-URL, HTML en indiener zijn verplicht.");
        }

        if (!Uri.TryCreate(request.SourceUrl, UriKind.Absolute, out _))
        {
            throw new ArgumentException("Bron-URL moet absoluut zijn.");
        }
    }
}
