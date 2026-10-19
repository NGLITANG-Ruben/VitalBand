using VitalBand.App.Models;

namespace VitalBand.App.Services;

public record PdfReport(
    string PatientName,
    DateTime From,
    DateTime To,
    List<Measurement> Measurements,      // déjà filtrées (période, types) et triées par date croissante
    List<Threshold> Thresholds,
    bool IncludeBp,
    bool IncludeGlycemia,
    bool IncludeCharts,
    bool IncludeComments);

public interface IPdfExporter
{
    // Retourne le chemin du fichier PDF créé (null en cas d'échec)
    Task<string?> CreateAsync(PdfReport report);
}

public class NoOpPdfExporter : IPdfExporter
{
    public Task<string?> CreateAsync(PdfReport report) => Task.FromResult<string?>(null);
}
