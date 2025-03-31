using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Reflection.Emit;

namespace HotelManagementApp.Models
{
    public class AppDbContext : IdentityDbContext<
            ApplicationUser,     // User
            ApplicationRole,     // Role
            int,                 // User and Role Key Type
            IdentityUserClaim<int>,
            IdentityUserRole<int>,
            IdentityUserLogin<int>,
            IdentityRoleClaim<int>,
            IdentityUserToken<int>>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // DbSets for custom entities
        //public DbSet<Hotel> Hotels { get; set; }
        //public DbSet<ApplicationRole> Roles { get; set; }
        public DbSet<RoomType> RoomTypes { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<ServiceOrder> ServiceOrders { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<MaintenanceRequest> MaintenanceRequests { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<CleaningTask> CleaningTasks { get; set; }
        public DbSet<Feedback> Feedback { get; set; }
        public DbSet<Invoice> Invoices { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Comprehensive relationship configurations
            //// User-Role Relationship
            //builder.Entity<ApplicationUser>()
            //    .HasOne(u => u.Role)
            //    .WithMany(r => r.Users)
            //    .HasForeignKey(u => u.RoleId)
            //    .IsRequired(false);

            // Reservation Relationships
            //Reservation User
            builder.Entity<Reservation>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reservations)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            //Reservation- Room
            builder.Entity<Reservation>()
                .HasOne(r => r.Room)
                .WithMany(rm => rm.Reservations)
                .HasForeignKey(r => r.RoomId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
            //Reservation-invoice
            builder.Entity<Reservation>()
                .HasMany(r => r.Invoices)
                .WithOne(so => so.Reservation)
                .HasForeignKey(so => so.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Reservation User Relationships
            builder.Entity<Reservation>()
                .HasOne(r => r.CreatedByUser)
                .WithMany()
                .HasForeignKey(r => r.CreatedBy)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Reservation>()
                .HasOne(r => r.CheckedInByUser)
                .WithMany(u => u.CheckedInReservations)
                .HasForeignKey(r => r.CheckedInBy)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Reservation>()
                .HasOne(r => r.CheckedOutByUser)
                .WithMany(u => u.CheckedOutReservations)
                .HasForeignKey(r => r.CheckedOutBy)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
            //Reservation-Feedback
            builder.Entity<Reservation>()
                .HasMany(r => r.Feedback)
                .WithOne(f => f.Reservation)
                .HasForeignKey(f => f.ReservationId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);


            // Room-RoomType Relationship
            builder.Entity<Room>()
                .HasOne(r => r.RoomType)
                .WithMany(rt => rt.Rooms)
                .HasForeignKey(r => r.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);
            //Room-User Relationship
            builder.Entity<Room>()
                .HasOne(r => r.LastCleanedBy)
                .WithMany()
                .HasForeignKey(r => r.CleanedById)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            //Room-CleaningTask
            builder.Entity<CleaningTask>()
                .HasOne(ct => ct.Room)
                .WithMany(r => r.CleaningTasks)
                .HasForeignKey(ct => ct.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
            //CleaningTask-User
            builder.Entity<CleaningTask>()
                .HasOne(ct => ct.AssignedTo)
                .WithMany(a => a.AssignedCleaningTasks)
                .HasForeignKey(ct => ct.AssignedToId)
                .OnDelete(DeleteBehavior.Restrict);
            //Feedback-User(Resolver)
            builder.Entity<Feedback>()
                .HasOne(f => f.ResolvedBy)
                .WithMany(a => a.ResolvedFeedback)
                .HasForeignKey(f => f.ResolvedById)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
            //Feedback-User
            builder.Entity<Feedback>()
                .HasOne(f => f.User)
                .WithMany(u => u.Feedback)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ServiceOrder Relationships
            builder.Entity<ServiceOrder>()
                .HasOne(so => so.Reservation)
                .WithMany(r => r.ServiceOrders)
                .HasForeignKey(so => so.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ServiceOrder>()
                .HasOne(so => so.Service)
                .WithMany(s => s.ServiceOrders)
                .HasForeignKey(so => so.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ApplicationUser>()
                .HasMany(u => u.ServiceOrdersCompleted)
                .WithOne(so => so.CompletedBy)
                .HasForeignKey(so => so.CompletedById)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Payment Relationships
            builder.Entity<Payment>()
                .HasOne(p => p.Invoice)
                .WithMany(r => r.Payments)
                .HasForeignKey(p => p.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            //builder.Entity<Payment>()
            //    .HasOne(p => p.ProcessedByUser)
            //    .WithMany(/*u => u.Payments*/)
            //    .HasForeignKey(p => p.ProcessedBy)
            //    .IsRequired(false);

            // MaintenanceRequest Relationships
            builder.Entity<MaintenanceRequest>()
                .HasOne(mr => mr.ReportedByUser)
                .WithMany(u => u.MaintenanceRequestsReported)
                .HasForeignKey(mr => mr.ReportedBy)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<MaintenanceRequest>()
                .HasOne(mr => mr.AssignedToUser)
                .WithMany(u => u.MaintenanceRequestsAssigned)
                .HasForeignKey(mr => mr.AssignedTo)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<MaintenanceRequest>()
                .HasOne(mr => mr.Room)
                .WithMany()
                .HasForeignKey(mr => mr.RoomId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Report Relationships
            builder.Entity<Report>()
                .HasOne(r => r.CreatedByUser)
                .WithMany(u => u.Reports)
                .HasForeignKey(r => r.CreatedBy)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            SeedData(builder);
        }
        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed Roles
            modelBuilder.Entity<ApplicationRole>().HasData(
                new ApplicationRole { Id = 1, Name = "Manager", Description = "Hotel management and administrative access" },
                new ApplicationRole { Id = 2, Name = "Receptionist", Description = "Front desk operations and guest services" },
                new ApplicationRole { Id = 3, Name = "Housekeeper", Description = "Room cleaning and maintenance" },
                new ApplicationRole { Id = 4, Name = "Guest", Description = "Hotel guest with booking capabilities" }
            );

            // Seed RoomTypes
            modelBuilder.Entity<RoomType>().HasData(
                new RoomType
                {
                    Id = 1,
                    Name = "Deluxe Room",
                    Description = "Comfortable room with a queen-sized bed, suitable for up to 2 people",
                    Capacity = 2,
                    BasePrice = 1.0m,
                    Amenities = "WiFi,TV,Mini-bar,Air conditioning",
                    ImageUrl = "img/room-deluxe.jpg"
                },
                new RoomType
                {
                    Id = 2,
                    Name = "Executive Suite",
                    Description = "Spacious suite with a king-sized bed and separate living area",
                    Capacity = 2,
                    BasePrice = 1.5m,
                    Amenities = "WiFi,4K TV,Mini-bar,Air conditioning,Work desk,Bathtub,Lounge area",
                    ImageUrl = "img/room-executive.jpg"
                },
                new RoomType
                {
                    Id = 3,
                    Name = "Family Room",
                    Description = "Large room with one king-sized and two single beds, perfect for families",
                    Capacity = 4,
                    BasePrice = 1.8m,
                    Amenities = "WiFi,TV,Mini-bar,Air conditioning,Extra beds,Family entertainment",
                    ImageUrl = "img/room-family.jpg"
                },
                new RoomType
                {
                    Id = 4,
                    Name = "Presidential Suite",
                    Description = "Our most luxurious accommodation with panoramic views and butler service",
                    Capacity = 4,
                    BasePrice = 3.0m,
                    Amenities = "WiFi,Smart TV,Full bar,Climate control,Work office,Jacuzzi,Dining area,Private butler,VIP services",
                    ImageUrl = "img/room-presidential.jpg"
                }
            );

            // Seed Sample Feedback
            modelBuilder.Entity<Feedback>().HasData(
                new Feedback
                {
                    Id = 1,
                    GuestName = "John Smith",
                    GuestEmail = "john.smith@example.com",
                    Rating = 5,
                    Subject = "Excellent Stay",
                    Comments = "Everything was perfect during our stay. The staff was very friendly and helpful.",
                    Category = "General",
                    IsResolved = true,
                    ResolutionNotes = "Thanked guest for positive feedback",
                    CreatedAt = DateTime.Parse("2023-04-15"),
                    ResolvedAt = DateTime.Parse("2023-04-16")
                },
                new Feedback
                {
                    Id = 2,
                    GuestName = "Jane Doe",
                    GuestEmail = "jane.doe@example.com",
                    Rating = 3,
                    Subject = "Room Cleanliness Issue",
                    Comments = "The room was not properly cleaned when we checked in. There were some issues with the bathroom.",
                    Category = "Cleanliness",
                    IsResolved = true,
                    ResolutionNotes = "Apologized to guest and offered complimentary breakfast. Room was immediately cleaned.",
                    CreatedAt = DateTime.Parse("2023-04-18"),
                    ResolvedAt = DateTime.Parse("2023-04-18")
                },
                new Feedback
                {
                    Id = 3,
                    GuestName = "Robert Johnson",
                    GuestEmail = "robert.johnson@example.com",
                    Rating = 4,
                    Subject = "Breakfast Variety",
                    Comments = "Great stay overall, but would appreciate more variety in the breakfast menu.",
                    Category = "Food",
                    IsResolved = false,
                    CreatedAt = DateTime.Parse("2023-04-20")
                }
            );
        }
    }
}
