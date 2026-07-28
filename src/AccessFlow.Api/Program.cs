using AccessFlow.Application;
using AccessFlow.Infrastructure;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSingleton<IContentRepository, InMemoryContentRepository>();
builder.Services.AddSingleton<ContentInspectionService>();
builder.Services.AddSingleton<ReviewWorkflowService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

await SeedDemoDataAsync(app.Services, app.Lifetime.ApplicationStopping);

app.MapGet("/healthz", () => Results.Ok(new { status = "healthy", service = "accessflow-api" }))
    .WithName("HealthCheck");

app.MapGet("/openapi/v1.json", () => Results.Json(new
{
    openapi = "3.0.3",
    info = new { title = "AccessFlow API", version = "v1", description = "Human-in-the-loop content quality workflow." },
    paths = new Dictionary<string, object>
    {
        ["/api/dashboard"] = new { get = new { summary = "Get review-queue metrics" } },
        ["/api/content-items"] = new { get = new { summary = "List content items" }, post = new { summary = "Submit content for review" } },
        ["/api/content-items/{id}/review"] = new { post = new { summary = "Apply a human review decision" } }
    }
}))
    .WithName("OpenApiDocument");

app.MapGet("/api/dashboard", async (ReviewWorkflowService workflow, CancellationToken cancellationToken) =>
    Results.Ok(await workflow.GetDashboardAsync(cancellationToken)))
    .WithName("GetDashboard");

app.MapGet("/api/content-items", async (IContentRepository repository, CancellationToken cancellationToken) =>
    Results.Ok(await repository.ListAsync(cancellationToken)))
    .WithName("ListContentItems");

app.MapGet("/api/content-items/{id:guid}", async (Guid id, IContentRepository repository, CancellationToken cancellationToken) =>
{
    var item = await repository.GetAsync(id, cancellationToken);
    return item is null ? Results.NotFound() : Results.Ok(item);
})
    .WithName("GetContentItem");

app.MapPost("/api/content-items", async (
    CreateContentItemRequest request,
    ReviewWorkflowService workflow,
    CancellationToken cancellationToken) =>
{
    try
    {
        var item = await workflow.SubmitAsync(request, cancellationToken);
        return Results.Created($"/api/content-items/{item.Id}", item);
    }
    catch (ArgumentException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [exception.Message] });
    }
})
    .WithName("CreateContentItem");

app.MapPost("/api/content-items/{id:guid}/review", async (
    Guid id,
    CreateReviewDecisionRequest request,
    ReviewWorkflowService workflow,
    CancellationToken cancellationToken) =>
{
    try
    {
        var item = await workflow.DecideAsync(id, request, cancellationToken);
        return item is null ? Results.NotFound() : Results.Ok(item);
    }
    catch (ArgumentException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["review"] = [exception.Message] });
    }
})
    .WithName("ReviewContentItem");

app.MapPost("/api/content-items/{id:guid}/suggestion", async (
    Guid id,
    IContentRepository repository,
    ReviewWorkflowService workflow,
    CancellationToken cancellationToken) =>
{
    var item = await repository.GetAsync(id, cancellationToken);
    if (item is null)
    {
        return Results.NotFound();
    }

    try
    {
        return Results.Ok(workflow.CreateSuggestion(item));
    }
    catch (InvalidOperationException exception)
    {
        return Results.Problem(exception.Message, statusCode: StatusCodes.Status409Conflict);
    }
})
    .WithName("CreateSafeSuggestion");

app.MapFallbackToFile("index.html");

await app.RunAsync();

static async Task SeedDemoDataAsync(IServiceProvider services, CancellationToken cancellationToken)
{
    using var scope = services.CreateScope();
    var repository = scope.ServiceProvider.GetRequiredService<IContentRepository>();
    if ((await repository.ListAsync(cancellationToken)).Count > 0)
    {
        return;
    }

    var workflow = scope.ServiceProvider.GetRequiredService<ReviewWorkflowService>();
    await workflow.SubmitAsync(
        new CreateContentItemRequest(
            "Informatie over studietoelagen",
            "https://voorbeeld.be/studietoelagen",
            "<html><head></head><body><h1>Studietoelagen</h1><h3>Wie komt in aanmerking?</h3><img src='campus.jpg'><a href='/form'>Klik hier</a></body></html>",
            "M. Janssens",
            true),
        cancellationToken);

    await workflow.SubmitAsync(
        new CreateContentItemRequest(
            "Praktische openingsuren",
            "https://voorbeeld.be/openingsuren",
            "<html lang='nl'><head><title>Openingsuren | Voorbeeld</title></head><body><main><h1>Openingsuren</h1><h2>Deze week</h2><img src='balie.jpg' alt='Onthaalbalie van het loket'></main></body></html>",
            "A. Peeters",
            false),
        cancellationToken);
}

public partial class Program;
