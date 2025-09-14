using Microsoft.AspNetCore.Mvc;
using LabelDesignerAPI.Data;
using LabelDesignerAPI.Models;

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
    public IActionResult GetTemplates() => Ok(_context.Templates.ToList());

    [HttpGet("{id}")]
    public IActionResult GetTemplate(int id)
    {
        var template = _context.Templates.Find(id);
        if (template == null) return NotFound();
        return Ok(template);
    }

    [HttpPost]
    public IActionResult CreateTemplate([FromBody] Template template)
    {
        _context.Templates.Add(template);
        _context.SaveChanges();
        return CreatedAtAction(nameof(GetTemplate), new { id = template.TemplateId }, template);
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteTemplate(int id)
    {
        var template = _context.Templates.Find(id);
        if (template == null) return NotFound();
        _context.Templates.Remove(template);
        _context.SaveChanges();
        return NoContent();
    }
}
