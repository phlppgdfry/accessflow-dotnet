namespace AccessFlow.Domain;

public enum ReviewStatus
{
    NeedsReview,
    Approved,
    ChangesRequested
}

public enum FindingSeverity
{
    Critical,
    High,
    Medium,
    Low
}

public sealed record AccessibilityFinding(
    string RuleCode,
    FindingSeverity Severity,
    string Title,
    string Detail,
    string Recommendation);

public sealed record ReviewDecision(
    ReviewStatus Status,
    string Reviewer,
    string? Note,
    DateTimeOffset DecidedAt);

public sealed record AiSuggestion(
    string Provider,
    string Summary,
    string Safeguard,
    DateTimeOffset CreatedAt);

public sealed class ContentItem
{
    public ContentItem(
        Guid id,
        string title,
        string sourceUrl,
        string html,
        string submittedBy,
        bool aiConsent,
        DateTimeOffset submittedAt,
        IReadOnlyList<AccessibilityFinding> findings)
    {
        Id = id;
        Title = title;
        SourceUrl = sourceUrl;
        Html = html;
        SubmittedBy = submittedBy;
        AiConsent = aiConsent;
        SubmittedAt = submittedAt;
        Findings = findings;
        Status = ReviewStatus.NeedsReview;
    }

    public Guid Id { get; }
    public string Title { get; }
    public string SourceUrl { get; }
    public string Html { get; }
    public string SubmittedBy { get; }
    public bool AiConsent { get; }
    public DateTimeOffset SubmittedAt { get; }
    public IReadOnlyList<AccessibilityFinding> Findings { get; }
    public ReviewStatus Status { get; private set; }
    public ReviewDecision? Decision { get; private set; }

    public void ApplyDecision(ReviewDecision decision)
    {
        Status = decision.Status;
        Decision = decision;
    }
}
