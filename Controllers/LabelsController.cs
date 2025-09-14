using Microsoft.AspNetCore.Mvc;
using LabelDesignerAPI.Data;
using LabelDesignerAPI.Models;
using LabelDesignerAPI.Services;
using System.Text.Json;

namespace LabelDesignerAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LabelsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LabelsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("generate")]
        public IActionResult GenerateLabel([FromBody] LabelRequest request)
        {
            try
            {
                var template = _context.Templates.Find(request.TemplateId);
                if (template == null)
                    return NotFound("Template not found");

                // Validate required fields
                if (!string.IsNullOrEmpty(template.RequiredFields))
                {
                    var requiredFields = JsonSerializer.Deserialize<string[]>(template.RequiredFields);
                    var missingFields = requiredFields.Where(field => !request.Data.ContainsKey(field)).ToList();

                    if (missingFields.Any())
                        return BadRequest($"Missing required fields: {string.Join(", ", missingFields)}");
                }

                var pdfBytes = LabelGenerator.Generate(request, template.LayoutJson);

                return File(pdfBytes, "application/pdf", $"label-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error generating label: {ex.Message}");
            }
        }

        [HttpPost("preview")]
        public IActionResult PreviewLabel([FromBody] PreviewLabelRequest request)
        {
            try
            {
                // Generate preview with sample data or provided data
                var labelRequest = new LabelRequest
                {
                    TemplateId = 0, // Not used for preview
                    Data = request.Data ?? new Dictionary<string, string>
                    {
                        { "ProductName", "Sample Product" },
                        { "Price", "99.99" },
                        { "Code", "123456789" }
                    }
                };

                var pdfBytes = LabelGenerator.Generate(labelRequest, request.LayoutJson);

                return File(pdfBytes, "application/pdf", "preview.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error generating preview: {ex.Message}");
            }
        }
    }
}