using LabelDesignerAPI.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.Json;

namespace LabelDesignerAPI.Services
{
    public static class LabelGenerator
    {
        public static byte[] Generate(LabelRequest request, string layoutJson)
        {
            try
            {
                Console.WriteLine($"Generating label with layout: {layoutJson}");

                // Parse the layout JSON
                TemplateLayout layout = null;
                
                if (!string.IsNullOrEmpty(layoutJson) && layoutJson != "{}")
                {
                    layout = JsonSerializer.Deserialize<TemplateLayout>(layoutJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }

                // If no custom layout, use default
                if (layout == null || !layout.Elements.Any())
                {
                    Console.WriteLine("No custom layout found, using default template");
                    return GenerateDefaultLabel(request);
                }

                Console.WriteLine($"Using custom layout with {layout.Elements.Count} elements");

                // Generate PDF with custom layout
                var pdf = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        // Use custom dimensions or default to A6
                        var pageWidth = layout.Width > 0 ? layout.Width : 300;
                        var pageHeight = layout.Height > 0 ? layout.Height : 200;
                        
                        page.Size(new PageSize((float)pageWidth, (float)pageHeight, Unit.Point));
                        page.Margin(0); // No margin for precise positioning

                        // Use absolute positioning for drag-and-drop elements
                        page.Content().Container().Height((float)pageHeight).Width((float)pageWidth).Layers(layers =>
                        {
                            // Background layer
                            if (!string.IsNullOrEmpty(layout.BackgroundColor) && layout.BackgroundColor != "#FFFFFF")
                            {
                                layers.PrimaryLayer().Container()
                                    .Width((float)pageWidth).Height((float)pageHeight)
                                    .Background(layout.BackgroundColor);
                            }

                            // Render each element in order of zIndex
                            foreach (var element in layout.Elements.OrderBy(e => e.ZIndex))
                            {
                                RenderElementWithPosition(layers, element, request.Data, pageHeight);
                            }
                        });
                    });
                });

                var result = pdf.GeneratePdf();
                Console.WriteLine($"PDF generated successfully: {result.Length} bytes");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Custom layout PDF generation error: {ex}");
                Console.WriteLine("Falling back to default template");
                return GenerateDefaultLabel(request);
            }
        }

        private static void RenderElementWithPosition(LayersDescriptor layers, LayoutElement element, Dictionary<string, string> data, double pageHeight)
        {
            // Convert coordinates (frontend might use top-left, PDF uses bottom-left)
            var x = (float)element.X;
            var y = (float)(pageHeight - element.Y - element.Height); // Flip Y coordinate
            var width = (float)element.Width;
            var height = (float)element.Height;

            layers.Layer().Container()
                .TranslateX(x)
                .TranslateY(y)
                .Width(width)
                .Height(height)
                .Component(new ElementRenderer(element, data));
        }

        private static byte[] GenerateDefaultLabel(LabelRequest request)
        {
            // Your original working code as fallback
            var productName = request.Data.ContainsKey("ProductName") ? request.Data["ProductName"] : "Unknown";
            var price = request.Data.ContainsKey("Price") ? request.Data["Price"] : "0";
            var code = request.Data.ContainsKey("Code") ? request.Data["Code"] : "123456";

            var barcodeBytes = BarcodeService.GenerateBarcode(code);
            var qrBytes = BarcodeService.GenerateQRCode(code);

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
    }

    // Custom component for rendering individual elements
    public class ElementRenderer : IComponent
    {
        private readonly LayoutElement _element;
        private readonly Dictionary<string, string> _data;

        public ElementRenderer(LayoutElement element, Dictionary<string, string> data)
        {
            _element = element;
            _data = data;
        }

        public void Compose(IContainer container)
        {
            switch (_element.Type.ToLower())
            {
                case "text":
                    RenderText(container);
                    break;
                case "barcode":
                    RenderBarcode(container);
                    break;
                case "qrcode":
                    RenderQRCode(container);
                    break;
                case "image":
                    RenderImage(container);
                    break;
                case "rectangle":
                    RenderRectangle(container);
                    break;
                default:
                    // Render placeholder for unknown elements
                    container.Text($"[{_element.Type}]").FontSize(8);
                    break;
            }
        }

        private void RenderText(IContainer container)
        {
            // Get text content
            string text = "Sample Text";
            if (_element.Properties.ContainsKey("dataField") && _element.Properties["dataField"] != null)
            {
                var dataField = _element.Properties["dataField"].ToString();
                if (_data.ContainsKey(dataField))
                    text = _data[dataField];
            }
            else if (_element.Properties.ContainsKey("text"))
            {
                text = _element.Properties["text"]?.ToString() ?? "Sample Text";
            }

            // Apply styling
            var textContainer = container.Text(text);
            
            if (_element.Properties.ContainsKey("fontSize") && int.TryParse(_element.Properties["fontSize"].ToString(), out int fontSize))
                textContainer = textContainer.FontSize(fontSize);
            else
                textContainer = textContainer.FontSize(12);

            if (_element.Properties.ContainsKey("color"))
                textContainer = textContainer.FontColor(_element.Properties["color"].ToString());

            if (_element.Properties.ContainsKey("fontWeight") && _element.Properties["fontWeight"].ToString() == "bold")
                textContainer = textContainer.Bold();

            // Alignment
            var alignment = _element.Properties.ContainsKey("alignment") ? _element.Properties["alignment"].ToString() : "left";
            switch (alignment.ToLower())
            {
                case "center":
                    container.AlignCenter();
                    break;
                case "right":
                    container.AlignRight();
                    break;
                default:
                    container.AlignLeft();
                    break;
            }
        }

        private void RenderBarcode(IContainer container)
        {
            string barcodeData = "123456";
            if (_element.Properties.ContainsKey("dataField") && _element.Properties["dataField"] != null)
            {
                var dataField = _element.Properties["dataField"].ToString();
                if (_data.ContainsKey(dataField))
                    barcodeData = _data[dataField];
            }

            try
            {
                var barcodeBytes = BarcodeService.GenerateBarcode(barcodeData);
                container.Image(barcodeBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Barcode generation failed: {ex.Message}");
                container.Text($"Barcode: {barcodeData}").FontSize(8);
            }
        }

        private void RenderQRCode(IContainer container)
        {
            string qrData = "123456";
            if (_element.Properties.ContainsKey("dataField") && _element.Properties["dataField"] != null)
            {
                var dataField = _element.Properties["dataField"].ToString();
                if (_data.ContainsKey(dataField))
                    qrData = _data[dataField];
            }

            try
            {
                var qrBytes = BarcodeService.GenerateQRCode(qrData);
                container.Image(qrBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"QR generation failed: {ex.Message}");
                container.Text($"QR: {qrData}").FontSize(8);
            }
        }

        private void RenderImage(IContainer container)
        {
            if (_element.Properties.ContainsKey("imageData") && _element.Properties["imageData"] != null)
            {
                try
                {
                    var imageData = _element.Properties["imageData"].ToString();
                    if (imageData.Contains(",")) // Remove data:image/png;base64, prefix if present
                        imageData = imageData.Split(',')[1];
                    
                    var imageBytes = Convert.FromBase64String(imageData);
                    container.Image(imageBytes);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Image rendering failed: {ex.Message}");
                    container.Text("[Image Error]").FontSize(8);
                }
            }
            else
            {
                container.Text("[No Image]").FontSize(8);
            }
        }

        private void RenderRectangle(IContainer container)
        {
            var fillColor = _element.Properties.ContainsKey("fillColor") ? _element.Properties["fillColor"].ToString() : "#CCCCCC";
            var borderColor = _element.Properties.ContainsKey("borderColor") ? _element.Properties["borderColor"].ToString() : "#000000";
            var borderWidth = 1f;

            if (_element.Properties.ContainsKey("borderWidth") && float.TryParse(_element.Properties["borderWidth"].ToString(), out float parsedWidth))
                borderWidth = parsedWidth;

            container.Background(fillColor).Border(borderWidth).BorderColor(borderColor);
        }
    }
}