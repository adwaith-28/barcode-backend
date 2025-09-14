using Microsoft.AspNetCore.Mvc;
using LabelDesignerAPI.Data;
using LabelDesignerAPI.Models;
using LabelDesignerAPI.Services;

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
            var template = _context.Templates.Find(request.TemplateId);
            if (template == null)
                return NotFound("Template not found");

            var pdfBytes = LabelGenerator.Generate(request, template.TemplateJson);

            return File(pdfBytes, "application/pdf", "label.pdf");
        }
    }
}
