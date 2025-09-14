using Microsoft.AspNetCore.Mvc;
using LabelDesignerAPI.Data;
using LabelDesignerAPI.Models;
using System.Text.Json;

namespace LabelDesignerAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TemplatesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TemplatesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetTemplates([FromQuery] string category = null, [FromQuery] bool activeOnly = true)
        {
            var query = _context.Templates.AsQueryable();

            if (activeOnly)
                query = query.Where(t => t.IsActive);

            if (!string.IsNullOrEmpty(category))
                query = query.Where(t => t.Category == category);

            var templates = query.OrderByDescending(t => t.CreatedAt).ToList();
            return Ok(templates);
        }

        [HttpGet("{id}")]
        public IActionResult GetTemplate(int id)
        {
            var template = _context.Templates.Find(id);
            if (template == null) return NotFound("Template not found");
            return Ok(template);
        }

        [HttpPost]
        public IActionResult CreateTemplate([FromBody] CreateTemplateRequest request)
        {
            try
            {
                // Validate layout JSON
                if (!string.IsNullOrEmpty(request.LayoutJson))
                {
                    JsonSerializer.Deserialize<TemplateLayout>(request.LayoutJson);
                }

                var template = new Template
                {
                    Name = request.Name,
                    Description = request.Description,
                    LayoutJson = request.LayoutJson ?? "{}",
                    Width = request.Width,
                    Height = request.Height,
                    RequiredFields = request.RequiredFields != null ?
                        JsonSerializer.Serialize(request.RequiredFields) : "[]",
                    Category = request.Category ?? "Product",
                    IsPublic = request.IsPublic,
                    PreviewImage = request.PreviewImage,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Templates.Add(template);
                _context.SaveChanges();

                return CreatedAtAction(nameof(GetTemplate), new { id = template.TemplateId }, template);
            }
            catch (JsonException)
            {
                return BadRequest("Invalid layout JSON format");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error creating template: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public IActionResult UpdateTemplate(int id, [FromBody] UpdateTemplateRequest request)
        {
            var template = _context.Templates.Find(id);
            if (template == null) return NotFound("Template not found");

            try
            {
                // Validate layout JSON if provided
                if (!string.IsNullOrEmpty(request.LayoutJson))
                {
                    JsonSerializer.Deserialize<TemplateLayout>(request.LayoutJson);
                    template.LayoutJson = request.LayoutJson;
                }

                if (!string.IsNullOrEmpty(request.Name))
                    template.Name = request.Name;

                if (!string.IsNullOrEmpty(request.Description))
                    template.Description = request.Description;

                if (request.Width.HasValue)
                    template.Width = request.Width.Value;

                if (request.Height.HasValue)
                    template.Height = request.Height.Value;

                if (!string.IsNullOrEmpty(request.PreviewImage))
                    template.PreviewImage = request.PreviewImage;

                if (request.RequiredFields != null)
                    template.RequiredFields = JsonSerializer.Serialize(request.RequiredFields);

                template.UpdatedAt = DateTime.UtcNow;
                _context.SaveChanges();

                return Ok(template);
            }
            catch (JsonException)
            {
                return BadRequest("Invalid layout JSON format");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating template: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteTemplate(int id)
        {
            var template = _context.Templates.Find(id);
            if (template == null) return NotFound("Template not found");

            _context.Templates.Remove(template);
            _context.SaveChanges();
            return NoContent();
        }

        [HttpPost("{id}/duplicate")]
        public IActionResult DuplicateTemplate(int id, [FromBody] DuplicateTemplateRequest request)
        {
            var originalTemplate = _context.Templates.Find(id);
            if (originalTemplate == null) return NotFound("Template not found");

            var duplicatedTemplate = new Template
            {
                Name = request.Name ?? $"{originalTemplate.Name} (Copy)",
                Description = originalTemplate.Description,
                LayoutJson = originalTemplate.LayoutJson,
                Width = originalTemplate.Width,
                Height = originalTemplate.Height,
                RequiredFields = originalTemplate.RequiredFields,
                Category = originalTemplate.Category,
                IsPublic = request.IsPublic ?? originalTemplate.IsPublic,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Templates.Add(duplicatedTemplate);
            _context.SaveChanges();

            return CreatedAtAction(nameof(GetTemplate), new { id = duplicatedTemplate.TemplateId }, duplicatedTemplate);
        }
    }
}