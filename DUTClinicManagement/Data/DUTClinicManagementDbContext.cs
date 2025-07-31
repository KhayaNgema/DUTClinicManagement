using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DUTClinicManagement.Data;

public class DUTClinicManagementDbContext : IdentityDbContext<IdentityUser>
{
    public DUTClinicManagementDbContext(DbContextOptions<DUTClinicManagementDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

    }

    public DbSet<DUTClinicManagement.Models.ActivityLog> ActivityLogs { get; set; }

    public DbSet<DUTClinicManagement.Models.DeviceInfo> DeviceInfos { get; set; }

    public DbSet<DUTClinicManagement.Models.Payment> Payments { get; set; }

    public DbSet<DUTClinicManagement.Models.SystemAdministrator> SystemAdministrators { get; set; }

    public DbSet<DUTClinicManagement.Models.UserBaseModel> Users { get; set; }

    public DbSet<DUTClinicManagement.Models.Booking> Bookings { get; set; }

    public DbSet<DUTClinicManagement.Models.Patient> Patients { get; set; }

    public DbSet<DUTClinicManagement.Models.Doctor> Doctors { get; set; }

    public DbSet<DUTClinicManagement.Models.MedicalHistory> MedicalHistorys { get; set; }

    public DbSet<DUTClinicManagement.Models.Medication> Medications { get; set; }

    public DbSet<DUTClinicManagement.Models.MedicationInventory> MedicationInventory { get; set; }

    public DbSet<DUTClinicManagement.Models.PatientMedicalHistory> PatientMedicalHistories { get; set; }

    public DbSet<DUTClinicManagement.Models.X_RayAppointment> X_RayAppointments { get; set; }

    public DbSet<DUTClinicManagement.Models.Pharmacist> Pharmacists { get; set; }

    public DbSet<DUTClinicManagement.Models.MedicationPescription> MedicationPescription { get; set; }

    public DbSet<DUTClinicManagement.Models.Receptionist> Receptionists { get; set; }

    public DbSet<DUTClinicManagement.Models.Room> Rooms { get; set; }

    public DbSet<DUTClinicManagement.Models.MedicationCategory> MedicationCategories { get; set; }
    public DbSet<DUTClinicManagement.Models.DeliveryPersonnel> DeliveryGuys { get; set; }

    public DbSet<DUTClinicManagement.Models.Nurse> Nurses { get; set; }
}
