using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace InventoryService.Infrastructure.Database
{
    public class AppDbContext : DbContext
    {
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<Seat> Seats => Set<Seat>();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Booking>().HasKey(x => x.Id);
            modelBuilder.Entity<Booking>()
                .Property(b => b.RowVersion)
                .IsRowVersion();
            modelBuilder.Entity<Booking>()
                .HasMany(b => b.Seats)
                .WithOne(s => s.Booking)
                .HasForeignKey(s => s.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Seat>().HasKey(x => new { x.ShowId, x.SeatNumber });
            modelBuilder.Entity<Seat>()
                .Property(b => b.RowVersion)
                .IsRowVersion();
        }
    }
}
