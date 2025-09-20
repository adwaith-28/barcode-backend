namespace LabelDesignerAPI.Models
{
    public class CreateTemplateRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string LayoutJson { get; set; }
        public double Width { get; set; } = 300;
        public double Height { get; set; } = 200;
        public string[] RequiredFields { get; set; }
        public string Category { get; set; } = "Product";
        public bool IsPublic { get; set; } = true;
        public string PreviewImage { get; set; } = string.Empty;
    }

    public class UpdateTemplateRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string LayoutJson { get; set; }
        public double? Width { get; set; }
        public double? Height { get; set; }
        public string[] RequiredFields { get; set; }
        public string PreviewImage { get; set; }
    }

    public class DuplicateTemplateRequest
    {
        public string Name { get; set; }
        public bool? IsPublic { get; set; }
    }

    public class PreviewLabelRequest
    {
        public int TemplateId { get; set; }
        public string LayoutJson { get; set; }
        public Dictionary<string, string> Data { get; set; }
        public string Format { get; set; } = "pdf";
    }
}