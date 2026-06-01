using DeliveryWeb.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DeliveryWeb.Data
{
    public class AppDbContext : IdentityDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Client> Clients { get; set; }
        public DbSet<Livreur> Livreurs { get; set; }
        public DbSet<Commande> Commandes { get; set; }
        public DbSet<Avis> Avis { get; set; }
        public DbSet<RefusLivraison> RefusLivraisons { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Commande → Client
            builder.Entity<Commande>()
                .HasOne(c => c.Client)
                .WithMany(cl => cl.Commandes)
                .HasForeignKey(c => c.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Commande → Livreur
            builder.Entity<Commande>()
                .HasOne(c => c.Livreur)
                .WithMany(l => l.Commandes)
                .HasForeignKey(c => c.LivreurId)
                .OnDelete(DeleteBehavior.Restrict);

            // Avis → Commande
            builder.Entity<Avis>()
                .HasOne(a => a.Commande)
                .WithOne(c => c.Avis)
                .HasForeignKey<Avis>(a => a.CommandeId)
                .OnDelete(DeleteBehavior.Restrict); // ← corrigé

            // Avis → Client
            builder.Entity<Avis>()
                .HasOne(a => a.Client)
                .WithMany(cl => cl.Avis)
                .HasForeignKey(a => a.ClientId)
                .OnDelete(DeleteBehavior.Restrict); // ← corrigé

            // Avis → Livreur
            builder.Entity<Avis>()
                .HasOne(a => a.Livreur)
                .WithMany(l => l.Avis)
                .HasForeignKey(a => a.LivreurId)
                .OnDelete(DeleteBehavior.Restrict); // ← corrigé

            // Prix en FCFA
            builder.Entity<Commande>()
                .Property(c => c.PrixCalcule)
                .HasColumnType("decimal(18,2)");
        }
    }
}