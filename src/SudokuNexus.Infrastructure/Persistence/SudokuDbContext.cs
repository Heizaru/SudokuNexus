using Microsoft.EntityFrameworkCore;

namespace SudokuNexus.Infrastructure.Persistence;

public sealed class SudokuDbContext(DbContextOptions<SudokuDbContext> options) : DbContext(options)
{
    public DbSet<PuzzleEntity> Puzzles => Set<PuzzleEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PuzzleEntity>(builder =>
        {
            builder.ToTable("Puzzles");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.PuzzleString).HasMaxLength(81).IsRequired();
            builder.Property(p => p.SolutionString).HasMaxLength(81).IsRequired();
            builder.Property(p => p.Difficulty).HasConversion<string>().HasMaxLength(32);
            builder.Property(p => p.MaxTechnique).HasConversion<string>().HasMaxLength(64);
            builder.HasIndex(p => p.Difficulty);
        });
    }
}
