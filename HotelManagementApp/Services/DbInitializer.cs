using HotelManagementApp.Models;
using HotelManagementApp.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HotelManagementApp.Services
{
    public static class DbInitializer
    {
        public static async Task Initialize(IServiceProvider serviceProvider, bool isDevelopment)
        {
            // If not in development, don't seed test data
            if (!isDevelopment)
                return;

            using var scope = serviceProvider.CreateScope();
            var scopedServices = scope.ServiceProvider;

            try
            {
                var context = scopedServices.GetRequiredService<AppDbContext>();
                var userManager = scopedServices.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = scopedServices.GetRequiredService<RoleManager<ApplicationRole>>();
                var logger = scopedServices.GetRequiredService<ILogger<Program>>();

                logger.LogInformation("Starting database initialization...");

                // Make sure the database is created
                await context.Database.EnsureCreatedAsync();

                // Only seed if database is empty
                if (await context.Users.AnyAsync())
                {
                    logger.LogInformation("Database already contains users. Skipping seeding.");
                    return;   // DB has been seeded
                }

                await SeedRoles(roleManager, logger);
                var users = await SeedUsers(userManager, logger);
                var rooms = await SeedRooms(context, logger);
                var serviceItems = await SeedServices(context, logger);
                var reservations = await SeedReservations(context, users, rooms, logger);
                await SeedCleaningTasks(context, users, rooms, logger);
                await SeedMaintenanceRequests(context, users, rooms, logger);
                await SeedFeedback(context, users, reservations, logger);
                await SeedInvoicesAndPayments(context, users, reservations, logger);
                await SeedServiceOrders(context, reservations, serviceItems, users, logger);
                await SeedReports(context, users, logger);

                logger.LogInformation("Database initialization completed successfully.");
            }
            catch (Exception ex)
            {
                var logger = scopedServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred while seeding the database.");
            }
        }

        private static async Task SeedRoles(RoleManager<ApplicationRole> roleManager, ILogger logger)
        {
            logger.LogInformation("Seeding roles...");

            // Define roles exactly as used in the [Authorize(Roles = "...")] attributes
            string[] roleNames = { "Manager", "Receptionist", "Housekeeper", "Guest" };
            string[] descriptions = {
                "Hotel management and administrative access",
                "Front desk operations and guest services",
                "Room cleaning and maintenance",
                "Hotel guest with booking capabilities"
            };

            for (int i = 0; i < roleNames.Length; i++)
            {
                var roleName = roleNames[i];
                var roleDescription = descriptions[i];

                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var roleResult = await roleManager.CreateAsync(new ApplicationRole
                    {
                        Name = roleName,
                        NormalizedName = roleName.ToUpper(),
                        Description = roleDescription
                    });

                    if (roleResult.Succeeded)
                    {
                        logger.LogInformation("Created role: {RoleName}", roleName);
                    }
                    else
                    {
                        var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                        logger.LogError("Failed to create role {RoleName}: {Errors}", roleName, errors);
                    }
                }
            }
        }

        private static async Task<List<ApplicationUser>> SeedUsers(UserManager<ApplicationUser> userManager, ILogger logger)
        {
            logger.LogInformation("Seeding users...");

            // Create test users for each role
            var users = new List<ApplicationUser>();

            // Manager users
            var manager1 = new ApplicationUser
            {
                UserName = "manager@hotel.com", // Use email as username for consistency
                Email = "manager@hotel.com",
                FirstName = "John",
                LastName = "Manager",
                EmailConfirmed = true,
                PhoneNumber = "1234567890",
                IsActive = true,
                AccountStatus = AccountStatus.Active,
                Created = DateTime.UtcNow,
                RegistrationDate = DateTime.UtcNow,
                Address = "123 Main St",
                City = "New York",
                State = "NY",
                Country = "USA",
                PostalCode = "10001",
                HireDate = DateTime.UtcNow.AddYears(-2)
            };
            await CreateUserWithRole(userManager, manager1, "Password123!", "Manager", logger);
            users.Add(manager1);

            // Receptionist users
            var receptionist1 = new ApplicationUser
            {
                UserName = "receptionist@hotel.com",
                Email = "receptionist@hotel.com",
                FirstName = "Alice",
                LastName = "Reception",
                EmailConfirmed = true,
                PhoneNumber = "1234567891",
                IsActive = true,
                AccountStatus = AccountStatus.Active,
                Created = DateTime.UtcNow,
                RegistrationDate = DateTime.UtcNow,
                Address = "456 Main St",
                City = "New York",
                State = "NY",
                Country = "USA",
                PostalCode = "10001",
                HireDate = DateTime.UtcNow.AddYears(-1)
            };
            await CreateUserWithRole(userManager, receptionist1, "Password123!", "Receptionist", logger);
            users.Add(receptionist1);

            // Housekeeper users
            var housekeeper1 = new ApplicationUser
            {
                UserName = "housekeeper@hotel.com",
                Email = "housekeeper@hotel.com",
                FirstName = "Bob",
                LastName = "Cleaning",
                EmailConfirmed = true,
                PhoneNumber = "1234567892",
                IsActive = true,
                AccountStatus = AccountStatus.Active,
                Created = DateTime.UtcNow,
                RegistrationDate = DateTime.UtcNow,
                Address = "789 Main St",
                City = "New York",
                State = "NY",
                Country = "USA",
                PostalCode = "10001",
                HireDate = DateTime.UtcNow.AddMonths(-6)
            };
            await CreateUserWithRole(userManager, housekeeper1, "Password123!", "Housekeeper", logger);
            users.Add(housekeeper1);

            // Guest users
            var guest1 = new ApplicationUser
            {
                UserName = "guest@example.com",
                Email = "guest@example.com",
                FirstName = "Sarah",
                LastName = "Guest",
                EmailConfirmed = true,
                PhoneNumber = "1234567893",
                IsActive = true,
                AccountStatus = AccountStatus.Active,
                Created = DateTime.UtcNow,
                RegistrationDate = DateTime.UtcNow,
                Address = "101 Guest Ave",
                City = "Boston",
                State = "MA",
                Country = "USA",
                PostalCode = "02101",
                IdType = "Passport",
                IdNumber = "AB123456"
            };
            await CreateUserWithRole(userManager, guest1, "Password123!", "Guest", logger);
            users.Add(guest1);

            var guest2 = new ApplicationUser
            {
                UserName = "guest2@example.com",
                Email = "guest2@example.com",
                FirstName = "Mark",
                LastName = "Traveler",
                EmailConfirmed = true,
                PhoneNumber = "9876543210",
                IsActive = true,
                AccountStatus = AccountStatus.Active,
                Created = DateTime.UtcNow,
                RegistrationDate = DateTime.UtcNow,
                Address = "202 Visitor St",
                City = "Chicago",
                State = "IL",
                Country = "USA",
                PostalCode = "60601",
                IdType = "Driver License",
                IdNumber = "DL987654"
            };
            await CreateUserWithRole(userManager, guest2, "Password123!", "Guest", logger);
            users.Add(guest2);

            return users;
        }

        private static async Task<List<Room>> SeedRooms(AppDbContext context, ILogger logger)
        {
            logger.LogInformation("Seeding rooms...");

            // Make sure we have the RoomTypes created in the AppDbContext.SeedData method
            var roomTypes = await context.RoomTypes.ToListAsync();
            if (roomTypes.Count == 0)
            {
                logger.LogWarning("No room types found. Using those from AppDbContext.SeedData method");
                await context.SaveChangesAsync(); // Make sure room types from SeedData are saved
                roomTypes = await context.RoomTypes.ToListAsync(); // Try to retrieve them again

                if (roomTypes.Count == 0)
                {
                    logger.LogError("Failed to retrieve room types after calling SaveChangesAsync");
                    return new List<Room>();
                }
            }

            if (!await context.Rooms.AnyAsync())
            {
                var rooms = new List<Room>();

                // Create rooms for each floor and room type
                for (int floor = 1; floor <= 4; floor++)
                {
                    foreach (var roomType in roomTypes)
                    {
                        // Create 5 rooms of each type per floor
                        for (int i = 1; i <= 5; i++)
                        {
                            // Room number format: Floor (1 digit) + Room type (1 digit) + Sequential (2 digits)
                            string roomNumber = $"{floor}{roomType.Id % 10}{i:D2}";

                            var room = new Room
                            {
                                RoomNumber = roomNumber,
                                Floor = floor,
                                RoomTypeId = roomType.Id,
                                Status = RoomStatus.Available,
                                BasePrice = roomType.BasePrice * 100, // Convert to actual price in currency units
                                NeedsCleaning = false,
                                LastCleaned = DateTime.UtcNow.AddDays(-1),
                                Notes = $"Standard {roomType.Name}"
                            };

                            rooms.Add(room);
                        }
                    }
                }

                await context.Rooms.AddRangeAsync(rooms);
                await context.SaveChangesAsync();
                logger.LogInformation("Added {Count} rooms", rooms.Count);
                return rooms;
            }

            return await context.Rooms.ToListAsync();
        }

        private static async Task<List<Service>> SeedServices(AppDbContext context, ILogger logger)
        {
            logger.LogInformation("Seeding services...");

            if (!await context.Services.AnyAsync())
            {
                var services = new List<Service>
                {
                    new Service
                    {
                        ServiceName = "Room Cleaning",
                        Description = "Standard room cleaning service",
                        ServiceType = "Housekeeping",
                        Price = 25.00m,
                        IsActive = true
                    },
                    new Service
                    {
                        ServiceName = "Breakfast",
                        Description = "Continental breakfast served in room",
                        ServiceType = "Dining",
                        Price = 15.00m,
                        IsActive = true
                    },
                    new Service
                    {
                        ServiceName = "Laundry",
                        Description = "Clothing laundry and ironing",
                        ServiceType = "Housekeeping",
                        Price = 35.00m,
                        IsActive = true
                    },
                    new Service
                    {
                        ServiceName = "Massage",
                        Description = "In-room massage service",
                        ServiceType = "Wellness",
                        Price = 80.00m,
                        IsActive = true
                    },
                    new Service
                    {
                        ServiceName = "Airport Shuttle",
                        Description = "Transportation to/from airport",
                        ServiceType = "Transportation",
                        Price = 50.00m,
                        IsActive = true
                    }
                };

                await context.Services.AddRangeAsync(services);
                await context.SaveChangesAsync();
                logger.LogInformation("Added {Count} services", services.Count);
                return services;
            }

            return await context.Services.ToListAsync();
        }

        private static async Task<List<Reservation>> SeedReservations(AppDbContext context, List<ApplicationUser> users, List<Room> rooms, ILogger logger)
        {
            logger.LogInformation("Seeding reservations...");

            if (!await context.Reservations.AnyAsync())
            {
                var reservations = new List<Reservation>();
                var random = new Random();

                // Get guest users
                var guests = users.Where(u => u.UserName.Contains("guest")).ToList();
                if (guests.Count == 0)
                {
                    logger.LogError("No guest users found for creating reservations");
                    return reservations;
                }

                var staff = users.Where(u => !u.UserName.Contains("guest")).ToList();
                if (staff.Count == 0)
                {
                    logger.LogError("No staff users found for creating reservations");
                    return reservations;
                }

                // Setup room types
                var roomTypes = await context.RoomTypes.ToListAsync();
                if (roomTypes.Count == 0)
                {
                    logger.LogError("No room types found for creating reservations");
                    return reservations;
                }

                // Create 5 active reservations
                for (int i = 0; i < 5; i++)
                {
                    var checkInDate = DateTime.UtcNow.AddDays(random.Next(1, 30));
                    var checkOutDate = checkInDate.AddDays(random.Next(1, 7));
                    var guest = guests[random.Next(guests.Count)];
                    var roomType = roomTypes[random.Next(roomTypes.Count)];
                    var manager = staff.FirstOrDefault(u => u.UserName.Contains("manager"));
                    var receptionist = staff.FirstOrDefault(u => u.UserName.Contains("receptionist"));

                    if (manager == null || receptionist == null)
                    {
                        logger.LogWarning("Manager or receptionist not found for reservation {Index}", i);
                        continue;
                    }

                    // Use available rooms of the selected room type
                    var availableRooms = rooms.Where(r => r.RoomTypeId == roomType.Id && r.Status == RoomStatus.Available).ToList();
                    if (availableRooms.Count == 0)
                    {
                        logger.LogWarning("No available rooms found for room type {RoomTypeId}", roomType.Id);
                        continue;
                    }

                    var room = availableRooms[random.Next(availableRooms.Count)];

                    var reservation = new Reservation
                    {
                        ReservationNumber = $"RES{DateTime.UtcNow.Year}{i + 1:D4}",
                        UserId = guest.Id,
                        ReservationDate = DateTime.UtcNow,
                        CheckInDate = checkInDate,
                        CheckOutDate = checkOutDate,
                        Status = ReservationStatus.Confirmed,
                        TotalPrice = roomType.BasePrice * 100 * (decimal)(checkOutDate - checkInDate).TotalDays,
                        PaymentMethod = PaymentMethod.CreditCard,
                        PaymentStatus = PaymentStatus.Pending,
                        NumberOfGuests = random.Next(1, roomType.Capacity + 1),
                        SpecialRequests = "No special requests",
                        RoomTypeId = roomType.Id,
                        RoomId = room.Id,
                        CreatedBy = manager.Id
                    };

                    reservations.Add(reservation);
                }

                // Create 3 checked-in reservations
                for (int i = 0; i < 3; i++)
                {
                    var checkInDate = DateTime.UtcNow.AddDays(-random.Next(1, 3));
                    var checkOutDate = DateTime.UtcNow.AddDays(random.Next(1, 5));
                    var guest = guests[random.Next(guests.Count)];
                    var roomType = roomTypes[random.Next(roomTypes.Count)];
                    var manager = staff.FirstOrDefault(u => u.UserName.Contains("manager"));
                    var receptionist = staff.FirstOrDefault(u => u.UserName.Contains("receptionist"));

                    if (manager == null || receptionist == null)
                    {
                        logger.LogWarning("Manager or receptionist not found for checked-in reservation {Index}", i);
                        continue;
                    }

                    // Use available rooms of the selected room type
                    var availableRooms = rooms.Where(r => r.RoomTypeId == roomType.Id && r.Status == RoomStatus.Available).ToList();
                    if (availableRooms.Count == 0)
                    {
                        logger.LogWarning("No available rooms found for room type {RoomTypeId} for checked-in reservation", roomType.Id);
                        continue;
                    }

                    var room = availableRooms[random.Next(availableRooms.Count)];

                    // Mark room as occupied
                    room.Status = RoomStatus.Occupied;

                    var reservation = new Reservation
                    {
                        ReservationNumber = $"RES{DateTime.UtcNow.Year}{5 + i + 1:D4}",
                        UserId = guest.Id,
                        ReservationDate = DateTime.UtcNow.AddDays(-random.Next(5, 15)),
                        CheckInDate = checkInDate,
                        CheckOutDate = checkOutDate,
                        Status = ReservationStatus.CheckedIn,
                        TotalPrice = roomType.BasePrice * 100 * (decimal)(checkOutDate - checkInDate).TotalDays,
                        PaymentMethod = PaymentMethod.CreditCard,
                        PaymentStatus = PaymentStatus.Paid,
                        NumberOfGuests = random.Next(1, roomType.Capacity + 1),
                        SpecialRequests = "Extra towels, please.",
                        RoomTypeId = roomType.Id,
                        RoomId = room.Id,
                        CreatedBy = manager.Id,
                        CheckedInBy = receptionist.Id,
                        CheckedInTime = checkInDate
                    };

                    reservations.Add(reservation);
                }

                // Create 2 checked-out reservations
                for (int i = 0; i < 2; i++)
                {
                    var checkInDate = DateTime.UtcNow.AddDays(-random.Next(10, 20));
                    var checkOutDate = DateTime.UtcNow.AddDays(-random.Next(1, 5));
                    var guest = guests[random.Next(guests.Count)];
                    var roomType = roomTypes[random.Next(roomTypes.Count)];
                    var manager = staff.FirstOrDefault(u => u.UserName.Contains("manager"));
                    var receptionist = staff.FirstOrDefault(u => u.UserName.Contains("receptionist"));

                    if (manager == null || receptionist == null)
                    {
                        logger.LogWarning("Manager or receptionist not found for checked-out reservation {Index}", i);
                        continue;
                    }

                    // Use available rooms of the selected room type
                    var availableRooms = rooms.Where(r => r.RoomTypeId == roomType.Id && r.Status == RoomStatus.Available).ToList();
                    if (availableRooms.Count == 0)
                    {
                        logger.LogWarning("No available rooms found for room type {RoomTypeId} for checked-out reservation", roomType.Id);
                        continue;
                    }

                    var room = availableRooms[random.Next(availableRooms.Count)];

                    var reservation = new Reservation
                    {
                        ReservationNumber = $"RES{DateTime.UtcNow.Year}{8 + i + 1:D4}",
                        UserId = guest.Id,
                        ReservationDate = DateTime.UtcNow.AddDays(-random.Next(25, 35)),
                        CheckInDate = checkInDate,
                        CheckOutDate = checkOutDate,
                        Status = ReservationStatus.CheckedOut,
                        TotalPrice = roomType.BasePrice * 100 * (decimal)(checkOutDate - checkInDate).TotalDays,
                        PaymentMethod = PaymentMethod.CreditCard,
                        PaymentStatus = PaymentStatus.Paid,
                        NumberOfGuests = random.Next(1, roomType.Capacity + 1),
                        SpecialRequests = "Late check-out requested",
                        RoomTypeId = roomType.Id,
                        RoomId = room.Id,
                        CreatedBy = manager.Id,
                        CheckedInBy = receptionist.Id,
                        CheckedInTime = checkInDate,
                        CheckedOutBy = receptionist.Id,
                        CheckedOutTime = checkOutDate
                    };

                    reservations.Add(reservation);
                }

                await context.Reservations.AddRangeAsync(reservations);
                await context.SaveChangesAsync();
                logger.LogInformation("Added {Count} reservations", reservations.Count);
                return reservations;
            }

            return await context.Reservations.ToListAsync();
        }

        private static async Task SeedCleaningTasks(AppDbContext context, List<ApplicationUser> users, List<Room> rooms, ILogger logger)
        {
            logger.LogInformation("Seeding cleaning tasks...");

            if (!await context.CleaningTasks.AnyAsync())
            {
                var cleaningTasks = new List<CleaningTask>();
                var random = new Random();

                // Get housekeeper
                var housekeeper = users.FirstOrDefault(u => u.UserName.Contains("housekeeper"));
                if (housekeeper == null)
                {
                    logger.LogError("No housekeeper user found for creating cleaning tasks");
                    return;
                }

                // Create pending cleaning tasks for some available rooms
                var availableRooms = rooms.Where(r => r.Status == RoomStatus.Available).Take(10).ToList();
                foreach (var room in availableRooms.Take(5))
                {
                    var task = new CleaningTask
                    {
                        TaskId = $"CLN{DateTime.UtcNow:yyyyMMdd}{random.Next(1000, 9999)}",
                        RoomId = room.Id,
                        AssignedToId = housekeeper.Id,
                        Description = "Regular cleaning",
                        Status = CleaningRequestStatus.InProgress,
                        Priority = Priority.Medium,
                        CreatedAt = DateTime.UtcNow.AddHours(-random.Next(1, 24))
                    };

                    cleaningTasks.Add(task);
                }

                // Create in-progress cleaning tasks
                foreach (var room in availableRooms.Skip(5).Take(3))
                {
                    var task = new CleaningTask
                    {
                        TaskId = $"CLN{DateTime.UtcNow:yyyyMMdd}{random.Next(1000, 9999)}",
                        RoomId = room.Id,
                        AssignedToId = housekeeper.Id,
                        Description = "Deep cleaning",
                        Status = CleaningRequestStatus.InProgress,
                        Priority = Priority.High,
                        CreatedAt = DateTime.UtcNow.AddHours(-random.Next(1, 4))
                    };

                    cleaningTasks.Add(task);

                    // Mark room as under maintenance (being cleaned)
                    room.Status = RoomStatus.Maintenance;
                    room.NeedsCleaning = true;
                }

                // Create completed cleaning tasks
                foreach (var room in availableRooms.Skip(8).Take(2))
                {
                    var createdAt = DateTime.UtcNow.AddDays(-random.Next(1, 5));
                    var completedAt = createdAt.AddHours(random.Next(1, 3));

                    var task = new CleaningTask
                    {
                        TaskId = $"CLN{createdAt:yyyyMMdd}{random.Next(1000, 9999)}",
                        RoomId = room.Id,
                        AssignedToId = housekeeper.Id,
                        Description = "Standard cleaning",
                        Status = CleaningRequestStatus.Cleaned,
                        Priority = Priority.Medium,
                        CreatedAt = createdAt,
                        CompletedAt = completedAt,
                        CompletionNotes = "Completed standard cleaning, replaced linens and restocked amenities."
                    };

                    cleaningTasks.Add(task);

                    // Update room's cleaning status
                    room.NeedsCleaning = false;
                    room.LastCleaned = completedAt;
                    room.CleanedById = housekeeper.Id;
                }

                await context.CleaningTasks.AddRangeAsync(cleaningTasks);
                await context.SaveChangesAsync();
                logger.LogInformation("Added {Count} cleaning tasks", cleaningTasks.Count);
            }
        }

        private static async Task SeedMaintenanceRequests(AppDbContext context, List<ApplicationUser> users, List<Room> rooms, ILogger logger)
        {
            logger.LogInformation("Seeding maintenance requests...");

            if (!await context.MaintenanceRequests.AnyAsync())
            {
                var maintenanceRequests = new List<Models.MaintenanceRequest>();
                var random = new Random();

                // Get staff members
                var manager = users.FirstOrDefault(u => u.UserName.Contains("manager"));
                var housekeeper = users.FirstOrDefault(u => u.UserName.Contains("housekeeper"));

                if (manager == null || housekeeper == null)
                {
                    logger.LogError("Manager or housekeeper not found for creating maintenance requests");
                    return;
                }

                // Create maintenance requests with different statuses
                var statuses = Enum.GetValues(typeof(MaintenanceRequestStatus)).Cast<MaintenanceRequestStatus>().ToList();
                var priorities = Enum.GetValues(typeof(MaintenanceRequestPriority)).Cast<MaintenanceRequestPriority>().ToList();

                // Get different rooms
                var selectedRooms = rooms.Take(5).ToList();

                for (int i = 0; i < 5; i++)
                {
                    var reportDate = DateTime.UtcNow.AddDays(-random.Next(1, 14));
                    var status = statuses[random.Next(statuses.Count)];
                    var room = selectedRooms[i];

                    var request = new Models.MaintenanceRequest
                    {
                        ReportDate = reportDate,
                        IssueDescription = GetRandomMaintenanceIssue(),
                        Status = status,
                        Priority = priorities[random.Next(priorities.Count)],
                        ReportedBy = i % 2 == 0 ? manager.Id : housekeeper.Id,
                        AssignedTo = housekeeper.Id,
                        RoomId = room.Id
                    };

                    // Add completion info for completed requests
                    if (status == MaintenanceRequestStatus.Resolved)
                    {
                        request.CompletedAt = reportDate.AddDays(random.Next(1, 3));
                        request.ResolutionNotes = "Issue has been fixed.";
                    }

                    maintenanceRequests.Add(request);
                }

                await context.MaintenanceRequests.AddRangeAsync(maintenanceRequests);
                await context.SaveChangesAsync();
                logger.LogInformation("Added {Count} maintenance requests", maintenanceRequests.Count);
            }
        }

        private static string GetRandomMaintenanceIssue()
        {
            var issues = new[]
            {
                "Leaking faucet in bathroom",
                "Air conditioning not working",
                "Light bulb needs replacement",
                "Television remote not working",
                "Toilet clogged",
                "Shower drain slow",
                "Door lock jammed",
                "Window not closing properly",
                "Ceiling fan making noise",
                "Mini fridge not cooling"
            };

            return issues[new Random().Next(issues.Length)];
        }

        private static async Task SeedFeedback(AppDbContext context, List<ApplicationUser> users, List<Reservation> reservations, ILogger logger)
        {
            logger.LogInformation("Seeding feedback...");

            if (!await context.Feedback.AnyAsync())
            {
                var feedbacks = new List<Feedback>();
                var random = new Random();

                // Get manager for resolution
                var manager = users.FirstOrDefault(u => u.UserName.Contains("manager"));
                if (manager == null)
                {
                    logger.LogError("Manager not found for feedback resolution");
                    return;
                }

                // Categories for feedback
                var categories = new[] { "Room", "Service", "Cleanliness", "Food", "General", "Staff" };

                // Create feedback for checked-out reservations
                var completedReservations = reservations.Where(r => r.Status == ReservationStatus.CheckedOut).ToList();
                foreach (var reservation in completedReservations)
                {
                    var user = users.FirstOrDefault(u => u.Id == reservation.UserId);
                    if (user == null)
                    {
                        logger.LogWarning("User not found for reservation {ReservationId}", reservation.Id);
                        continue;
                    }

                    var rating = random.Next(3, 6); // 3-5 stars
                    var createdAt = reservation.CheckedOutTime?.AddDays(random.Next(1, 5)) ?? DateTime.UtcNow;
                    var isResolved = random.Next(2) == 0; // 50% resolved

                    var feedback = new Feedback
                    {
                        UserId = reservation.UserId,
                        ReservationId = reservation.Id,
                        GuestName = $"{user.FirstName} {user.LastName}",
                        GuestEmail = user.Email,
                        Rating = rating,
                        Subject = GetRandomFeedbackSubject(rating),
                        Comments = GetRandomFeedbackComment(rating),
                        Category = categories[random.Next(categories.Length)],
                        IsResolved = isResolved,
                        CreatedAt = createdAt
                    };

                    if (isResolved)
                    {
                        feedback.ResolutionNotes = "Thank you for your feedback. We have addressed your concerns.";
                        feedback.ResolvedById = manager.Id;
                        feedback.ResolvedAt = createdAt.AddDays(random.Next(1, 3));
                    }

                    feedbacks.Add(feedback);
                }

                // Add some standalone feedback not tied to reservations
                for (int i = 0; i < 3; i++)
                {
                    var rating = random.Next(1, 6); // 1-5 stars
                    var isResolved = rating <= 3; // Resolve negative feedback
                    var createdAt = DateTime.UtcNow.AddDays(-random.Next(1, 30));

                    var feedback = new Feedback
                    {
                        GuestName = GetRandomName(),
                        GuestEmail = $"guest{random.Next(100, 999)}@example.com",
                        Rating = rating,
                        Subject = GetRandomFeedbackSubject(rating),
                        Comments = GetRandomFeedbackComment(rating),
                        Category = categories[random.Next(categories.Length)],
                        IsResolved = isResolved,
                        CreatedAt = createdAt
                    };

                    if (isResolved)
                    {
                        feedback.ResolutionNotes = "We apologize for the inconvenience and have taken steps to improve.";
                        feedback.ResolvedById = manager.Id;
                        feedback.ResolvedAt = createdAt.AddDays(random.Next(1, 3));
                    }

                    feedbacks.Add(feedback);
                }

                await context.Feedback.AddRangeAsync(feedbacks);
                await context.SaveChangesAsync();
                logger.LogInformation("Added {Count} feedback items", feedbacks.Count);
            }
        }

        private static async Task SeedInvoicesAndPayments(AppDbContext context, List<ApplicationUser> users, List<Reservation> reservations, ILogger logger)
        {
            logger.LogInformation("Seeding invoices and payments...");

            if (!await context.Invoices.AnyAsync())
            {
                var invoices = new List<Invoice>();
                var payments = new List<Models.Payment>();
                var random = new Random();

                // Get staff for processing
                var receptionist = users.FirstOrDefault(u => u.UserName.Contains("receptionist"));
                if (receptionist == null)
                {
                    logger.LogError("Receptionist not found for payment processing");
                    return;
                }

                foreach (var reservation in reservations)
                {
                    // Create invoice for each reservation
                    var creationDate = reservation.ReservationDate.AddDays(1);
                    var IsPaid = reservation.Status == ReservationStatus.CheckedOut || reservation.Status == ReservationStatus.CheckedIn;

                    var invoice = new Invoice
                    {
                        InvoiceNumber = $"INV-{creationDate:yyyyMMdd}-{reservation.Id}",
                        ReservationId = reservation.Id,
                        Amount = reservation.TotalPrice,
                        Tax = reservation.TotalPrice * 0.12m, // 12% tax
                        Total = reservation.TotalPrice * 1.12m,
                        CreatedAt = creationDate,
                        //Notes = "Standard invoice for hotel stay",
                        IsPaid = IsPaid,
                        PaidAt = IsPaid ? DateTime.UtcNow : null
                    };

                    invoices.Add(invoice);
                }

                await context.Invoices.AddRangeAsync(invoices);
                await context.SaveChangesAsync();
                logger.LogInformation("Added {Count} invoices", invoices.Count);

                // Now add payments for paid invoices
                foreach (var invoice in invoices.Where(i => i.IsPaid))
                {
                    var reservation = reservations.FirstOrDefault(r => r.Id == invoice.ReservationId);
                    if (reservation == null) continue;

                    var paymentDate = reservation.CheckedOutTime ?? DateTime.UtcNow.AddDays(-random.Next(1, 5));

                    var payment = new Models.Payment
                    {
                        InvoiceId = invoice.Id,
                        AmountPaid = invoice.Total,
                        PaymentDate = paymentDate,
                        Method = reservation.PaymentMethod,
                        ProcessedBy = receptionist.Id,
                        TransactionId = $"TXN{paymentDate:yyyyMMdd}{random.Next(1000, 9999)}",
                        Notes = "Payment received at check-in/checkout"
                    };

                    payments.Add(payment);
                }

                if (payments.Any())
                {
                    await context.Payments.AddRangeAsync(payments);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Added {Count} payments", payments.Count);
                }
            }
        }

        private static async Task SeedServiceOrders(AppDbContext context, List<Reservation> reservations, List<Service> services, List<ApplicationUser> users, ILogger logger)
        {
            logger.LogInformation("Seeding service orders...");

            if (!await context.ServiceOrders.AnyAsync())
            {
                var serviceOrders = new List<Models.ServiceOrder>();
                var random = new Random();

                // Get staff for processing
                var housekeeper = users.FirstOrDefault(u => u.UserName.Contains("housekeeper"));
                if (housekeeper == null)
                {
                    logger.LogError("Housekeeper not found for service order completion");
                    return;
                }

                // Create service orders for checked-in and checked-out reservations
                var activeReservations = reservations.Where(r =>
                    r.Status == ReservationStatus.CheckedIn ||
                    r.Status == ReservationStatus.CheckedOut).ToList();

                foreach (var reservation in activeReservations)
                {
                    // Create 1-3 service orders per reservation
                    var orderCount = random.Next(1, 4);

                    for (int i = 0; i < orderCount; i++)
                    {
                        if (services.Count == 0) continue;

                        var service = services[random.Next(services.Count)];
                        var quantity = random.Next(1, 4);
                        var orderDate = reservation.Status == ReservationStatus.CheckedIn
                            ? DateTime.UtcNow.AddHours(-random.Next(1, 24))
                            : reservation.CheckInDate.AddHours(random.Next(24));

                        var status = reservation.Status == ReservationStatus.CheckedOut
                            ? ServiceOrderStatus.Completed
                            : (ServiceOrderStatus)random.Next(4); // Random status for checked-in

                        var serviceOrder = new Models.ServiceOrder
                        {
                            ReservationId = reservation.Id,
                            ServiceId = service.Id,
                            OrderDateTime = orderDate,
                            Quantity = quantity,
                            Status = status,
                            SpecialInstructions = i % 3 == 0 ? "Please deliver to room" : null,
                            DeliveryLocation = $"Room {reservation.Room?.RoomNumber ?? "TBD"}"
                        };

                        // Add completion details for completed orders
                        if (status == ServiceOrderStatus.Completed)
                        {
                            serviceOrder.CompletedById = housekeeper.Id;
                            serviceOrder.CompletedAt = orderDate.AddHours(random.Next(1, 3));
                            serviceOrder.CompletionNotes = "Service completed to guest's satisfaction";
                        }

                        serviceOrders.Add(serviceOrder);
                    }
                }

                await context.ServiceOrders.AddRangeAsync(serviceOrders);
                await context.SaveChangesAsync();
                logger.LogInformation("Added {Count} service orders", serviceOrders.Count);
            }
        }

        private static async Task SeedReports(AppDbContext context, List<ApplicationUser> users, ILogger logger)
        {
            logger.LogInformation("Seeding reports...");

            if (!await context.Reports.AnyAsync())
            {
                var reports = new List<Report>();
                var random = new Random();

                // Get manager for report creation
                var manager = users.FirstOrDefault(u => u.UserName.Contains("manager"));
                if (manager == null)
                {
                    logger.LogError("Manager not found for report creation");
                    return;
                }

                // Report types
                var reportTypes = Enum.GetValues(typeof(ReportType)).Cast<ReportType>().ToList();

                // Create various reports
                for (int i = 0; i < 5; i++)
                {
                    var creationDate = DateTime.UtcNow.AddDays(-random.Next(1, 30));
                    var reportType = reportTypes[random.Next(reportTypes.Count)];

                    var report = new Report
                    {
                        ReportName = $"{reportType} Report - {creationDate:MMM yyyy}",
                        ReportType = reportType,
                        CreationDate = creationDate,
                        CreatedBy = manager.Id,
                        ReportData = GetMockReportData(reportType)
                    };

                    reports.Add(report);
                }

                await context.Reports.AddRangeAsync(reports);
                await context.SaveChangesAsync();
                logger.LogInformation("Added {Count} reports", reports.Count);
            }
        }

        private static string GetRandomName()
        {
            var firstNames = new[] { "James", "Mary", "Robert", "Patricia", "John", "Jennifer", "Michael", "Linda", "William", "Elizabeth" };
            var lastNames = new[] { "Smith", "Johnson", "Williams", "Jones", "Brown", "Davis", "Miller", "Wilson", "Moore", "Taylor" };
            var random = new Random();

            return $"{firstNames[random.Next(firstNames.Length)]} {lastNames[random.Next(lastNames.Length)]}";
        }

        private static string GetRandomFeedbackSubject(int rating)
        {
            if (rating >= 4)
            {
                var positiveSubjects = new[]
                {
                    "Excellent stay",
                    "Great experience",
                    "Wonderful service",
                    "Highly recommend",
                    "Exceptional staff"
                };
                return positiveSubjects[new Random().Next(positiveSubjects.Length)];
            }
            else if (rating == 3)
            {
                var neutralSubjects = new[]
                {
                    "Average experience",
                    "Decent stay",
                    "Room for improvement",
                    "Mixed feelings",
                    "Acceptable service"
                };
                return neutralSubjects[new Random().Next(neutralSubjects.Length)];
            }
            else
            {
                var negativeSubjects = new[]
                {
                    "Disappointed with stay",
                    "Service issues",
                    "Room cleanliness concern",
                    "Would not recommend",
                    "Several problems encountered"
                };
                return negativeSubjects[new Random().Next(negativeSubjects.Length)];
            }
        }

        private static string GetRandomFeedbackComment(int rating)
        {
            if (rating >= 4)
            {
                var positiveComments = new[]
                {
                    "Everything was perfect during our stay. The staff was very friendly and helpful.",
                    "I had a wonderful experience at this hotel. The room was clean and comfortable.",
                    "The service exceeded my expectations. I'll definitely come back.",
                    "Very impressed with the amenities and staff professionalism.",
                    "Great location, excellent service, and comfortable rooms."
                };
                return positiveComments[new Random().Next(positiveComments.Length)];
            }
            else if (rating == 3)
            {
                var neutralComments = new[]
                {
                    "The stay was okay, but there were some minor issues that could be improved.",
                    "Average hotel experience. Nothing exceptional but no major problems either.",
                    "Decent room and service, but expected a bit more for the price.",
                    "Some aspects were great, others needed improvement.",
                    "Staff was friendly but response times were slow."
                };
                return neutralComments[new Random().Next(neutralComments.Length)];
            }
            else
            {
                var negativeComments = new[]
                {
                    "The room was not properly cleaned when we checked in. There were several issues with the bathroom.",
                    "Poor service and unresponsive staff. Had to make multiple requests for basic amenities.",
                    "The room did not match the description online. Very disappointing.",
                    "Noisy neighbors and staff did little to address the issue.",
                    "Several maintenance issues in the room that were never fixed despite reporting them."
                };
                return negativeComments[new Random().Next(negativeComments.Length)];
            }
        }

        private static string GetMockReportData(ReportType reportType)
        {
            var random = new Random();

            switch (reportType)
            {
                case ReportType.Occupancy:
                    return $@"{{
                        ""totalRooms"": 100,
                        ""occupiedRooms"": {random.Next(50, 95)},
                        ""occupancyRate"": {random.Next(50, 95)}.{random.Next(10, 99)},
                        ""averageDailyRate"": {random.Next(100, 250)}.{random.Next(10, 99)},
                        ""revPAR"": {random.Next(80, 200)}.{random.Next(10, 99)}
                    }}";

                case ReportType.Revenue:
                    return $@"{{
                        ""totalRevenue"": {random.Next(50000, 150000)}.00,
                        ""roomRevenue"": {random.Next(40000, 120000)}.00,
                        ""foodBeverageRevenue"": {random.Next(5000, 30000)}.00,
                        ""otherRevenue"": {random.Next(1000, 10000)}.00,
                        ""expenses"": {random.Next(30000, 100000)}.00,
                        ""profit"": {random.Next(10000, 50000)}.00
                    }}";

                case ReportType.Maintenance:
                    return $@"{{
                        ""totalRequests"": {random.Next(10, 50)},
                        ""resolvedRequests"": {random.Next(5, 45)},
                        ""pendingRequests"": {random.Next(1, 10)},
                        ""averageResolutionTime"": ""{random.Next(1, 24)} hours"",
                        ""mostCommonIssue"": ""Plumbing problems""
                    }}";

                default:
                    return $@"{{
                        ""reportDate"": ""{DateTime.UtcNow:yyyy-MM-dd}"",
                        ""generatedBy"": ""System"",
                        ""dataPoints"": {random.Next(10, 100)}
                    }}";
            }
        }

        private static async Task<bool> CreateUserWithRole(UserManager<ApplicationUser> userManager, ApplicationUser user, string password, string role, ILogger logger)
        {
            if (await userManager.FindByNameAsync(user.UserName) == null)
            {
                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    var roleResult = await userManager.AddToRoleAsync(user, role);
                    if (roleResult.Succeeded)
                    {
                        logger.LogInformation("Created user {UserName} with role {Role}", user.UserName, role);
                        return true;
                    }
                    else
                    {
                        var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                        logger.LogError("Failed to assign role {Role} to user {UserName}: {Errors}", role, user.UserName, errors);
                    }
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    logger.LogError("Failed to create user {UserName}: {Errors}", user.UserName, errors);
                }
            }
            else
            {
                logger.LogWarning("User {UserName} already exists", user.UserName);
            }
            return false;
        }
    }
}
