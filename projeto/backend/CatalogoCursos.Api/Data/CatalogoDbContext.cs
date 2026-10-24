using CatalogoCursos.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CatalogoCursos.Api.Data;

public class CatalogoDbContext(DbContextOptions<CatalogoDbContext> options) : DbContext(options)
{
    public DbSet<Curso> Cursos => Set<Curso>();
    public DbSet<Modulo> Modulos => Set<Modulo>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Modalidade> Modalidades => Set<Modalidade>();
    public DbSet<StatusCurso> Status => Set<StatusCurso>();
    public DbSet<Depoimento> Depoimentos => Set<Depoimento>();
    public DbSet<Indicador> Indicadores => Set<Indicador>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Categoria>(e =>
        {
            e.ToTable("categorias");
            e.Property(p => p.Nome).HasMaxLength(120).IsRequired();
            e.Property(p => p.Slug).HasMaxLength(80).IsRequired();
            e.Property(p => p.Icone).HasMaxLength(80);
            e.HasIndex(p => p.Slug).IsUnique();
            e.HasIndex(p => p.MoodleId).IsUnique();
        });

        mb.Entity<Modalidade>(e =>
        {
            e.ToTable("modalidades");
            e.Property(p => p.Nome).HasMaxLength(80).IsRequired();
            e.Property(p => p.Slug).HasMaxLength(40).IsRequired();
            e.HasIndex(p => p.Slug).IsUnique();
        });

        mb.Entity<StatusCurso>(e =>
        {
            e.ToTable("status");
            e.Property(p => p.Nome).HasMaxLength(80).IsRequired();
            e.Property(p => p.Slug).HasMaxLength(40).IsRequired();
            e.Property(p => p.Cor).HasMaxLength(9);
            e.HasIndex(p => p.Slug).IsUnique();
        });

        mb.Entity<Curso>(e =>
        {
            e.ToTable("cursos");
            e.Property(p => p.NomeCompleto).HasMaxLength(200).IsRequired();
            e.Property(p => p.NomeBreve).HasMaxLength(60).IsRequired();
            e.Property(p => p.Descricao).HasColumnType("text").IsRequired();
            e.Property(p => p.PublicoAlvo).HasMaxLength(300).IsRequired();
            e.Property(p => p.ImagemUrl).HasMaxLength(500).IsRequired();
            e.Property(p => p.MoodleUrl).HasMaxLength(500);
            e.HasIndex(p => p.NomeBreve).IsUnique();
            e.HasIndex(p => p.MoodleId).IsUnique();
            e.HasIndex(p => new { p.CategoriaId, p.ModalidadeId, p.StatusId });
            e.HasOne(p => p.Categoria).WithMany(c => c.Cursos).HasForeignKey(p => p.CategoriaId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.Modalidade).WithMany(m => m.Cursos).HasForeignKey(p => p.ModalidadeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.Status).WithMany(s => s.Cursos).HasForeignKey(p => p.StatusId).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<Depoimento>(e =>
        {
            e.ToTable("depoimentos");
            e.Property(p => p.Nome).HasMaxLength(120).IsRequired();
            e.Property(p => p.Cargo).HasMaxLength(160).IsRequired();
            e.Property(p => p.Texto).HasMaxLength(1000).IsRequired();
            e.HasOne(p => p.Curso).WithMany().HasForeignKey(p => p.CursoId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(p => new { p.CursoId, p.Ordem });
        });

        mb.Entity<Indicador>(e =>
        {
            e.ToTable("indicadores");
            e.Property(p => p.Valor).HasMaxLength(40).IsRequired();
            e.Property(p => p.Rotulo).HasMaxLength(120).IsRequired();
        });

        mb.Entity<Modulo>(e =>
        {
            e.ToTable("modulos");
            e.Property(p => p.Titulo).HasMaxLength(200).IsRequired();
            e.Property(p => p.Descricao).HasColumnType("text").IsRequired();
            e.HasIndex(p => new { p.CursoId, p.Ordem }).IsUnique();
            e.HasOne(p => p.Curso).WithMany(c => c.Modulos).HasForeignKey(p => p.CursoId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
