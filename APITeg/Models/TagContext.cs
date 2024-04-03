using Microsoft.EntityFrameworkCore;
using APITag.Models;

public class TagContext : DbContext
{
    public TagContext(DbContextOptions<TagContext> options)
        : base(options)
    {
    }

    public TagContext()
    {
    }

    public virtual DbSet<Tag> Tags { get; set; } = null!;
}
