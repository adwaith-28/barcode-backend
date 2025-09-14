using System;
using System.ComponentModel.DataAnnotations;

namespace LabelDesignerAPI.Models
{
    public class Template
    {
        [Key]
        public int TemplateId { get; set; }
        public string Name { get; set; }
        public string TemplateJson { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
