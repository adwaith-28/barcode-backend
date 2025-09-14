using LabelDesignerAPI.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.IO;

namespace LabelDesignerAPI.Services
{
    public static class LabelGenerator
    {
        public static byte[] Generate(LabelRequest request, string templateJson)
        {
            try
            {
                var productName = request.Data.ContainsKey("ProductName") ? request.Data["ProductName"] : "Unknown";
                var price = request.Data.ContainsKey("Price") ? request.Data["Price"] : "0";
                var code = request.Data.ContainsKey("Code") ? request.Data["Code"] : "123456";

                var barcodeBytes = BarcodeService.GenerateBarcode(code);
                var qrBytes = BarcodeService.GenerateQRCode(code);

                // Optional debugging: save PNGs to disk
                File.WriteAllBytes("debug_barcode.png", barcodeBytes);
                File.WriteAllBytes("debug_qr.png", qrBytes);

                // Validate
                if (barcodeBytes == null || barcodeBytes.Length == 0)
                    throw new ArgumentException("Barcode generation failed");

                if (qrBytes == null || qrBytes.Length == 0)
                    throw new ArgumentException("QR code generation failed");

                // Generate PDF
                var pdf = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A6);
                        page.Margin(20);
                        page.DefaultTextStyle(x => x.FontSize(16));

                        page.Content().Column(col =>
                        {
                            col.Spacing(10);
                            col.Item().Text($"Product: {productName}");
                            col.Item().Text($"Price: ₹{price}");

                            col.Item().Height(80).Image(barcodeBytes);
                            col.Item().Height(80).Image(qrBytes);
                        });
                    });
                });

                return pdf.GeneratePdf();
            }
            catch (Exception ex)
            {
                // Log full details
                Console.WriteLine($"PDF generation error: {ex}");
                throw new ApplicationException("Label PDF generation failed", ex);
            }
        }
    }
}
