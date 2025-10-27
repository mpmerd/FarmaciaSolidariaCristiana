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
        public DbSet<Supply> Supplies { get; set; }
        public DbSet<Delivery> Deliveries { get; set; }
        public DbSet<Donation> Donations { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<PatientDocument> PatientDocuments { get; set; }
        public DbSet<Sponsor> Sponsors { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<Delivery>()
                .HasOne(d => d.Medicine)
                .WithMany(m => m.Deliveries)
                .HasForeignKey(d => d.MedicineId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Delivery>()
                .HasOne(d => d.Patient)
                .WithMany(p => p.Deliveries)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Donation>()
                .HasOne(d => d.Medicine)
                .WithMany(m => m.Donations)
                .HasForeignKey(d => d.MedicineId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PatientDocument>()
                .HasOne(pd => pd.Patient)
                .WithMany(p => p.Documents)
                .HasForeignKey(pd => pd.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure decimal precision for Patient vitals
            modelBuilder.Entity<Patient>()
                .Property(p => p.Weight)
                .HasPrecision(5, 2); // Max 999.99 kg

            modelBuilder.Entity<Patient>()
                .Property(p => p.Height)
                .HasPrecision(5, 2); // Max 999.99 cm
        }
    }
}
