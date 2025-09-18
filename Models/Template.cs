using System.ComponentModel.DataAnnotations;

namespace LabelDesignerAPI.Models
{
    public class Template
    {
        [Key]
        public int TemplateId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        // JSON structure defining the layout
        public string LayoutJson { get; set; }

        // Template dimensions
        public double Width { get; set; } = 300; // in points
        public double Height { get; set; } = 200; // in points

        // Required data fields for this template
        public string RequiredFields { get; set; } // JSON array: ["ProductName", "Price", "Code"]

        // Preview image (base64 or URL)
        public string PreviewImage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;  

        // Template category/type
        public string Category { get; set; } = "Product";

        // Is this a public template or user-specific
        public bool IsPublic { get; set; } = true;

        // Template status
        public bool IsActive { get; set; } = true;
    }
}