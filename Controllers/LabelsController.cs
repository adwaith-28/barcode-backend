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

        [HttpPost("generate-custom")]
        public IActionResult GenerateCustomLabel([FromBody] PreviewLabelRequest request)
        {
            try
            {
                // Use provided layout JSON or get from template
                string layoutJson = request.LayoutJson;
                if (string.IsNullOrEmpty(layoutJson) && request.TemplateId > 0)
                {
                    var template = _context.Templates.Find(request.TemplateId);
                    if (template != null)
                    {
                        layoutJson = template.LayoutJson;
                    }
                }

                if (string.IsNullOrEmpty(layoutJson))
                    return BadRequest("No layout JSON provided");

                // Create a label request for the generator
                var labelRequest = new LabelRequest
                {
                    TemplateId = request.TemplateId,
                    Data = request.Data ?? new Dictionary<string, string>()
                };

                var pdfBytes = LabelGenerator.Generate(labelRequest, layoutJson);

                return File(pdfBytes, "application/pdf", $"label-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error generating custom label: {ex.Message}");
            }
        }

        [HttpPost("preview")]
        public IActionResult PreviewLabel([FromBody] PreviewLabelRequest request)
        {
            try
            {
                // If templateId is provided, get the template from database
                string layoutJson = request.LayoutJson;
                if (request.TemplateId > 0)
                {
                    var template = _context.Templates.Find(request.TemplateId);
                    if (template != null)
                    {
                        layoutJson = template.LayoutJson;
                    }
                }

                // Generate preview with provided data or sample data
                var labelRequest = new LabelRequest
                {
                    TemplateId = request.TemplateId,
                    Data = request.Data ?? new Dictionary<string, string>
                    {
                        { "ProductName", "Sample Product" },
                        { "Price", "99.99" },
                        { "Code", "123456789" }
                    }
                };

                var pdfBytes = LabelGenerator.Generate(labelRequest, layoutJson);

                // Return appropriate content type based on format
                if (request.Format?.ToLower() == "png")
                {
                    // For now, return PDF. In a real implementation, you'd convert PDF to PNG
                    return File(pdfBytes, "application/pdf", "preview.pdf");
                }
                else
                {
                    return File(pdfBytes, "application/pdf", "preview.pdf");
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Error generating preview: {ex.Message}");
            }
        }
    }
}