using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ParrotsAPI2.Data
{
    public class DataContext : IdentityDbContext<AppUser>
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }
        public DbSet<Bid> Bids { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<VehicleImage> VehicleImages { get; set; }
        public DbSet<Voyage> Voyages { get; set; }
        public DbSet<VoyageImage> VoyageImages { get; set; }
        public DbSet<Waypoint> Waypoints { get; set; }
        public DbSet<Favorite> Favorites { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AppUser>()
                .HasKey(u => u.Id);

            modelBuilder.Entity<Vehicle>()
                .HasOne(v => v.User)
                .WithMany(u => u.Vehicles)
                .HasForeignKey(v => v.UserId)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AppUser>()
                .HasMany(u => u.Vehicles)
                .WithOne(v => v.User)
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AppUser>()
                .HasMany(u => u.Voyages)
                .WithOne(v => v.User)
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AppUser>()
                .HasMany(u => u.Bids)
                .WithOne(b => b.User)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Message>()
                .HasOne<AppUser>()
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne<AppUser>()
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Vehicle>()
                .HasMany(v => v.VehicleImages)
                .WithOne(vi => vi.Vehicle)
                .HasForeignKey(vi => vi.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Vehicle>()
                .HasMany(v => v.Voyages)
                .WithOne(v => v.Vehicle)
                .HasForeignKey(v => v.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Voyage>()
                .HasOne(v => v.Vehicle)
                .WithMany(v => v.Voyages)
                .HasForeignKey(v => v.VehicleId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Voyage>()
                .HasMany(v => v.VoyageImages)
                .WithOne(vi => vi.Voyage)
                .HasForeignKey(vi => vi.VoyageId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Voyage>()
                .HasMany(v => v.Waypoints)
                .WithOne(w => w.Voyage)
                .HasForeignKey(w => w.VoyageId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AppUser>()
                .HasMany(u => u.Vehicles)
                .WithOne(v => v.User)
                .HasForeignKey(v => v.UserId)
                .IsRequired();

            modelBuilder.Entity<AppUser>()
                .HasMany(u => u.Voyages)
                .WithOne(v => v.User)
                .HasForeignKey(v => v.UserId)
                .IsRequired();

            modelBuilder.Entity<AppUser>()
                .HasMany(u => u.Bids)
                .WithOne(b => b.User)
                .HasForeignKey(b => b.UserId)
                .IsRequired();

            modelBuilder.Entity<Message>()
                .HasOne<AppUser>()
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne<AppUser>()
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // ✅ FIX decimal precision warnings
            modelBuilder.Entity<Bid>()
                .Property(b => b.OfferPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Voyage>()
                .Property(v => v.MinPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Voyage>()
                .Property(v => v.MaxPrice)
                .HasPrecision(18, 2);


            // Seed default vehicles ("walk" and "run") with UserId = "1"
            /*
            modelBuilder.Entity<Vehicle>().HasData(
                new Vehicle
                {
                    Id = 1,
                    Name = "walk",
                    ProfileImageUrl = "0",
                    Type = VehicleType.Walk, // replace with appropriate enum value
                    Capacity = 10000000,
                    Description = "Walk",
                    UserId = "1",
                    CreatedAt = new DateTime(1980, 6, 3, 0, 0, 0, DateTimeKind.Utc),
                    Confirmed = true,
                    IsDeleted = false
                },
                new Vehicle
                {
                    Id = 2,
                    Name = "run",
                    ProfileImageUrl = "0",
                    Type = VehicleType.Run, // replace with appropriate enum value
                    Capacity = 10000000,
                    Description = "Run",
                    UserId = "1",
                    CreatedAt = new DateTime(1980, 6, 3, 0, 0, 0, DateTimeKind.Utc),
                    Confirmed = true,
                    IsDeleted = false
                }
            );
            */

        }
    }
}
