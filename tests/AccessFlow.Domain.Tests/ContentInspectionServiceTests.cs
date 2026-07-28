using AccessFlow.Application;

namespace AccessFlow.Domain.Tests;

public sealed class ContentInspectionServiceTests
{
    private readonly ContentInspectionService service = new();

    [Fact]
    public void Inspect_WhenMarkupHasAccessibilityGaps_ReturnsActionableFindings()
    {
        const string html = "<html><head></head><body><h1>Nieuws</h1><h3>Meer</h3><img src='news.jpg'><a href='/x'>Klik hier</a></body></html>";

        var findings = service.Inspect(html);

        Assert.Contains(findings, finding => finding.RuleCode == "AF-001" && finding.Severity == FindingSeverity.High);
        Assert.Contains(findings, finding => finding.RuleCode == "AF-004" && finding.Severity == FindingSeverity.High);
        Assert.Contains(findings, finding => finding.RuleCode == "AF-006");
    }

    [Fact]
    public void Inspect_WhenMarkupIsWellStructured_ReturnsNoFindings()
    {
        const string html = "<html lang='nl'><head><title>Nieuws</title></head><body><main><h1>Nieuws</h1><h2>Vandaag</h2><img src='news.jpg' alt='Student leest nieuws'></main></body></html>";

        var findings = service.Inspect(html);

        Assert.Empty(findings);
    }
}
