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
                    try
                    {
                        layout = JsonSerializer.Deserialize<TemplateLayout>(layoutJson, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        Console.WriteLine($"Successfully parsed layout with {layout?.Elements?.Count ?? 0} elements");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing layout JSON: {ex.Message}");
                        Console.WriteLine($"Layout JSON was: {layoutJson}");
                    }
                }

                // If no custom layout, create a minimal layout to avoid blank pages
                if (layout == null || !layout.Elements.Any())
                {
                    Console.WriteLine("No custom layout found, creating minimal layout");
                    layout = new TemplateLayout
                    {
                        Width = 400,
                        Height = 300,
                        BackgroundColor = "#FFFFFF",
                        Elements = new List<LayoutElement>()
                    };
                }

                Console.WriteLine($"Using custom layout with {layout.Elements.Count} elements");

                // Generate PDF with custom layout
                var pdf = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        // Use custom dimensions or default
                        var pageWidth = layout.Width > 0 ? layout.Width : 300;
                        var pageHeight = layout.Height > 0 ? layout.Height : 200;

                        page.Size(new PageSize((float)pageWidth, (float)pageHeight, Unit.Point));
                        page.Margin(0);

                        // Render entire page in a single container; use layers so content doesn't expand layout
                        page.Content()
                            .Container()
                            .Width((float)pageWidth)
                            .Height((float)pageHeight)
                            .Layers(layers =>
                            {
                                // Always add a primary layer for the background
                                layers.PrimaryLayer()
                                    .Container()
                                    .Width((float)pageWidth)
                                    .Height((float)pageHeight)
                                    .Background(layout.BackgroundColor ?? "#FFFFFF");

                                // Elements layer (absolute positioning via translate)
                                foreach (var element in layout.Elements.OrderBy(e => e.ZIndex))
                                {
                                    layers.Layer()
                                        .Container()
                                        .TranslateX((float)element.X)
                                        .TranslateY((float)element.Y)
                                        .Width((float)element.Width)
                                        .Height((float)element.Height)
                                        .Rotate(element.Properties.TryGetValue("rotation", out var rotationObj) && 
                                               float.TryParse(rotationObj?.ToString(), out float rotation) ? rotation : 0f)
                                        .Component(new ElementRenderer(element, request.Data ?? new Dictionary<string, string>()));
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

        private static byte[] GenerateDefaultLabel(LabelRequest request)
        {
            var data = request.Data ?? new Dictionary<string, string>();
            var productName = data.GetValueOrDefault("ProductName", "Sample Product");
            var price = data.GetValueOrDefault("Price", "99.99");
            var code = data.GetValueOrDefault("Code", "123456789");

            try
            {
                var barcodeBytes = BarcodeService.GenerateBarcode(code);
                var qrBytes = BarcodeService.GenerateQRCode(code);

                var pdf = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(300, 200, Unit.Point);
                        page.Margin(10);

                        page.Content().Stack(stack =>
                        {
                            // Header with product name
                            stack.Item().Text(productName).FontSize(14).Bold();

                            // Price
                            stack.Item().PaddingTop(5).Text($"₹{price}").FontSize(12);

                            // Barcode (constrain rendering to available box)
                            stack.Item()
                                .PaddingTop(10)
                                .Height(40)
                                .Image(barcodeBytes, ImageScaling.FitArea);

                            // QR Code (square box to avoid aspect ratio overflow)
                            stack.Item()
                                .PaddingTop(5)
                                .AlignRight()
                                .Width(40)
                                .Height(40)
                                .Image(qrBytes, ImageScaling.FitArea);

                            // Code text
                            stack.Item().PaddingTop(5).Text(code).FontSize(8).FontColor("#666666");
                        });
                    });
                });

                return pdf.GeneratePdf();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Default label generation error: {ex}");
                return GenerateErrorLabel(ex.Message);
            }
        }

        private static byte[] GenerateErrorLabel(string errorMessage)
        {
            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(300, 200, Unit.Point);
                    page.Margin(20);
                    page.Content().Text($"Error generating label: {errorMessage}").FontSize(12).FontColor("#FF0000");
                });
            });
            return pdf.GeneratePdf();
        }
    }

    // Improved Element Renderer 
    public class ElementRenderer : IComponent
    {
        private readonly LayoutElement _element;
        private readonly Dictionary<string, string> _data;

        public ElementRenderer(LayoutElement element, Dictionary<string, string> data)
        {
            _element = element;
            _data = data ?? new Dictionary<string, string>();
        }

        public void Compose(IContainer container)
        {
            try
            {
                switch (_element.Type.ToLower())
                {
                    case "text":
                    case "dynamic-text":
                    case "product-code":
                        RenderText(container);
                        break;
                    case "barcode":
                        RenderBarcode(container);
                        break;
                    case "qrcode":
                        RenderQRCode(container);
                        break;
                    case "image":
                    case "logo":
                        RenderImage(container);
                        break;
                    case "dynamic-image":
                        RenderDynamicImage(container);
                        break;
                    case "rectangle":
                        RenderRectangle(container);
                        break;
                    case "line":
                        RenderLine(container);
                        break;
                    default:
                        container.Text($"[Unknown: {_element.Type}]").FontSize(8).FontColor("#FF0000");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rendering element {_element.Type}: {ex.Message}");
                container.Text($"[Error]").FontSize(8).FontColor("#FF0000");
            }
        }

        // ---------- TEXT ----------
        private void RenderText(IContainer container)
        {
            string text = GetTextContent();
            
            // Apply all styling in a single fluent chain to avoid container reuse
            var textStyle = container.Text(text);
            
            // Font size
            if (_element.Properties.TryGetValue("fontSize", out var fontSizeObj) &&
                int.TryParse(fontSizeObj?.ToString(), out int fontSize))
                textStyle.FontSize(Math.Max(6, Math.Min(72, fontSize))); 
            else
                textStyle.FontSize(12);

            // Color
            if (_element.Properties.TryGetValue("color", out var colorObj) && colorObj != null)
            {
                var color = colorObj.ToString();
                if (!string.IsNullOrEmpty(color))
                    textStyle.FontColor(color);
            }

            // Bold
            if (_element.Properties.TryGetValue("fontWeight", out var weightObj) &&
                weightObj?.ToString()?.ToLower() == "bold")
                textStyle.Bold();

            // Italic
            if (_element.Properties.TryGetValue("fontStyle", out var styleObj) &&
                styleObj?.ToString()?.ToLower() == "italic")
                textStyle.Italic();
        }

        private string GetTextContent()
        {
            // Priority: bind to request.Data via dataField
            if (_element.Properties.TryGetValue("dataField", out var dataFieldObj) && dataFieldObj != null)
            {
                var dataField = dataFieldObj.ToString();
                if (_data.TryGetValue(dataField, out var dataValue))
                    return dataValue;
            }

            // Frontend uses "content"
            if (_element.Properties.TryGetValue("content", out var contentObj) && contentObj != null)
                return contentObj.ToString();

            // Backend originally used "text"
            if (_element.Properties.TryGetValue("text", out var textObj) && textObj != null)
                return textObj.ToString();

            return "Sample Text";
        }


        private void RenderBarcode(IContainer container)
        {
            string barcodeData = null;

            // Try to get data from the request using dataField (user input takes priority)
            barcodeData = GetDataFieldValue("dataField", null);

            // If no data from user input, try to get static data from the element's properties
            if (string.IsNullOrEmpty(barcodeData) &&
                _element.Properties.TryGetValue("data", out var staticDataObj) && staticDataObj != null)
            {
                barcodeData = staticDataObj.ToString();
            }

            // If still no data, use a default
            if (string.IsNullOrEmpty(barcodeData))
                barcodeData = "123456789";

            try
            {
                var barcodeBytes = BarcodeService.GenerateBarcode(barcodeData);

                // Force scaling into element's width/height from layoutJson
                container
                    .Width((float)_element.Width)
                    .Height((float)_element.Height)
                    .Image(barcodeBytes, ImageScaling.FitArea);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Barcode generation failed: {ex.Message}");
                container.Text(barcodeData).FontSize(8);
            }
        }

        private void RenderQRCode(IContainer container)
        {
            string qrData = null;

            // Try to get data from the request using dataField (user input takes priority)
            qrData = GetDataFieldValue("dataField", null);

            // If no data from user input, try to get static data from the element's properties
            if (string.IsNullOrEmpty(qrData) &&
                _element.Properties.TryGetValue("data", out var staticDataObj) && staticDataObj != null)
            {
                qrData = staticDataObj.ToString();
            }

            // If still no data, use a default
            if (string.IsNullOrEmpty(qrData))
                qrData = "123456789";

            try
            {
                var qrBytes = BarcodeService.GenerateQRCode(qrData);

                container
                    .Width((float)_element.Width)
                    .Height((float)_element.Height)
                    .Image(qrBytes, ImageScaling.FitArea);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"QR generation failed: {ex.Message}");
                container.Text(qrData).FontSize(8);
            }
        }


        private void RenderImage(IContainer container)
        {
            try
            {
                byte[]? imageBytes = null;

                // Backend schema
                if (_element.Properties.TryGetValue("imageData", out var imageDataObj) && imageDataObj != null)
                {
                    var imageData = imageDataObj.ToString();
                    if (imageData.Contains(",")) imageData = imageData.Split(',')[1];
                    imageBytes = Convert.FromBase64String(imageData);
                }

                // Frontend schema
                if (imageBytes == null && _element.Properties.TryGetValue("src", out var srcObj) && srcObj != null)
                {
                    var imageData = srcObj.ToString();
                    if (imageData.Contains(",")) imageData = imageData.Split(',')[1];
                    imageBytes = Convert.FromBase64String(imageData);
                }

                if (imageBytes != null)
                {
                    container
                        .Width((float)_element.Width)
                        .Height((float)_element.Height)
                        .Image(imageBytes, ImageScaling.FitArea);
                }
                else
                {
                    container
                        .Width((float)_element.Width)
                        .Height((float)_element.Height)
                        .Background("#F0F0F0")
                        .AlignCenter()
                        .AlignMiddle()
                        .Text("[Image]").FontSize(10);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Image rendering failed: {ex.Message}");
                container
                    .Width((float)_element.Width)
                    .Height((float)_element.Height)
                    .Background("#FFE0E0")
                    .AlignCenter()
                    .AlignMiddle()
                    .Text("[Image Error]").FontSize(8);
            }
        }

        private void RenderDynamicImage(IContainer container)
        {
            try
            {
                // Try to get data from the request using element ID (for dynamic-image elements)
                string imageData = null;
                if (_data.TryGetValue(_element.Id, out var elementData))
                {
                    imageData = elementData;
                }

                // If no data from element ID, try to get data from dataField
                if (string.IsNullOrEmpty(imageData))
                {
                    imageData = GetDataFieldValue("dataField", null);
                }

                // If no data from user input, try to get static data from the element's properties
                if (string.IsNullOrEmpty(imageData) &&
                    _element.Properties.TryGetValue("data", out var staticDataObj) && staticDataObj != null)
                {
                    imageData = staticDataObj.ToString();
                }

                // If still no data, use a default placeholder
                if (string.IsNullOrEmpty(imageData))
                {
                    container
                        .Width((float)_element.Width)
                        .Height((float)_element.Height)
                        .Background("#E0E0FF")
                        .AlignCenter()
                        .AlignMiddle()
                        .Text("[Dynamic Image]").FontSize(10);
                    return;
                }

                // Try to parse as base64 image data
                byte[]? imageBytes = null;
                try
                {
                    if (imageData.Contains(","))
                    {
                        imageData = imageData.Split(',')[1];
                    }
                    imageBytes = Convert.FromBase64String(imageData);
                }
                catch
                {
                    // If not base64, treat as placeholder text
                    container
                        .Width((float)_element.Width)
                        .Height((float)_element.Height)
                        .Background("#E0E0FF")
                        .AlignCenter()
                        .AlignMiddle()
                        .Text(imageData).FontSize(10);
                    return;
                }

                if (imageBytes != null)
                {
                    container
                        .Width((float)_element.Width)
                        .Height((float)_element.Height)
                        .Image(imageBytes, ImageScaling.FitArea);
                }
                else
                {
                    container
                        .Width((float)_element.Width)
                        .Height((float)_element.Height)
                        .Background("#E0E0FF")
                        .AlignCenter()
                        .AlignMiddle()
                        .Text("[Dynamic Image]").FontSize(10);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Dynamic image rendering failed: {ex.Message}");
                container
                    .Width((float)_element.Width)
                    .Height((float)_element.Height)
                    .Background("#FFE0E0")
                    .AlignCenter()
                    .AlignMiddle()
                    .Text("[Image Error]").FontSize(8);
            }
        }


        // ---------- RECTANGLE ----------
        private void RenderRectangle(IContainer container)
        {
            var fillColor = GetPropertyValue("fillColor", GetPropertyValue("fill", "#CCCCCC"));
            var borderColor = GetPropertyValue("borderColor", GetPropertyValue("stroke", "#000000"));
            var borderWidth = GetFloatProperty("borderWidth", GetFloatProperty("strokeWidth", 1f));

            container.Background(fillColor).Border(borderWidth).BorderColor(borderColor);
        }

        // ---------- LINE ----------
        private void RenderLine(IContainer container)
        {
            var lineColor = GetPropertyValue("color", GetPropertyValue("stroke", "#000000"));
            var lineWidth = GetFloatProperty("width", GetFloatProperty("strokeWidth", 1f));

            container.Height(lineWidth).Background(lineColor);
        }

        // ---------- HELPERS ----------
        private string GetDataFieldValue(string propertyKey, string defaultValue)
        {
            if (_element.Properties.TryGetValue(propertyKey, out var dataFieldObj) && dataFieldObj != null)
            {
                var dataField = dataFieldObj.ToString();
                if (_data.TryGetValue(dataField, out var value))
                    return value;
            }
            return defaultValue;
        }

        private string GetPropertyValue(string key, string defaultValue)
        {
            return _element.Properties.TryGetValue(key, out var value) && value != null
                ? value.ToString()
                : defaultValue;
        }

        private float GetFloatProperty(string key, float defaultValue)
        {
            if (_element.Properties.TryGetValue(key, out var value) && value != null &&
                float.TryParse(value.ToString(), out float result))
                return result;
            return defaultValue;
        }
    }

}