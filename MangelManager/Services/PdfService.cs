using System;
using System.Collections.Generic;
using System.Linq;
using MangelManager.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MangelManager.Services;

public static class PdfService
{
    public static void Export(List<Mangel> maengel, string dateiPfad)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                // Header
                page.Header()
                    .Column(col =>
                    {
                        col.Item()
                            .Text("Mängelbericht")
                            .FontSize(24).Bold().FontColor(Colors.Blue.Darken3);
                        col.Item()
                            .PaddingTop(5)
                            .Text($"Erstellt am: {DateTime.Now:dd.MM.yyyy HH:mm} | Gesamtanzahl: {maengel.Count}")
                            .FontSize(12).FontColor(Colors.Grey.Darken2);
                    });

                // Content
                page.Content()
                    .PaddingVertical(20)
                    .Column(column =>
                    {
                        column.Spacing(10);

                        foreach (var mangel in maengel)
                        {
                            MangelEintrag(column.Item(), mangel);
                        }
                    });

                // Footer
                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("Seite ").FontSize(10);
                        text.CurrentPageNumber().FontSize(10);
                        text.Span(" von ").FontSize(10);
                        text.TotalPages().FontSize(10);
                    });
            });
        })
        .GeneratePdf(dateiPfad);
    }

    private static void MangelEintrag(IContainer container, Mangel mangel)
    {
        container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(10)
            .Column(col =>
            {
                col.Spacing(5);

                // Kopfzeile
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text(mangel.MangelNummer).FontSize(14).Bold();
                    row.RelativeItem()
                        .Text(GetStatusText(mangel.Status))
                        .FontSize(12)
                        .FontColor(GetStatusFarbe(mangel.Status));
                    row.RelativeItem()
                        .AlignRight()
                        .Text(GetPrioritaetText(mangel.Prioritaet))
                        .FontSize(12)
                        .FontColor(GetPrioritaetFarbe(mangel.Prioritaet));
                });

                col.Item().Text(mangel.Titel).FontSize(14).Bold().FontColor(Colors.Blue.Medium);

                col.Item().Text(mangel.Beschreibung).FontSize(11);

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Text($"Gewerk: {mangel.Gewerk}").FontSize(10);
                    row.RelativeItem().Text($"Ort: {mangel.Ort}").FontSize(10);
                    row.RelativeItem().Text($"Etage: {mangel.Etage}").FontSize(10);
                });

                col.Item().Row(row =>
                {
                    row.RelativeItem().Text($"Erfasst: {mangel.ErfasstAm:dd.MM.yyyy}").FontSize(10);
                    row.RelativeItem().Text($"Erfasser: {mangel.Erfasser}").FontSize(10);
                    row.RelativeItem().Text($"Zuständig: {mangel.Zustaendig}").FontSize(10);
                });

                if (mangel.FaelligAm.HasValue)
                {
                    col.Item().Text($"Fällig: {mangel.FaelligAm.Value:dd.MM.yyyy}").FontSize(10).FontColor(Colors.Red.Medium);
                }

                if (!string.IsNullOrWhiteSpace(mangel.Bemerkung))
                {
                    col.Item().Text($"Bemerkung: {mangel.Bemerkung}").FontSize(10);
                }

                if (mangel.HatFotos)
                {
                    col.Item().Text($"Fotos: {mangel.Fotos.Count}").FontSize(10).Italic();
                }
            });
    }

    private static string GetStatusText(MangelStatus status) => status switch
    {
        MangelStatus.Offen => "Offen",
        MangelStatus.InBearbeitung => "In Bearbeitung",
        MangelStatus.Erledigt => "Erledigt",
        MangelStatus.Abgelehnt => "Abgelehnt",
        _ => "Unbekannt"
    };

    private static string GetStatusFarbe(MangelStatus status) => status switch
    {
        MangelStatus.Offen => Colors.Red.Medium,
        MangelStatus.InBearbeitung => Colors.Orange.Medium,
        MangelStatus.Erledigt => Colors.Green.Medium,
        MangelStatus.Abgelehnt => Colors.Grey.Medium,
        _ => Colors.Black
    };

    private static string GetPrioritaetText(MangelPrioritaet p) => p switch
    {
        MangelPrioritaet.Niedrig => "Prio: Niedrig",
        MangelPrioritaet.Mittel => "Prio: Mittel",
        MangelPrioritaet.Hoch => "Prio: Hoch",
        MangelPrioritaet.Kritisch => "Prio: Kritisch",
        _ => ""
    };

    private static string GetPrioritaetFarbe(MangelPrioritaet p) => p switch
    {
        MangelPrioritaet.Niedrig => Colors.Grey.Medium,
        MangelPrioritaet.Mittel => Colors.Blue.Medium,
        MangelPrioritaet.Hoch => Colors.Orange.Medium,
        MangelPrioritaet.Kritisch => Colors.Red.Medium,
        _ => Colors.Black
    };
}
