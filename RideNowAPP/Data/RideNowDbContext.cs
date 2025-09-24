using Microsoft.EntityFrameworkCore;
using RideNowAPI.Models;

namespace RideNowAPI.Data
{
    public class RideNowDbContext : DbContext
    {
        public RideNowDbContext(DbContextOptions<RideNowDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<Ride> Rides { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        public DbSet<DriverEarnings> DriverEarnings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User entity configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Phone).IsUnique();
            });

            // Driver entity configuration
            modelBuilder.Entity<Driver>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Phone).IsUnique();
                entity.HasIndex(e => e.LicenseNumber).IsUnique();
            });

            // Ride relationships
            modelBuilder.Entity<Ride>(entity =>
            {
                entity.HasOne(r => r.User)
                    .WithMany(u => u.Rides)
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Driver)
                    .WithMany(d => d.Rides)
                    .HasForeignKey(r => r.DriverId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Payment relationships
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasOne(p => p.Ride)
                    .WithOne(r => r.Payment)
                    .HasForeignKey<Payment>(p => p.RideId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Rating relationships
            modelBuilder.Entity<Rating>(entity =>
            {
                entity.HasOne(r => r.Ride)
                    .WithOne(ride => ride.Rating)
                    .HasForeignKey<Rating>(r => r.RideId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.User)
                    .WithMany(u => u.Ratings)
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Driver)
                    .WithMany(d => d.DriverRatings)
                    .HasForeignKey(r => r.DriverId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // DriverEarnings relationships
            modelBuilder.Entity<DriverEarnings>(entity =>
            {
                entity.HasOne(de => de.Driver)
                    .WithMany(d => d.Earnings)
                    .HasForeignKey(de => de.DriverId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(de => de.Ride)
                    .WithMany()
                    .HasForeignKey(de => de.RideId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
