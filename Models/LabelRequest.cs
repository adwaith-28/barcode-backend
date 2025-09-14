namespace LabelDesignerAPI.Models
{
    public class LabelRequest
    {
        public int TemplateId { get; set; } // Which template to use
        public Dictionary<string, string> Data { get; set; } // Key-value pairs like { "ProductName": "Shampoo", "Price": "199" }
    }
}
