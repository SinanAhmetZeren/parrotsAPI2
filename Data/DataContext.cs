using Microsoft.EntityFrameworkCore;

namespace ParrotsAPI2.Data
{
    public class DataContext  : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }
        
        public DbSet<Character> Characters => Set<Character>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Bid> Bids { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<VehicleImage> VehicleImages { get; set; }
        public DbSet<Voyage> Voyages { get; set; }
        public DbSet<VoyageImage> VoyageImages { get; set; }
        public DbSet<Waypoint> Waypoints { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasMany(u => u.Vehicles)
                .WithOne(v => v.User)
                .HasForeignKey(v => v.UserId);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Voyages)
                .WithOne(v => v.User)
                .HasForeignKey(v => v.UserId);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Bids)
                .WithOne(b => b.User)
                .HasForeignKey(b => b.UserId);

            modelBuilder.Entity<User>()
                .HasMany(u => u.SentMessages)
                .WithOne(m => m.Sender)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany(u => u.ReceivedMessages)
                .WithOne(m => m.Receiver)
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Vehicle>()
                .HasMany(v => v.VehicleImages)
                .WithOne(vi => vi.Vehicle)
                .HasForeignKey(vi => vi.VehicleId);

            modelBuilder.Entity<Vehicle>()
                .HasMany(v => v.Voyages)
                .WithOne(v => v.Vehicle)
                .HasForeignKey(v => v.VehicleId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete when Vehicle is deleted

            modelBuilder.Entity<Voyage>()
                .HasMany(v => v.VoyageImages)
                .WithOne(vi => vi.Voyage)
                .HasForeignKey(vi => vi.VoyageId);

            modelBuilder.Entity<Voyage>()
                .HasOne(v => v.Vehicle)
                .WithMany(v => v.Voyages)
                .HasForeignKey(v => v.VehicleId)
                .OnDelete(DeleteBehavior.NoAction); 

            modelBuilder.Entity<Voyage>()
                .HasMany(v => v.Bids)
                .WithOne(b => b.Voyage)
                .HasForeignKey(b => b.VoyageId);

            modelBuilder.Entity<Waypoint>()
                .HasOne(w => w.Voyage)
                .WithMany(v => v.Waypoints)
                .HasForeignKey(w => w.VoyageId);

            modelBuilder.Entity<Bid>()
                .HasOne(b => b.Voyage)
                .WithMany(v => v.Bids)
                .HasForeignKey(b => b.VoyageId)
                .OnDelete(DeleteBehavior.NoAction);


        }
    }
}
