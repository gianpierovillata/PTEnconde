using EncodeAssistant.EncodeApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EncodeAssistant.EncodeApi.Persistence
{
    /// <summary>
    /// DbContext de EF Core para la bandeja conversacional.
    /// Se conecta a SQL Server (contenedor del docker-compose).
    /// </summary>
    public class EncodeDbContext : DbContext
    {
        public EncodeDbContext(DbContextOptions<EncodeDbContext> options) : base(options) { }

        public DbSet<Contacto> Contactos => Set<Contacto>();
        public DbSet<Mensaje> Mensajes => Set<Mensaje>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Contacto>(entity =>
            {
                entity.ToTable("Contactos");
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Id).HasMaxLength(64).ValueGeneratedNever();
                entity.Property(c => c.Nombre).HasMaxLength(200);
                entity.Property(c => c.Username).HasMaxLength(100);
                entity.Property(c => c.UltimoTexto).HasMaxLength(500);
                entity.Property(c => c.Activo).HasDefaultValue(true);
                entity.Property(c => c.NoLeidos).HasDefaultValue(0);
                entity.Property(c => c.CreadoEn).HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasMany(c => c.Mensajes)
                      .WithOne(m => m.Contacto!)
                      .HasForeignKey(m => m.ContactoId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(c => c.UltimaFecha);
            });

            modelBuilder.Entity<Mensaje>(entity =>
            {
                entity.ToTable("Mensajes");
                entity.HasKey(m => m.Id);
                entity.Property(m => m.Id).ValueGeneratedOnAdd();
                entity.Property(m => m.ContactoId).HasMaxLength(64);
                entity.Property(m => m.Texto).HasMaxLength(4000);
                entity.Property(m => m.Direccion).HasMaxLength(16);
                entity.Property(m => m.Leido).HasDefaultValue(false);
                entity.Property(m => m.ErrorEntrega).HasMaxLength(500);
                entity.Property(m => m.FechaCreacion).HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasIndex(m => new { m.ContactoId, m.FechaCreacion });
            });
        }
    }
}
