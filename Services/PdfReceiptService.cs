using System.Globalization;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace AriasEmpireERP.Services;

public sealed record ReceiptField(string Label, string Value);

public sealed class PdfReceiptService
{
    private static readonly CultureInfo ColombianCulture = CultureInfo.GetCultureInfo("es-CO");

    public byte[] Create(string title, string number, DateTime createdAtUtc, IEnumerable<ReceiptField> fields)
    {
        PdfSharp.Fonts.GlobalFontSettings.UseWindowsFontsUnderWindows = true;
        using var document = new PdfDocument();
        document.Info.Title = $"{title} {number}";
        document.Info.CreationDate = DateTime.UtcNow;
        var page = document.AddPage();
        var graphics = XGraphics.FromPdfPage(page);
        var headingFont = new XFont("Arial", 18, XFontStyleEx.Bold);
        var subheadingFont = new XFont("Arial", 12, XFontStyleEx.Bold);
        var bodyFont = new XFont("Arial", 10, XFontStyleEx.Regular);
        var smallFont = new XFont("Arial", 8, XFontStyleEx.Regular);
        var y = 55d;

        graphics.DrawString("ARIAS EMPIRE", headingFont, XBrushes.Navy, new XPoint(50, y));
        y += 28;
        graphics.DrawString(title, subheadingFont, XBrushes.Black, new XPoint(50, y));
        graphics.DrawString(number, subheadingFont, XBrushes.SteelBlue, new XRect(300, y - 16, page.Width.Point - 350, 24), XStringFormats.TopRight);
        y += 24;
        graphics.DrawString($"Fecha: {createdAtUtc.ToLocalTime():yyyy-MM-dd HH:mm}", bodyFont, XBrushes.Black, new XPoint(50, y));
        y += 18;
        graphics.DrawLine(XPens.LightGray, 50, y, page.Width.Point - 50, y);
        y += 24;

        foreach (var field in fields)
        {
            if (y > page.Height.Point - 75)
            {
                graphics.Dispose();
                page = document.AddPage();
                graphics = XGraphics.FromPdfPage(page);
                y = 55;
            }

            graphics.DrawString(field.Label, subheadingFont, XBrushes.DarkSlateGray, new XRect(50, y, 150, 34), XStringFormats.TopLeft);
            graphics.DrawString(field.Value, bodyFont, XBrushes.Black, new XRect(205, y, page.Width.Point - 255, 40), XStringFormats.TopLeft);
            y += 42;
        }

        graphics.DrawLine(XPens.LightGray, 50, page.Height.Point - 55, page.Width.Point - 50, page.Height.Point - 55);
        graphics.DrawString("Comprobante generado por Arias Empire ERP.", smallFont, XBrushes.Gray, new XPoint(50, page.Height.Point - 38));
        graphics.Dispose();

        using var output = new MemoryStream();
        document.Save(output, closeStream: false);
        return output.ToArray();
    }

    public static string Currency(decimal value) => value.ToString("C2", ColombianCulture);
}
