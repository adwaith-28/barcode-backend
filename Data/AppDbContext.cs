using Microsoft.EntityFrameworkCore;
using LabelDesignerAPI.Models;

namespace LabelDesignerAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Template> Templates { get; set; }
    }
}
