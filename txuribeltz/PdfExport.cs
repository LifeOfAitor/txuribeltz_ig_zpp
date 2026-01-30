using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace txuribeltz;

internal static class PdfExport
{
    internal static string EnsureOutputFolder()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "Txuribeltz");

        Directory.CreateDirectory(folder);
        return folder;
    }

    internal static void OpenFile(string path)
    {
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
}

internal sealed class Top10Document : IDocument
{
    private readonly IReadOnlyList<Top10Row> _rows;
    private readonly DateTime _generatedAt;

    internal sealed record Top10Row(int Rank, string Player, string? Elo);

    public Top10Document(IEnumerable<string> rawEntries)
    {
        _generatedAt = DateTime.Now;

        // Accepts either:
        //  - "name|elo"
        //  - "name:elo"
        //  - "name" (elo unknown)
        _rows = rawEntries
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select((entry, idx) =>
            {
                var (name, elo) = ParseEntry(entry);
                return new Top10Row(idx + 1, name, elo);
            })
            .Take(10)
            .ToList();
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30);
            page.DefaultTextStyle(x => x.FontSize(11));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().AlignCenter().Text(text =>
            {
                text.Span("Generated: ").SemiBold();
                text.Span(_generatedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
                text.Span("  |  Page ");
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("Txuribeltz - TOP 10").FontSize(18).SemiBold();
                    c.Item().Text("Leaderboard export").FontColor(Colors.Grey.Darken2);
                });

                row.ConstantItem(120).AlignRight()
                   .Text("PDF").FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
            });

            col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(15).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(40);    // Rank
                columns.RelativeColumn(3);     // Player
                columns.RelativeColumn(1);     // Elo
            });

            // Header
            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).Text("#");
                header.Cell().Element(HeaderCell).Text("Player");
                header.Cell().Element(HeaderCell).AlignRight().Text("ELO");
            });

            foreach (var r in _rows)
            {
                table.Cell().Element(BodyCell).Text(r.Rank.ToString(CultureInfo.InvariantCulture));
                table.Cell().Element(BodyCell).Text(r.Player);
                table.Cell().Element(BodyCell).AlignRight().Text(r.Elo ?? "-");
            }
        });
    }

    private static IContainer HeaderCell(IContainer c) =>
        c.PaddingVertical(6).PaddingHorizontal(8)
         .Background(Colors.Grey.Lighten3)
         .DefaultTextStyle(x => x.SemiBold());

    private static IContainer BodyCell(IContainer c) =>
        c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
         .PaddingVertical(6).PaddingHorizontal(8);

    private static (string name, string? elo) ParseEntry(string entry)
    {
        var trimmed = entry.Trim();

        // Prefer common separators
        var parts = trimmed.Split(new[] { '|', ':' }, 2);
        if (parts.Length == 2)
            return (parts[0].Trim(), parts[1].Trim());

        return (trimmed, null);
    }
}

internal sealed class UserStatsDocument : IDocument
{
    private readonly string _username;
    private readonly string _elo;
    private readonly string _wins;
    private readonly string _losses;
    private readonly string _winrate;
    private readonly DateTime _generatedAt;

    public UserStatsDocument(string username, string elo, string wins, string losses, string winrate)
    {
        _generatedAt = DateTime.Now;
        _username = username;
        _elo = elo;
        _wins = wins;
        _losses = losses;
        _winrate = winrate;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30);
            page.DefaultTextStyle(x => x.FontSize(11));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().AlignCenter().Text(_generatedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Text($"Txuribeltz - Player report").FontSize(18).SemiBold();
            col.Item().Text(_username).FontSize(13).FontColor(Colors.Blue.Darken2);
            col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(18).Column(col =>
        {
            col.Spacing(10);

            col.Item().Element(Card).Column(c =>
            {
                c.Item().Text("Summary").SemiBold().FontSize(13);
                c.Item().PaddingTop(8).Element(KeyValueTable);
            });
        });
    }

    private void KeyValueTable(IContainer container)
    {
        container.Table(t =>
        {
            t.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(2);
                cols.RelativeColumn(3);
            });

            Row(t, "ELO", _elo);
            Row(t, "Wins", _wins);
            Row(t, "Losses", _losses);
            Row(t, "Winrate", _winrate);
        });

        static void Row(TableDescriptor t, string k, string v)
        {
            t.Cell().Element(KeyCell).Text(k).SemiBold();
            t.Cell().Element(ValueCell).Text(v);
        }

        static IContainer KeyCell(IContainer c) => c.PaddingVertical(6).PaddingHorizontal(8).Background(Colors.Grey.Lighten4);
        static IContainer ValueCell(IContainer c) => c.PaddingVertical(6).PaddingHorizontal(8).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
    }

    private static IContainer Card(IContainer c) =>
        c.Border(1).BorderColor(Colors.Grey.Lighten2)
         .Background(Colors.White)
         .Padding(14);
}

internal sealed class PartidaKopuruaDocument : IDocument
{
    private readonly string _count;
    private readonly string _start;
    private readonly string _end;
    private readonly DateTime _generatedAt;

    public PartidaKopuruaDocument(string partidaKopurua, string dataHasiera, string dataAmaiera)
    {
        _generatedAt = DateTime.Now;
        _count = string.IsNullOrWhiteSpace(partidaKopurua) ? "0" : partidaKopurua.Trim();
        _start = dataHasiera?.Trim() ?? string.Empty;
        _end = dataAmaiera?.Trim() ?? string.Empty;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30);
            page.DefaultTextStyle(x => x.FontSize(11));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().AlignCenter().Text(text =>
            {
                text.Span("Generated: ").SemiBold();
                text.Span(_generatedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
                text.Span("  |  Page ");
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Text("Txuribeltz - Match count report").FontSize(18).SemiBold();

            if (!string.IsNullOrWhiteSpace(_start) || !string.IsNullOrWhiteSpace(_end))
            {
                col.Item().Text($"From: {_start}   To: {_end}")
                   .FontSize(12)
                   .FontColor(Colors.Grey.Darken2);
            }

            col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(25).Column(col =>
        {
            col.Spacing(16);

            col.Item().Element(Card).Column(c =>
            {
                c.Item().Text("Total matches played").FontSize(13).SemiBold();
                c.Item().PaddingTop(10);

                c.Item().AlignCenter().Text(_count)
                    .FontSize(48)
                    .SemiBold()
                    .FontColor(Colors.Blue.Darken2);
            });

            col.Item().Text("Note: date range is based on the dates selected in the admin panel.")
               .FontSize(10)
               .FontColor(Colors.Grey.Darken2);
        });
    }

    private static IContainer Card(IContainer c) =>
        c.Border(1).BorderColor(Colors.Grey.Lighten2)
         .Background(Colors.White)
         .Padding(18);
}