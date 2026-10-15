using Microsoft.EntityFrameworkCore;

namespace CitasMedicas.Infrastructure.Persistence.SqlServer;

public sealed class ClinicDbContext(DbContextOptions<ClinicDbContext> options) : DbContext(options)
{
    public DbSet<PatientRow> Patients => Set<PatientRow>();
    public DbSet<SpecialtyRow> Specialties => Set<SpecialtyRow>();
    public DbSet<DoctorRow> Doctors => Set<DoctorRow>();
    public DbSet<ScheduleSlotRow> ScheduleSlots => Set<ScheduleSlotRow>();
    public DbSet<AppointmentRow> Appointments => Set<AppointmentRow>();
    public DbSet<AttentionRow> Attentions => Set<AttentionRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PatientRow>(entity =>
        {
            entity.ToTable("Patients");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Dni).HasMaxLength(8).IsRequired();
            entity.HasIndex(x => x.Dni).IsUnique();
            entity.Property(x => x.FirstName).IsRequired();
            entity.Property(x => x.LastName).IsRequired();
            entity.Property(x => x.Phone).IsRequired();
        });

        modelBuilder.Entity<SpecialtyRow>(entity =>
        {
            entity.ToTable("Specialties");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired();
        });

        modelBuilder.Entity<DoctorRow>(entity =>
        {
            entity.ToTable("Doctors");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.LicenseNumber).IsRequired();
            entity.Property(x => x.FirstName).IsRequired();
            entity.Property(x => x.LastName).IsRequired();
            entity.HasOne<SpecialtyRow>().WithMany().HasForeignKey(x => x.SpecialtyId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ScheduleSlotRow>(entity =>
        {
            entity.ToTable("ScheduleSlots");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Date).HasColumnType("date");
            entity.Property(x => x.StartTime).HasColumnType("time");
            entity.Property(x => x.EndTime).HasColumnType("time");
            entity.HasIndex(x => new { x.DoctorId, x.Date, x.StartTime });
            entity.HasOne<DoctorRow>().WithMany().HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AppointmentRow>(entity =>
        {
            entity.ToTable("Appointments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Reason).IsRequired();
            entity.HasIndex(x => x.ScheduleSlotId).IsUnique();
            entity.HasOne<PatientRow>().WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ScheduleSlotRow>().WithMany().HasForeignKey(x => x.ScheduleSlotId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AttentionRow>(entity =>
        {
            entity.ToTable("Attentions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Diagnosis).IsRequired();
            entity.HasIndex(x => x.AppointmentId).IsUnique();
            entity.HasOne<AppointmentRow>().WithMany().HasForeignKey(x => x.AppointmentId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}

public sealed class PatientRow
{
    public int Id { get; set; }
    public string Dni { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string? Email { get; set; }
}

public sealed class SpecialtyRow
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

public sealed class DoctorRow
{
    public int Id { get; set; }
    public string LicenseNumber { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public int SpecialtyId { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ScheduleSlotRow
{
    public int Id { get; set; }
    public int DoctorId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int Status { get; set; }
}

public sealed class AppointmentRow
{
    public int Id { get; set; }
    public int ScheduleSlotId { get; set; }
    public int PatientId { get; set; }
    public string Reason { get; set; } = "";
    public int Status { get; set; }
}

public sealed class AttentionRow
{
    public int Id { get; set; }
    public int AppointmentId { get; set; }
    public string Diagnosis { get; set; } = "";
    public string? Prescription { get; set; }
}
