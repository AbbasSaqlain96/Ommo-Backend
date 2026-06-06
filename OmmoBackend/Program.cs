using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using OmmoBackend.Data;
using OmmoBackend.Exceptions;
using OmmoBackend.Handlers;
using OmmoBackend.Helpers;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Hubs;
using OmmoBackend.Middlewares;
using OmmoBackend.Repositories.Implementations;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Implementations;
using OmmoBackend.Services.Interfaces;
using OmmoBackend.Services.Interfaces.Stripe;
using OmmoBackend.Services.Implementations.Stripe;
using OmmoBackend.Validators;
using Serilog;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args); // Create a builder for configuring the web application

Stripe.StripeConfiguration.ApiKey =
    builder.Configuration["Stripe:SecretKey"];

var logPath = builder.Configuration["Logging:LogPath"] ?? "Logs";

if (!Directory.Exists(logPath))
    Directory.CreateDirectory(logPath);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console() // Write log output to the console
    .WriteTo.File(Path.Combine(logPath, "log-.txt"), rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext() // Adds additional context to logs
    .CreateLogger(); // Create the Serilog logger

// Use Serilog as the logging provider
builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddSignalR();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters
        .Add(new JsonStringEnumConverter());
    });

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

builder.Services.Configure<TokenValidationOptions>(builder.Configuration.GetSection("TokenValidation"));

// Register FluentValidation with DI
builder.Services.AddFluentValidation(fv =>
{
    //    // Register validators manually if not using automatic scanning
    //    fv.RegisterValidatorsFromAssemblyContaining<CompanyTypeValidationAttribute>();
    //    fv.RegisterValidatorsFromAssemblyContaining<CreateMaintenanceIssueRequestValidator>();
    //    fv.RegisterValidatorsFromAssemblyContaining<CreateRoleRequestValidator>();
    fv.RegisterValidatorsFromAssemblyContaining<CreateSubscriptionRequestValidator>();
    //    fv.RegisterValidatorsFromAssemblyContaining<EmailValidationAttribute>();
    //    fv.RegisterValidatorsFromAssemblyContaining<MCNumberValidationAttribute>();
    fv.RegisterValidatorsFromAssemblyContaining<VerifyOtpRequestValidator>();
});


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(option =>
{
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 1safsfsdfdfd\"",
    });
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null);
        }));
// Needed for IHttpClientFactory
builder.Services.AddHttpClient();

var twilioSid = builder.Configuration["Twilio:AccountSid"];
var twilioToken = builder.Configuration["Twilio:AuthToken"];
Twilio.TwilioClient.Init(twilioSid, twilioToken);
// Register custom exception handler and problem details services
builder.Services.AddExceptionHandler<GlobalExceptionHandler>(); // Add custom global exception handler
builder.Services.AddProblemDetails(); // Add problem details middleware to handle HTTP errors

// Register application services and repositories with dependency injection
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<ICarrierService, CarrierService>();
builder.Services.AddScoped<ICallService, CallService>();
builder.Services.AddScoped<IDriverService, DriverService>();
builder.Services.AddScoped<ITabService, TabService>();
builder.Services.AddScoped<ITrailerService, TrailerService>();
builder.Services.AddScoped<ITruckService, TruckService>();
builder.Services.AddScoped<IUnitService, UnitService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddTransient<ISMSService, SMSService>();
builder.Services.AddScoped<IOtpVerificationService, OtpVerificationService>();
//builder.Services.AddScoped<IMaintenanceIssueService, MaintenanceIssueService>();
builder.Services.AddScoped<IIssueTicketService, IssueTicketService>();
builder.Services.AddScoped<IModuleService, ModuleService>();
builder.Services.AddScoped<IAIAgentService, AIAgentService>();
//builder.Services.AddScoped<ICallTranscriptService, CallTranscriptService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IRoleModuleService, RoleModuleService>();
builder.Services.AddScoped<IAccidentDetailsService, AccidentDetailsService>();
builder.Services.AddScoped<IAccidentService, AccidentService>();
builder.Services.AddScoped<IDriverDocumentService, DriverDocumentService>();
builder.Services.AddScoped<IDriverPerformanceService, DriverPerformanceService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IIncidentService, IncidentService>();
builder.Services.AddScoped<IPerformanceService, PerformanceService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IViolationService, ViolationService>();
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<IMaintenanceCategoryService, MaintenanceCategoryService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IOnboardingService, OnboardingService>();
builder.Services.AddScoped<IIntegrationService, IntegrationService>();
builder.Services.AddScoped<ILoadBoardService, LoadBoardService>();
builder.Services.AddScoped<ICallService, CallService>();
builder.Services.AddScoped<ITokenValidationService, TokenValidationService>();
builder.Services.AddScoped<IAiInsightsService, AiInsightsService>();
builder.Services.AddScoped<IAIAgentSettingService, AIAgentSettingService>();
builder.Services.AddScoped<ICompanyOnboardingService, CompanyOnboardingService>();
builder.Services.AddScoped<IPlansService, PlansService>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<ICallSettingsService, CallSettingsService>();
builder.Services.AddScoped<ISupportService, SupportService>();
builder.Services.AddScoped<IStripeBillingService, StripeBillingService>();
builder.Services.AddScoped<IPackagePlanValidationService, PackagePlanValidationService>();
builder.Services.AddScoped<IPeriodCalculationService, PeriodCalculationService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IStripeMetadataService, StripeMetadataService>();

builder.Services.AddScoped<IStripeEventHandler, CheckoutSessionCompletedHandler>();
builder.Services.AddScoped<IStripeEventHandler, InvoicePaidHandler>();
builder.Services.AddScoped<IStripeEventHandler, InvoicePaymentFailedHandler>();
builder.Services.AddScoped<IStripeEventHandler, SubscriptionDeletedHandler>();

builder.Services.Configure<StripeSettings>(
    builder.Configuration.GetSection("Stripe"));

builder.Services.AddSingleton<IEncryptionService, AesGcmEncryptionService>();

builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
builder.Services.AddScoped<ICarrierRepository, CarrierRepository>();
builder.Services.AddScoped<IDispatchServiceRepository, DispatchServiceRepository>();
builder.Services.AddScoped<IAIAgentRepository, AIAgentRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IModuleRepository, ModuleRepository>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<IRequestModuleRepository, RequestModuleRepository>();
builder.Services.AddScoped<IDriverRepository, DriverRepository>();
builder.Services.AddScoped<IRoleModuleRelationshipRepository, RoleModuleRelationshipRepository>();
builder.Services.AddScoped<ITabRepository, TabRepository>();
builder.Services.AddScoped<ITrailerRepository, TrailerRepository>();
builder.Services.AddScoped<ITruckRepository, TruckRepository>();
builder.Services.AddScoped<IUnitRepository, UnitRepository>();
builder.Services.AddScoped<IOtpRepository, OtpRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
//builder.Services.AddScoped<IMaintenanceIssueRepository, MaintenanceIssueRepository>();
builder.Services.AddScoped<IIssueTicketRepository, IssueTicketRepository>();
builder.Services.AddScoped<IAccidentDetailsRepository, AccidentDetailsRepository>();
builder.Services.AddScoped<IAccidentRepository, AccidentRepository>();
builder.Services.AddScoped<IClaimRepository, ClaimRepository>();
builder.Services.AddScoped<IDriverDocumentRepository, DriverDocumentRepository>();
builder.Services.AddScoped<IDriverPerformanceRepository, DriverPerformanceRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IIncidentRepository, IncidentRepository>();
builder.Services.AddScoped<IPerformanceRepository, PerformanceRepository>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<IViolationRepository, ViolationRepository>();
builder.Services.AddScoped<ITicketDocRepository, TicketDocRepository>();
builder.Services.AddScoped<IIncidentPicturesRepository, IncidentPicturesRepository>();
builder.Services.AddScoped<IAccidentDocRepository, AccidentDocRepository>();
builder.Services.AddScoped<IAccidentPicturesRepository, AccidentPicturesRepository>();
builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IAssetRepository, AssetRepository>();
builder.Services.AddScoped<IMaintenanceCategoryRepository, MaintenanceCategoryRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IIncidentTypeRepository, IncidentTypeRepository>();
builder.Services.AddScoped<IIncidentDocRepository, IncidentDocRepository>();
builder.Services.AddScoped<IIncidentEquipDamageRepository, IncidentEquipDamageRepository>();
builder.Services.AddScoped<IIncidentTypeRepository, IncidentTypeRepository>();
builder.Services.AddScoped<ITicketPictureRepository, TicketPictureRepository>();
builder.Services.AddScoped<IDotInspectionService, DotInspectionService>();
builder.Services.AddScoped<IDotInspectionRepository, DotInspectionRepository>();
builder.Services.AddScoped<IWarningService, WarningService>();
builder.Services.AddScoped<IWarningRepository, WarningRepository>();
builder.Services.AddScoped<IDocInspectionRepository, DocInspectionRepository>();
builder.Services.AddScoped<IWarningDocRepository, WarningDocRepository>();
builder.Services.AddScoped<ISendEmailRepository, SendEmailRepository>();
builder.Services.AddScoped<IIntegrationRepository, IntegrationRepository>();
builder.Services.AddScoped<IGlobalIntegrationCredentialRepository, GlobalIntegrationCredentialRepository>();
builder.Services.AddScoped<ICallRepository, CallRepository>();
builder.Services.AddScoped<IDefaultIntegrationRepository, DefaultIntegrationRepository>();
builder.Services.AddScoped<IAIAgentSettingRepository, AIAgentSettingRepository>();
builder.Services.AddScoped<ICompanyOnboardingRepository, CompanyOnboardingRepository>();
builder.Services.AddScoped<IOnboardingRepository, OnboardingRepository>();
builder.Services.AddScoped<IQuestionnaireRepository, QuestionnaireRepository>();
builder.Services.AddScoped<ICustomPackageRepository, CustomPackageRepository>();
builder.Services.AddScoped<IPlanRepository, PlanRepository>();
builder.Services.AddScoped<IBillingRepository, BillingRepository>();
builder.Services.AddScoped<ICompanyPaymentProfileRepository, CompanyPaymentProfileRepository>();
builder.Services.AddScoped<IPackagePlanRepository, PackagePlanRepository>();
builder.Services.AddScoped<ISupportRequestRepository, SupportRequestRepository>();
builder.Services.AddScoped<ICompanyPlanChangeRequestRepository, CompanyPlanChangeRequestRepository>();
builder.Services.AddScoped<IStripeProcessedEventRepository, StripeProcessedEventRepository>();

// UnitOfWork
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// configure HttpClient for Truckstop and DAT
builder.Services.AddHttpClient();

builder.Services.AddHttpClient<IUltravoxService, UltravoxService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10); // Fail fast
});

builder.Services.AddSingleton<ISecretProtector, DataProtectionSecretProtector>();
builder.Services.AddDataProtection().SetApplicationName("IntegrationEmailPoller");

builder.Services.AddHostedService<IntegrationEmailPollerService>();
builder.Services.AddHostedService<TranscriptPollingService>();

// Configures Npgsql to use legacy timestamp behavior for compatibility
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
NpgsqlConnection.GlobalTypeMapper.MapEnum<OtpSubject>("otp_subject_enum");
NpgsqlConnection.GlobalTypeMapper.MapEnum<CompanyStatus>("company_status");
NpgsqlConnection.GlobalTypeMapper.MapEnum<UserStatus>("user_status");
NpgsqlConnection.GlobalTypeMapper.MapEnum<AccessLevel>("access_level_enum");
NpgsqlConnection.GlobalTypeMapper.MapEnum<DocType>("doc_type_enum");
NpgsqlConnection.GlobalTypeMapper.MapEnum<DocCategory>("doc_cat_enum");
NpgsqlConnection.GlobalTypeMapper.MapEnum<CompanyType>("company_type_access");
NpgsqlConnection.GlobalTypeMapper.MapEnum<DriverStatus>("status_enum");
NpgsqlConnection.GlobalTypeMapper.MapEnum<EmploymentType>("employment_type_enum");
NpgsqlConnection.GlobalTypeMapper.MapEnum<HiringStatus>("hiring_status_enum");
NpgsqlConnection.GlobalTypeMapper.MapEnum<IssueCat>("issue_cat_enum");
NpgsqlConnection.GlobalTypeMapper.MapEnum<IssueTicketStatus>("issue_ticket_status_enum");
NpgsqlConnection.GlobalTypeMapper.MapEnum<IssueType>("issue_type_enum");
NpgsqlConnection.GlobalTypeMapper.MapEnum<LicensePlateState>("license_plate_state_enum");
NpgsqlConnection.GlobalTypeMapper.MapEnum<LicenseState>("license_state_enum");
NpgsqlConnection.GlobalTypeMapper.MapEnum<Priority>("priority_enum");
NpgsqlConnection.GlobalTypeMapper.MapEnum<RoleCategory>("role_category");
NpgsqlConnection.GlobalTypeMapper.MapEnum<UnitStatus>("unit_status_enum");
NpgsqlConnection.GlobalTypeMapper.MapEnum<VehicleStatus>("vehicle_status_enum");
NpgsqlConnection.GlobalTypeMapper.MapEnum<VehicleType>("vehicle_type_enum");
NpgsqlConnection.GlobalTypeMapper.MapEnum<USState>("us_state_enum");
NpgsqlConnection.GlobalTypeMapper.MapEnum<TrailerType>("trailer_type_enum");
NpgsqlConnection.GlobalTypeMapper.MapEnum<DriverDocStatus>("driver_doc_status_enum");
NpgsqlConnection.GlobalTypeMapper.MapEnum<EventType>("event_type_enum");
NpgsqlConnection.GlobalTypeMapper.MapEnum<UnitTicketStatus>("unit_ticket_status_enum");
NpgsqlConnection.GlobalTypeMapper.MapEnum<ClaimType>("claim_type_enum");
NpgsqlConnection.GlobalTypeMapper.MapEnum<ClaimStatus>("claim_status_enum");
NpgsqlConnection.GlobalTypeMapper.MapEnum<EventAuthority>("event_authority_enum");
NpgsqlConnection.GlobalTypeMapper.MapEnum<FeesPaidBy>("fees_paid_by_enum");
NpgsqlConnection.GlobalTypeMapper.MapEnum<DocInspectionStatus>("doc_inspection_status_enum");
NpgsqlConnection.GlobalTypeMapper.MapEnum<InspectionLevel>("inspection_level_enum");
NpgsqlConnection.GlobalTypeMapper.MapEnum<CitationStatus>("citation_enum");
NpgsqlConnection.GlobalTypeMapper.MapEnum<OnboardingStep>("onboarding_step");
NpgsqlConnection.GlobalTypeMapper.MapEnum<SubscriptionStatus>("subscription_status");
NpgsqlConnection.GlobalTypeMapper.MapEnum<VerificationStatus>("verification_status");
NpgsqlConnection.GlobalTypeMapper.MapEnum<PlanStatus>("plan_status");
NpgsqlConnection.GlobalTypeMapper.MapEnum<PlanType>("plan_type");
NpgsqlConnection.GlobalTypeMapper.MapEnum<PlanInterval>("plan_interval");

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader()
        .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("SignalRCors", policy =>
    {
        policy
            .WithOrigins("https://ommo.ai", "http://localhost:5173")// 👈 frontend origin
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials(); // 👈 REQUIRED
    });
});

// Add JWT authentication services
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
        ValidIssuer = builder.Configuration["Jwt:Issuer"], // Set in configuration (appsettings.json or environment variables)
        ValidAudience = builder.Configuration["Jwt:Audience"], // Set in configuration
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])), // Set in configuration
        NameClaimType = ClaimTypes.NameIdentifier // Set the NameClaimType to match the user identifier
    };
});

var app = builder.Build();

// Use the global exception handler
app.UseExceptionHandler();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//  app.UseHttpsRedirection(); // Redirect HTTP requests to HTTPS
//}

app.UseSwagger(); // Enable Swagger
app.UseSwaggerUI(); // Enable Swagger

//var contentRoot = builder.Environment.ContentRootPath;

//app.UseStaticFiles(new StaticFileOptions
//{
//    FileProvider = new PhysicalFileProvider(Path.Combine(contentRoot, "ProfilePicture")),
//    RequestPath = "/ProfilePicture"
//});

//app.UseStaticFiles(new StaticFileOptions
//{
//    FileProvider = new PhysicalFileProvider(Path.Combine(contentRoot, "Documents")),
//    RequestPath = "/Documents"
//});

//app.UseStaticFiles(new StaticFileOptions
//{
//    FileProvider = new PhysicalFileProvider(Path.Combine(contentRoot, "Logo")),
//    RequestPath = "/Logo"
//});

app.UseStaticFiles(new StaticFileOptions
{
    //FileProvider = new PhysicalFileProvider("/var/www/ommo-backend/ProfilePicture"),
    FileProvider = new PhysicalFileProvider("/IT Company/Ommo-Backend/OmmoBackend/ProfilePicture"),
    RequestPath = "/ProfilePicture"
});

app.UseStaticFiles(new StaticFileOptions
{
    //FileProvider = new PhysicalFileProvider("/var/www/ommo-backend/Documents"),
    FileProvider = new PhysicalFileProvider("/IT Company/Ommo-Backend/OmmoBackend/Documents"),
    RequestPath = "/Documents"
});

app.UseStaticFiles(new StaticFileOptions
{
    //FileProvider = new PhysicalFileProvider("/var/www/ommo-backend/Logo"),
    FileProvider = new PhysicalFileProvider("/IT Company/Ommo-Backend/OmmoBackend/Logo"),
    RequestPath = "/Logo"
});

app.UseSerilogRequestLogging();

app.UseRouting();

app.UseCors("SignalRCors");

app.UseAuthentication();

app.UseAuthorization();

app.UseMiddleware<RolePermissionMiddleware>();

// Use Notification Middleware
app.UseMiddleware<NotificationMiddleware>();

//app.UseStaticFiles(); // Enable serving static files from the wwwroot folder

app.MapControllers(); // Map attribute-routed controllers

// Map SignalR hub
app.MapHub<NotificationHub>("/notifications");
app.MapHub<CallTranscriptHub>("/hubs/callTranscript");
app.MapHub<UserChatHub>("/hubs/user-chat");

app.Run();