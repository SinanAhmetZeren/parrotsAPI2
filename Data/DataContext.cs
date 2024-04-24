using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ParrotsAPI2.Data
{
    public class DataContext : IdentityDbContext<AppUser>
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }
        public DbSet<Character> Characters => Set<Character>();
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
                .OnDelete(DeleteBehavior.Cascade); // Cascade on user deletion

            modelBuilder.Entity<AppUser>()
                .HasMany(u => u.Vehicles)
                .WithOne(v => v.User)
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade on user deletion

            modelBuilder.Entity<AppUser>()
                .HasMany(u => u.Voyages)
                .WithOne(v => v.User)
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade on user deletion

            modelBuilder.Entity<AppUser>()
                .HasMany(u => u.Bids)
                .WithOne(b => b.User)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.NoAction); // No cascade on user deletion for bids

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
                .OnDelete(DeleteBehavior.Cascade); // Cascade on vehicle deletion

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
                .OnDelete(DeleteBehavior.Cascade); // Cascade on voyage deletion

            modelBuilder.Entity<Voyage>()
                .HasMany(v => v.Waypoints)
                .WithOne(w => w.Voyage)
                .HasForeignKey(w => w.VoyageId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade on voyage deletion

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



        }
    }
}
