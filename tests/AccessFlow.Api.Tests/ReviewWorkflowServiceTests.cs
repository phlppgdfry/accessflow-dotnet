using AccessFlow.Application;
using AccessFlow.Domain;
using AccessFlow.Infrastructure;

namespace AccessFlow.Api.Tests;

public sealed class ReviewWorkflowServiceTests
{
    [Fact]
    public async Task SubmitAsync_CreatesReviewItemWithHumanDecisionRequired()
    {
        var workflow = CreateWorkflow();

        var item = await workflow.SubmitAsync(
            new CreateContentItemRequest(
                "Welkom",
                "https://example.org/welcome",
                "<html><body><h1>Welkom</h1></body></html>",
                "Editor",
                true),
            CancellationToken.None);

        Assert.Equal(ReviewStatus.NeedsReview, item.Status);
        Assert.NotEmpty(item.Findings);
    }

    [Fact]
    public async Task CreateSuggestion_WhenConsentIsMissing_RejectsExternalAiFlow()
    {
        var workflow = CreateWorkflow();
        var item = await workflow.SubmitAsync(
            new CreateContentItemRequest(
                "Welkom",
                "https://example.org/welcome",
                "<html><body><h1>Welkom</h1></body></html>",
                "Editor",
                false),
            CancellationToken.None);

        Action action = () => _ = workflow.CreateSuggestion(item);
        var exception = Assert.Throws<InvalidOperationException>(action);

        Assert.Contains("toestemming", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static ReviewWorkflowService CreateWorkflow() => new(
        new InMemoryContentRepository(),
        new ContentInspectionService(),
        TimeProvider.System);
}
