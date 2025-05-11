
using HotelManagementApp.Models;
using HotelManagementApp.Services;
using HotelManagementApp.Services.Authentication;
using HotelManagementApp.Services.CleaningTaskSpace;
using HotelManagementApp.Services.FeedbackServiceSpace;
using HotelManagementApp.Services.InvoiceSpace;
using HotelManagementApp.Services.MaintenanceRequest;
using HotelManagementApp.Services.Payment;
using HotelManagementApp.Services.ReservationServiceSpace;
using HotelManagementApp.Services.RoomServiceSpace;
using HotelManagementApp.Services.ServiceOrder;
using HotelManagementApp.Services.ServiceService;
using HotelManagementApp.Services.Statistics;
using HotelManagementApp.Services.UserManagement;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace HotelManagementApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Create the WebApplicationBuilder
            var builder = WebApplication.CreateBuilder(args);

            // Configure logging
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();
            builder.Logging.SetMinimumLevel(LogLevel.Debug);

            Console.WriteLine("Starting application initialization...");

            // Add DbContext with improved error handling
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    sqlServerOptionsAction: sqlOptions => {
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                    }
                ));

            Console.WriteLine($"Connection string: {builder.Configuration.GetConnectionString("DefaultConnection")}");

            // Add Identity
            builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            // Add JWT authentication with fallback for development
            try
            {
                if (string.IsNullOrEmpty(builder.Configuration["Jwt:Key"]) ||
                    string.IsNullOrEmpty(builder.Configuration["Jwt:Issuer"]) ||
                    string.IsNullOrEmpty(builder.Configuration["Jwt:Audience"]))
                {
                    Console.WriteLine("WARNING: JWT configuration is missing or incomplete in appsettings.json");

                    // Use fallback values for development
                    if (builder.Environment.IsDevelopment())
                    {
                        Console.WriteLine("Using fallback JWT configuration for development environment");
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            "JWT configuration is missing in appsettings.json. Please check Jwt:Key, Jwt:Issuer, and Jwt:Audience.");
                    }
                }

                var key = Encoding.ASCII.GetBytes(
                    builder.Configuration["Jwt:Key"] ??
                    "DefaultKeyForDevelopmentThatIsAtLeast32CharsLong");

                builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "http://localhost:7138",
                        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "http://localhost:5500",
                        IssuerSigningKey = new SymmetricSecurityKey(key)
                    };
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error configuring JWT authentication: {ex.Message}");
                throw;
            }

            // Register services
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IFeedbackService, FeedbackService>();
            builder.Services.AddScoped<ICleaningService, CleaningService>();
            builder.Services.AddScoped<IInvoiceService, InvoiceService>();
            builder.Services.AddScoped<IMaintenanceRequestService, MaintenanceRequestService>();
            builder.Services.AddScoped<IPaymentService, PaymentService>();
            builder.Services.AddScoped<IReservationService, ReservationService>();
            builder.Services.AddScoped<RoomService>();
            builder.Services.AddScoped<IServiceService, ServiceService>();
            builder.Services.AddScoped<IServiceOrderService, ServiceOrderService>();
            builder.Services.AddScoped<IStatisticsService, StatisticsService>();

            // Add API controllers
            builder.Services.AddControllers();

            // Add Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Build the application
            WebApplication app;
            try
            {
                app = builder.Build();
                Console.WriteLine("Application built successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error building application: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return;
            }

            // Configure the HTTP request pipeline (middleware)
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                Console.WriteLine("Configured Swagger for development environment");
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            // Initialize the database
            try
            {
                using (var scope = app.Services.CreateScope())
                {
                    var services = scope.ServiceProvider;

                    try
                    {
                        // Test database connection first
                        var dbContext = services.GetRequiredService<AppDbContext>();
                        Console.WriteLine("Testing database connection...");

                        // Try simple database operations to verify connection
                        var canConnect = dbContext.Database.CanConnectAsync().GetAwaiter().GetResult();
                        Console.WriteLine($"Database connection test result: {canConnect}");

                        if (!canConnect)
                        {
                            Console.WriteLine("WARNING: Cannot connect to database. Check your connection string.");

                            // For development, we can create the database if it doesn't exist
                            if (app.Environment.IsDevelopment())
                            {
                                Console.WriteLine("Attempting to create database...");
                                dbContext.Database.EnsureCreated();
                                Console.WriteLine("Database creation attempted");
                            }
                        }

                        // Initialize database with test data if in development
                        Console.WriteLine("Initializing database...");
                        // DbInitializer.Initialize(services, app.Environment.IsDevelopment()).GetAwaiter().GetResult();
                        RunTestDataSeeder(app);
                        Console.WriteLine("Database initialized successfully");

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Database initialization error: {ex.Message}");
                        Console.WriteLine(ex.StackTrace);

                        // Log the error but don't crash in development
                        if (app.Environment.IsDevelopment())
                        {
                            var logger = services.GetService<ILogger<Program>>();
                            if (logger != null)
                            {
                                logger.LogError(ex, "An error occurred while initializing the database");
                            }
                        }
                        else
                        {
                            // Rethrow in production to prevent starting with invalid database
                            throw;
                        }
                    }
                }

                Console.WriteLine("Application initialization completed, starting web server...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Startup error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);

                // Only crash in production, let development continue with issues
                if (!app.Environment.IsDevelopment())
                {
                    return;
                }
            }
            // Run database seeder
            
            // Run the application with synchronous Run rather than async RunAsync
            app.Run();
        }
        private static void RunTestDataSeeder(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;

            try
            {
                var context = services.GetRequiredService<AppDbContext>();
                var logger = services.GetRequiredService<ILogger<Program>>();

                // Ensure database is created
                context.Database.EnsureCreated();

                // Run the TestDataSeeder
                TestDataSeeder.SeedTestData(services, logger).GetAwaiter().GetResult();

                logger.LogInformation("Test data seeding completed successfully.");
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred while seeding the test data.");
            }
        }
    }
}
