using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using FarmaciaSolidariaCristiana.Models;

namespace FarmaciaSolidariaCristiana.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Medicine> Medicines { get; set; }
        public DbSet<Delivery> Deliveries { get; set; }
        public DbSet<Donation> Donations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<Delivery>()
                .HasOne(d => d.Medicine)
                .WithMany(m => m.Deliveries)
                .HasForeignKey(d => d.MedicineId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Donation>()
                .HasOne(d => d.Medicine)
                .WithMany(m => m.Donations)
                .HasForeignKey(d => d.MedicineId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
