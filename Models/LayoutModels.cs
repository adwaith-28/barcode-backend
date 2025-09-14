namespace LabelDesignerAPI.Models
{
    public class LayoutElement
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; } // "text", "barcode", "qrcode", "image", "line", "rectangle"
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public int ZIndex { get; set; } = 0;
        public Dictionary<string, object> Properties { get; set; } = new();
        public Dictionary<string, object> Style { get; set; } = new();
    }

    public class TemplateLayout
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public string BackgroundColor { get; set; } = "#FFFFFF";
        public List<LayoutElement> Elements { get; set; } = new();
        public Dictionary<string, object> Settings { get; set; } = new();
    }

    // Specific element types for better type safety
    public class TextElement : LayoutElement
    {
        public string Text { get; set; } = "Sample Text";
        public string DataField { get; set; } // Maps to request.Data["ProductName"]
        public string FontFamily { get; set; } = "Arial";
        public int FontSize { get; set; } = 12;
        public string FontWeight { get; set; } = "normal";
        public string Color { get; set; } = "#000000";
        public string Alignment { get; set; } = "left"; // left, center, right
    }

    public class BarcodeElement : LayoutElement
    {
        public string DataField { get; set; } = "Code";
        public string BarcodeType { get; set; } = "CODE_128";
        public bool ShowText { get; set; } = true;
    }

    public class QRCodeElement : LayoutElement
    {
        public string DataField { get; set; } = "Code";
        public string ErrorCorrectionLevel { get; set; } = "M";
    }

    public class ImageElement : LayoutElement
    {
        public string ImageUrl { get; set; }
        public string ImageData { get; set; } // Base64
        public string DataField { get; set; } // For dynamic images
        public bool MaintainAspectRatio { get; set; } = true;
    }
}