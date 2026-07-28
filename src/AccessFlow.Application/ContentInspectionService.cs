using System.Text.RegularExpressions;
using AccessFlow.Domain;

namespace AccessFlow.Application;

public sealed partial class ContentInspectionService
{
    public IReadOnlyList<AccessibilityFinding> Inspect(string html)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(html);

        var findings = new List<AccessibilityFinding>();

        if (!HtmlLanguageRegex().IsMatch(html))
        {
            findings.Add(Finding(
                "AF-001",
                FindingSeverity.High,
                "Documenttaal ontbreekt",
                "De HTML bevat geen lang-attribuut op het html-element.",
                "Declareer de primaire taal, bijvoorbeeld <html lang=\"nl\">."));
        }

        if (!TitleRegex().IsMatch(html))
        {
            findings.Add(Finding(
                "AF-002",
                FindingSeverity.Medium,
                "Paginatitel ontbreekt",
                "Er is geen niet-lege title-tag gevonden.",
                "Voeg een unieke, beschrijvende paginatitel toe."));
        }

        if (!MainRegex().IsMatch(html))
        {
            findings.Add(Finding(
                "AF-003",
                FindingSeverity.Medium,
                "Hoofdinhoud is niet gemarkeerd",
                "Er is geen main-landmark gevonden.",
                "Plaats de primaire inhoud in een <main>-element."));
        }

        if (ImageWithoutAltRegex().IsMatch(html))
        {
            findings.Add(Finding(
                "AF-004",
                FindingSeverity.High,
                "Afbeelding zonder alternatief",
                "Minstens één afbeelding heeft geen alt-attribuut.",
                "Gebruik betekenisvolle alt-tekst of een lege alt voor decoratieve beelden."));
        }

        if (VagueLinkRegex().IsMatch(html))
        {
            findings.Add(Finding(
                "AF-005",
                FindingSeverity.Medium,
                "Onduidelijke linktekst",
                "Een link gebruikt generieke tekst zoals ‘klik hier’ of ‘lees meer’.",
                "Maak de linktekst zelfstandig begrijpelijk, ook buiten de omringende context."));
        }

        var headingLevels = HeadingRegex()
            .Matches(html)
            .Select(match => int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture))
            .ToList();

        if (headingLevels.Count == 0 || headingLevels[0] != 1 || HasSkippedHeadingLevel(headingLevels))
        {
            findings.Add(Finding(
                "AF-006",
                FindingSeverity.Medium,
                "Headingstructuur verdient controle",
                "De headinghiërarchie start niet met h1 of slaat een niveau over.",
                "Gebruik één duidelijke h1 en verhoog headingniveaus stapsgewijs."));
        }

        return findings;
    }

    private static AccessibilityFinding Finding(
        string code,
        FindingSeverity severity,
        string title,
        string detail,
        string recommendation) => new(code, severity, title, detail, recommendation);

    private static bool HasSkippedHeadingLevel(IReadOnlyList<int> levels)
    {
        for (var index = 1; index < levels.Count; index++)
        {
            if (levels[index] > levels[index - 1] + 1)
            {
                return true;
            }
        }

        return false;
    }

    [GeneratedRegex("<html[^>]*\\blang\\s*=", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HtmlLanguageRegex();

    [GeneratedRegex("<title[^>]*>\\s*.+?\\s*</title>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex TitleRegex();

    [GeneratedRegex("<main(?:\\s|>)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MainRegex();

    [GeneratedRegex("<img(?![^>]*\\balt\\s*=)[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ImageWithoutAltRegex();

    [GeneratedRegex("<a[^>]*>\\s*(klik hier|lees meer|click here|read more)\\s*</a>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex VagueLinkRegex();

    [GeneratedRegex("<h([1-6])(?:\\s|>)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HeadingRegex();
}
