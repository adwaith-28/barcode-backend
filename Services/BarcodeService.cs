using ZXing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;
using System.IO;

namespace LabelDesignerAPI.Services
{
    public static class BarcodeService
    {
        public static byte[] GenerateBarcode(string text)
        {
            var writer = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.CODE_128,
                Options = new ZXing.Common.EncodingOptions
                {
                    Width = 300,
                    Height = 100,
                    Margin = 2
                }
            };

            var pixelData = writer.Write(text);

            using var image = new Image<Rgba32>(pixelData.Width, pixelData.Height);
            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < pixelData.Height; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (int x = 0; x < pixelData.Width; x++)
                    {
                        var idx = (y * pixelData.Width + x) * 4;
                        row[x] = new Rgba32(pixelData.Pixels[idx], pixelData.Pixels[idx + 1], pixelData.Pixels[idx + 2], pixelData.Pixels[idx + 3]);
                    }
                }
            });

            using var ms = new MemoryStream();
            image.Save(ms, new PngEncoder());
            return ms.ToArray();
        }

        public static byte[] GenerateQRCode(string text)
        {
            using var qrGenerator = new QRCoder.QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(text, QRCoder.QRCodeGenerator.ECCLevel.Q);
            var pngQrCode = new QRCoder.PngByteQRCode(qrCodeData);
            return pngQrCode.GetGraphic(20);
        }
    }
}
