using Microsoft.EntityFrameworkCore;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Models;

namespace OmmoBackend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Company> company { get; set; }

        public DbSet<Carrier> carrier { get; set; }

        public DbSet<DispatchService> dispatch_service { get; set; }

        public DbSet<Role> role { get; set; }

        public DbSet<User> users { get; set; }

        public DbSet<Module> module { get; set; }

        public DbSet<RoleModuleRelationship> role_module_relationship { get; set; }

        public DbSet<SubscriptionRequest> subscription_request { get; set; }

        public DbSet<RequestModule> request_module { get; set; }

        public DbSet<Component> component { get; set; }

        public DbSet<Driver> driver { get; set; }

        public DbSet<Unit> unit { get; set; }

        public DbSet<Truck> truck { get; set; }

        //public DbSet<TruckTrailerLocation> truck_trailer_location { get; set; }

        public DbSet<TruckTracking> truck_tracking { get; set; }

        public DbSet<Trailer> trailer { get; set; }

        public DbSet<Otp> otp { get; set; }

        public DbSet<RefreshToken> refresh_tokens { get; set; }

        public DbSet<MaintenanceCategory> maintenance_category { get; set; }

        public DbSet<IssueTicket> issue_ticket { get; set; }

        public DbSet<RoleComponentRelationship> role_component_relationship { get; set; }

        public DbSet<Vehicle> vehicle { get; set; }

        public DbSet<DocumentType> document_type { get; set; }

        public DbSet<DriverDoc> driver_doc { get; set; }

        public DbSet<EventDriver> event_driver { get; set; }

        public DbSet<TruckLocation> truck_location { get; set; }

        public DbSet<Accident> accident { get; set; }

        public DbSet<Claims> claim { get; set; }

        public DbSet<Incident> incident { get; set; }

        public DbSet<Violation> violation { get; set; }

        public DbSet<PerformanceEvents> performance_event { get; set; }

        public DbSet<UnitTicket> unit_ticket { get; set; }

        public DbSet<AccidentDoc> accident_doc { get; set; }

        public DbSet<ViolationTicket> ticket_violation { get; set; }

        public DbSet<TicketDoc> ticket_doc { get; set; }

        public DbSet<AccidentPicture> accident_pictures { get; set; }
        public DbSet<IncidentPicture> incident_pictures { get; set; }

        public DbSet<VehicleAttribute> vehicle_attributes { get; set; }

        public DbSet<VehicleDocument> vehicle_document { get; set; }

        public DbSet<TicketFile> ticket_file { get; set; }

        public DbSet<Notification> notifications { get; set; }

        public DbSet<IncidentEquipDamage> incident_equip_damage { get; set; }
        public DbSet<IncidentEquipDamageRelationship> incident_equip_damage_relationship { get; set; }
        public DbSet<IncidentType> incident_type { get; set; }
        public DbSet<IncidentTypeIncidentRelationship> incident_type_incident_relationship { get; set; }
        public DbSet<IncidentDoc> incident_doc { get; set; }
        public DbSet<TicketPicture> ticket_pictures { get; set; }
        public DbSet<DocInspection> doc_inspection { get; set; }
        public DbSet<DocInspectionViolation> doc_inspection_violation { get; set; }
        public DbSet<DocInspectionDocument> doc_inspection_documents { get; set; }
        public DbSet<Warning> warning { get; set; }
        public DbSet<WarningViolation> warning_violation { get; set; }
        public DbSet<WarningDocument> warning_documents { get; set; }
        public DbSet<SendEmail> send_email { get; set; }

        public DbSet<Agent> agent { get; set; }

        public DbSet<AgentSettings> agent_settings { get; set; }
        public DbSet<Call> call { get; set; }
        public DbSet<CallTranscript> call_transcript { get; set; }

        public DbSet<Integrations> integrations { get; set; }
        public DbSet<DefaultIntegrations> default_integrations { get; set; }
        public DbSet<GlobalIntegrationCredentials> global_integration_credentials { get; set; }

        public DbSet<IntegrationEmailProcess> integration_email_process { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure AccessLevel enum to be stored as int in the database
            modelBuilder.Entity<RoleModuleRelationship>()
                .Property(e => e.access_level)
                .HasConversion<int>();

            modelBuilder.Entity<RoleComponentRelationship>()
                .Property(e => e.access_level)
                .HasConversion<int>();

            modelBuilder.Entity<Truck>()
                .Property(t => t.truck_status)
                .HasConversion<string>();

            modelBuilder
                .Entity<Module>()
                .Property(e => e.company_type_access)
                .HasConversion<string>();

            modelBuilder.Entity<RoleModuleRelationship>()
                .HasKey(r => new { r.role_id, r.module_id });

            modelBuilder.Entity<TruckLocation>()
                .Property(t => t.location_state)
                .HasConversion(
                    v => v.ToString(),
                    v => (USState)Enum.Parse(typeof(USState), v, true)
                );

            modelBuilder.Entity<TruckLocation>()
                .Property(t => t.location_state)
                .HasConversion<string>();

            //modelBuilder.Entity<Driver>()
            //    .Property(t => t.license_state)
            //    .HasConversion(
            //        v => v.ToString(),
            //        v => (LicenseState)Enum.Parse(typeof(LicenseState), v, true)
            //    );

            //modelBuilder.Entity<Driver>()
            //    .Property(t => t.license_state)
            //    .HasConversion<string>();

            modelBuilder.Entity<AccidentDoc>(entity =>
            {
                entity.HasNoKey(); // Mark it as a keyless entity
                entity.ToTable("accident_doc"); // Optional: Map to a specific table
            });

            //modelBuilder.Entity<DocumentType>()
            //   .Property(t => t.doc_type)
            //   .HasConversion(
            //       v => v.ToString(),
            //       v => (DocType)Enum.Parse(typeof(DocType), v, true)
            //   );

            //modelBuilder.Entity<DocumentType>() // Replace with your actual entity class name
            //    .Property(e => e.doc_type)
            //    .HasConversion<string>(); // Converts between the enum and string


            modelBuilder.Entity<TicketDoc>()
        .Property(e => e.status)
        .HasConversion(
            v => v.ToString(),  // Convert Enum to string when saving
            v => (TicketDocStatus)Enum.Parse(typeof(TicketDocStatus), v) // Convert string back to Enum when reading
        );

            modelBuilder.Entity<Truck>()
           .Property(t => t.fuel_type)
           .HasConversion(
               v => v.ToString(),  // Convert enum to string when saving to DB
               v => (TruckFuelType)Enum.Parse(typeof(TruckFuelType), v) // Convert string back to enum when reading from DB
           );

            modelBuilder.Entity<AccidentDoc>()
       .HasKey(ad => ad.accident_doc_id); // Ensure this is the correct PK

            modelBuilder.Entity<AccidentDoc>()
        .Property(e => e.status)
        .HasConversion(
            v => v.ToString(),  // Convert Enum to string when saving
            v => (AccidentDocStatus)Enum.Parse(typeof(AccidentDocStatus), v) // Convert string back to Enum when reading
        );

            modelBuilder.Entity<VehicleDocument>()
    .Property(e => e.state_code)
    .HasConversion(
        v => v.ToString(),  // Convert Enum to string when saving
        v => (USState)Enum.Parse(typeof(USState), v) // Convert string back to Enum when reading
    );

            modelBuilder.Entity<VehicleDocument>()
.Property(e => e.status)
.HasConversion(
v => v.ToString(),  // Convert Enum to string when saving
v => (VehicleDocumentStatus)Enum.Parse(typeof(VehicleDocumentStatus), v) // Convert string back to Enum when reading
);

            //          modelBuilder.Entity<MaintenanceCategory>()
            //.Property(e => e.cat_type)
            //.HasConversion(
            //    v => v.ToString(),  // Convert Enum to string when saving
            //    v => (MaintenanceCategoryType)Enum.Parse(typeof(MaintenanceCategoryType), v) // Convert string back to Enum when reading
            //);


            modelBuilder.Entity<MaintenanceCategory>()
                .Property(t => t.cat_type)
                .HasConversion<string>();

            modelBuilder.Entity<IssueTicket>()
          .Property(t => t.recurrent_type)
          .HasConversion<string>();
        }
    }
}