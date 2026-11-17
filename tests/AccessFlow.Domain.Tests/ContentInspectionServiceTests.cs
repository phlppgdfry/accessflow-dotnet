using System.Text.Json;
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

    [Theory]
    [InlineData("<html data-lang='nl'>", "AF-001")]
    [InlineData("<html lang=''>", "AF-001")]
    [InlineData("<title>   </title>", "AF-002")]
    [InlineData("<img src='a.jpg' data-alt='x'>", "AF-004")]
    public void Inspect_LookalikeOrEmptyAttributes_AreStillReported(string fragment, string expectedRule)
    {
        var findings = service.Inspect($"<main><h1>Titel</h1>{fragment}</main>");

        Assert.Contains(findings, finding => finding.RuleCode == expectedRule);
    }

    [Theory]
    [InlineData("<html lang=nl><title>T</title>")]
    [InlineData("<HTML class='x' LANG=\"nl-BE\"><TITLE>\n  Nieuws\n</TITLE>")]
    public void Inspect_ValidLanguageAndTitleVariants_AreAccepted(string head)
    {
        var findings = service.Inspect($"{head}<main><h1>Titel</h1><img src='a.jpg' alt=''></main>");

        Assert.Empty(findings);
    }

    public static TheoryData<string> ParityCaseNames()
    {
        var data = new TheoryData<string>();
        foreach (var testCase in LoadParityCases())
        {
            data.Add(testCase.GetProperty("name").GetString()!);
        }

        return data;
    }

    // The same fixtures are checked against the browser port (scripts/check-browser-port.mjs).
    [Theory]
    [MemberData(nameof(ParityCaseNames))]
    public void Inspect_SharedParityCase_ReturnsExpectedRuleCodesInOrder(string name)
    {
        var testCase = LoadParityCases().Single(c => c.GetProperty("name").GetString() == name);
        var expected = testCase.GetProperty("expected").EnumerateArray().Select(code => code.GetString()!).ToArray();

        var actual = service.Inspect(testCase.GetProperty("html").GetString()!).Select(finding => finding.RuleCode).ToArray();

        Assert.Equal(expected, actual);
    }

    private static IEnumerable<JsonElement> LoadParityCases()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "parity", "inspection-cases.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.GetProperty("cases").EnumerateArray().Select(c => c.Clone()).ToList();
    }
}
