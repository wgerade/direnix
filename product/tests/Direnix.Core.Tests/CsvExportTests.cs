using Direnix.Core.Findings;
using Direnix.Core.Reporting;
using Direnix.Core.Storage;
using Xunit;

namespace Direnix.Core.Tests;

public class CsvExportTests
{
    private static FindingRow Row(string title, string obj) => new(
        StableFindingKey: "k1", RuleId: "AD-TEST-1", Title: title, Category: FindingCategory.Hygiene,
        Severity: Severity.High, Decision: FindingDecision.Adjust, Status: FindingStatus.Open,
        BusinessRiskScore: 50, ObjectDisplay: obj, FirstSeen: DateTimeOffset.UtcNow, LastSeen: DateTimeOffset.UtcNow,
        EvidenceJson: "{}", LastRunId: "run", ObjectKey: "ok", ResolutionReason: null);

    [Fact]
    public void FindingsCsv_HasNoSepLine()
    {
        var csv = ReportBuilder.BuildFindingsCsv(new[] { Row("t", "o") }, "pt");
        Assert.DoesNotContain("sep=", csv);
        Assert.StartsWith("Severidade", csv); // primeira linha é o cabeçalho, não a diretiva
    }

    [Fact]
    public void FindingsCsv_UsesSemicolonForPt_CommaForEn()
    {
        var pt = ReportBuilder.BuildFindingsCsv(new[] { Row("t", "o") }, "pt");
        var en = ReportBuilder.BuildFindingsCsv(new[] { Row("t", "o") }, "en");
        Assert.Contains("Severidade;Categoria;RuleId", pt);
        Assert.Contains("Severity,Category,RuleId", en);
    }

    [Fact]
    public void FindingsCsv_QuotesFieldContainingDelimiter()
    {
        // Título com ';' precisa ser aspado no CSV pt (delimitador ';').
        var pt = ReportBuilder.BuildFindingsCsv(new[] { Row("nome; com ponto e virgula", "o") }, "pt");
        Assert.Contains("\"nome; com ponto e virgula\"", pt);
        // O mesmo título NÃO é aspado no en (delimitador ','), pois não contém vírgula.
        var en = ReportBuilder.BuildFindingsCsv(new[] { Row("nome; com ponto e virgula", "o") }, "en");
        Assert.Contains("nome; com ponto e virgula", en);
        Assert.DoesNotContain("\"nome; com ponto e virgula\"", en);
    }

    [Fact]
    public void InventoryCsv_HasNoSepLine_AndLocaleDelimiter()
    {
        var inv = new[] { new InventoryState("User", 10, DateTimeOffset.UtcNow, true) };
        var pt = ReportBuilder.BuildInventoryCsv(inv, "pt");
        var en = ReportBuilder.BuildInventoryCsv(inv, "en");
        Assert.DoesNotContain("sep=", pt);
        Assert.Contains("Tipo;Total", pt);
        Assert.Contains("Type,Total", en);
    }
}
