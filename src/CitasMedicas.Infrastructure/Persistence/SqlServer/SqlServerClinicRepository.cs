using System.Data;
using CitasMedicas.Application.Abstractions.Persistence;
using CitasMedicas.Application.Catalogs;
using CitasMedicas.Application.Common;
using CitasMedicas.Domain.Appointments;
using CitasMedicas.Domain.Common;
using CitasMedicas.Domain.Doctors;
using CitasMedicas.Domain.Patients;
using CitasMedicas.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;

namespace CitasMedicas.Infrastructure.Persistence.SqlServer;

public sealed class SqlServerClinicRepository(ClinicDbContext db, ClinicClock clock) :
    IPatientRepository, IDoctorRepository, ISpecialtyRepository, IAppointmentRepository
{
    public async Task<Result<Patient>> AddAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        var row = new PatientRow
        {
            Dni = patient.Dni.Value,
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            Phone = patient.Phone,
            Email = patient.Email?.Value
        };
        db.Patients.Add(row);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return ToDomain(row);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            db.Entry(row).State = EntityState.Detached;
            return PatientErrors.DniDuplicate;
        }
    }

    async Task<Patient?> IPatientRepository.GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var row = await db.Patients.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return row is null ? null : ToDomain(row);
    }

    public async Task<Patient?> GetByDniAsync(string dni, CancellationToken cancellationToken = default)
    {
        var row = await db.Patients.AsNoTracking().SingleOrDefaultAsync(x => x.Dni == dni, cancellationToken);
        return row is null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyList<Patient>> SearchByLastNameAsync(string text, CancellationToken cancellationToken = default)
    {
        var query = TextNormalizer.ForSearch(text);
        var rows = await db.Patients.AsNoTracking().ToListAsync(cancellationToken);
        return rows.Where(x => TextNormalizer.ForSearch(x.LastName).Contains(query))
            .OrderBy(x => TextNormalizer.ForSearch(x.LastName), StringComparer.Ordinal)
            .ThenBy(x => TextNormalizer.ForSearch(x.FirstName), StringComparer.Ordinal)
            .Select(ToDomain).ToList();
    }

    async Task<Doctor?> IDoctorRepository.GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var row = await (from doctor in db.Doctors.AsNoTracking()
                         join specialty in db.Specialties.AsNoTracking() on doctor.SpecialtyId equals specialty.Id
                         where doctor.Id == id
                         select new { doctor, specialty }).SingleOrDefaultAsync(cancellationToken);
        return row is null ? null : ToDomain(row.doctor, row.specialty, row.doctor.IsActive && row.specialty.IsActive);
    }

    public async Task<IReadOnlyList<DoctorCatalogResponse>> ListAsync(bool includeInactive, CancellationToken cancellationToken = default)
    {
        var query = from doctor in db.Doctors.AsNoTracking()
                    join specialty in db.Specialties.AsNoTracking() on doctor.SpecialtyId equals specialty.Id
                    select new { doctor, specialty };
        if (!includeInactive) query = query.Where(x => x.doctor.IsActive);
        var rows = await query.OrderBy(x => x.doctor.LastName).ThenBy(x => x.doctor.FirstName).ToListAsync(cancellationToken);
        return rows.Select(x => ToCatalog(x.doctor, x.specialty)).ToList();
    }

    public async Task<Result<DoctorCatalogResponse>> AddAsync(DoctorWriteRequest request, CancellationToken cancellationToken = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var specialty = await LockSpecialtyAsync(request.SpecialtyId!.Value, cancellationToken);
        if (specialty is null) return CatalogErrors.SpecialtyNotFound;
        if (!specialty.IsActive) return CatalogErrors.DoctorSpecialtyNotActive;
        var row = new DoctorRow
        {
            LicenseNumber = request.LicenseNumber!, FirstName = request.FirstName!, LastName = request.LastName!,
            SpecialtyId = specialty.Id, IsActive = true
        };
        db.Doctors.Add(row);
        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return ToCatalog(row, specialty);
    }

    public async Task<Result<DoctorCatalogResponse>> UpdateAsync(int id, DoctorWriteRequest request, CancellationToken cancellationToken = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var row = await db.Doctors.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null) return CatalogErrors.DoctorNotFound;
        var specialty = await LockSpecialtyAsync(request.SpecialtyId!.Value, cancellationToken);
        if (specialty is null) return CatalogErrors.SpecialtyNotFound;
        if (!specialty.IsActive) return CatalogErrors.DoctorSpecialtyNotActive;
        row.LicenseNumber = request.LicenseNumber!;
        row.FirstName = request.FirstName!;
        row.LastName = request.LastName!;
        row.SpecialtyId = specialty.Id;
        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return ToCatalog(row, specialty);
    }

    public async Task<Result> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var initialDoctor = await db.Doctors.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (initialDoctor is null) return CatalogErrors.DoctorNotFound;
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        await LockSpecialtyAsync(initialDoctor.SpecialtyId, cancellationToken);
        await LockDoctorAsync(id, cancellationToken);
        var row = await db.Doctors.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null) return CatalogErrors.DoctorNotFound;
        if (await HasFutureUnfinishedAppointmentForDoctor(id, cancellationToken)) return CatalogErrors.DoctorFutureAppointmentsExist;
        row.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return Result.Success();
    }

    async Task<IReadOnlyList<SpecialtyResponse>> ISpecialtyRepository.ListAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        var query = db.Specialties.AsNoTracking();
        if (!includeInactive) query = query.Where(x => x.IsActive);
        return await query.OrderBy(x => x.Name).Select(x => new SpecialtyResponse(x.Id, x.Name, x.IsActive)).ToListAsync(cancellationToken);
    }

    public async Task<Result<SpecialtyResponse>> AddAsync(SpecialtyWriteRequest request, CancellationToken cancellationToken = default)
    {
        var row = new SpecialtyRow { Name = request.Name!, IsActive = true };
        db.Specialties.Add(row);
        await db.SaveChangesAsync(cancellationToken);
        return new SpecialtyResponse(row.Id, row.Name, row.IsActive);
    }

    public async Task<Result<SpecialtyResponse>> UpdateAsync(int id, SpecialtyWriteRequest request, CancellationToken cancellationToken = default)
    {
        var row = await db.Specialties.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null) return CatalogErrors.SpecialtyNotFound;
        row.Name = request.Name!;
        await db.SaveChangesAsync(cancellationToken);
        return new SpecialtyResponse(row.Id, row.Name, row.IsActive);
    }

    async Task<Result> ISpecialtyRepository.DeactivateAsync(int id, CancellationToken cancellationToken)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        await LockSpecialtyAsync(id, cancellationToken);
        var row = await db.Specialties.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null) return CatalogErrors.SpecialtyNotFound;
        if (await HasFutureUnfinishedAppointmentForSpecialty(id, cancellationToken)) return CatalogErrors.SpecialtyFutureAppointmentsExist;
        row.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<ScheduleSlot>> AddSlotAsync(ScheduleSlot slot, CancellationToken cancellationToken = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var doctor = await db.Doctors.AsNoTracking().SingleOrDefaultAsync(x => x.Id == slot.DoctorId, cancellationToken);
        if (doctor is null) return DoctorErrors.NotAvailable;
        var specialty = await LockSpecialtyAsync(doctor.SpecialtyId, cancellationToken);
        await LockDoctorAsync(doctor.Id, cancellationToken);
        doctor = await db.Doctors.AsNoTracking().SingleOrDefaultAsync(x => x.Id == slot.DoctorId, cancellationToken);
        var specialtyActive = specialty is { IsActive: true };
        if (doctor is null || !doctor.IsActive || !specialtyActive) return DoctorErrors.NotAvailable;
        var existingRows = await db.ScheduleSlots.AsNoTracking()
            .Where(x => x.DoctorId == slot.DoctorId && x.Date == slot.Date).ToListAsync(cancellationToken);
        var existing = existingRows.Select(ToDomain).ToList();
        if (existing.Any(x => x.OverlapsWith(slot))) return SchedulingErrors.Overlap;
        var row = new ScheduleSlotRow
        {
            DoctorId = slot.DoctorId, Date = slot.Date, StartTime = slot.StartTime,
            EndTime = slot.EndTime, Status = (int)slot.Status
        };
        db.ScheduleSlots.Add(row);
        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return ToDomain(row);
    }

    public async Task<ScheduleSlot?> GetSlotByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var row = await db.ScheduleSlots.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return row is null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyList<ScheduleSlot>> GetSlotsByDoctorAndDateAsync(int doctorId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var rows = await db.ScheduleSlots.AsNoTracking().Where(x => x.DoctorId == doctorId && x.Date == date)
            .OrderBy(x => x.StartTime).ToListAsync(cancellationToken);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<Result<Appointment>> ReserveAsync(ScheduleSlot slot, Appointment appointment, CancellationToken cancellationToken = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var slotSnapshot = await db.ScheduleSlots.AsNoTracking().SingleOrDefaultAsync(x => x.Id == slot.Id, cancellationToken);
        if (slotSnapshot is null || slotSnapshot.Status != (int)ScheduleSlotStatus.Available) return SchedulingErrors.SlotNotAvailable;
        var doctor = await db.Doctors.AsNoTracking().SingleOrDefaultAsync(x => x.Id == slotSnapshot.DoctorId, cancellationToken);
        if (doctor is null) return SchedulingErrors.SlotNotAvailable;
        var specialty = await LockSpecialtyAsync(doctor.SpecialtyId, cancellationToken);
        await LockDoctorAsync(doctor.Id, cancellationToken);
        doctor = await db.Doctors.AsNoTracking().SingleOrDefaultAsync(x => x.Id == slotSnapshot.DoctorId, cancellationToken);
        var specialtyActive = specialty is { IsActive: true };
        if (doctor is null || !doctor.IsActive || !specialtyActive) return SchedulingErrors.SlotNotAvailable;
        if (!await db.Patients.AnyAsync(x => x.Id == appointment.PatientId, cancellationToken)) return PatientErrors.NotFound;
        var claimed = await db.ScheduleSlots.Where(x => x.Id == slot.Id && x.Status == (int)ScheduleSlotStatus.Available)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.Status, (int)ScheduleSlotStatus.Reserved), cancellationToken);
        if (claimed == 0) return SchedulingErrors.SlotNotAvailable;
        var appointmentRow = new AppointmentRow
        {
            ScheduleSlotId = slotSnapshot.Id, PatientId = appointment.PatientId,
            Reason = appointment.Reason, Status = (int)AppointmentStatus.Pending
        };
        db.Appointments.Add(appointmentRow);
        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return Appointment.Rehydrate(appointmentRow.Id, slotSnapshot.Id, appointmentRow.PatientId,
            appointmentRow.Reason, AppointmentStatus.Pending, null, null);
    }

    public async Task<Appointment?> GetAppointmentByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var row = await db.Appointments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null) return null;
        var attention = await db.Attentions.AsNoTracking().SingleOrDefaultAsync(x => x.AppointmentId == id, cancellationToken);
        return ToDomain(row, attention);
    }

    public async Task<Result<Appointment>> RegisterAttentionAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var row = await db.Appointments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == appointment.Id, cancellationToken);
        if (row is null || row.Status != (int)AppointmentStatus.Pending ||
            await db.Attentions.AnyAsync(x => x.AppointmentId == appointment.Id, cancellationToken))
            return AppointmentErrors.NotPending;
        var claimed = await db.Appointments.Where(x => x.Id == appointment.Id && x.Status == (int)AppointmentStatus.Pending)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.Status, (int)AppointmentStatus.Attended), cancellationToken);
        if (claimed == 0) return AppointmentErrors.NotPending;
        var attention = new AttentionRow
        {
            AppointmentId = row.Id, Diagnosis = appointment.Diagnosis!, Prescription = appointment.Prescription
        };
        db.Attentions.Add(attention);
        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return Appointment.Rehydrate(row.Id, row.ScheduleSlotId, row.PatientId, row.Reason,
            AppointmentStatus.Attended, attention.Diagnosis, attention.Prescription);
    }

    private async Task<bool> HasFutureUnfinishedAppointmentForDoctor(int doctorId, CancellationToken ct)
    {
        var today = clock.Today;
        var now = clock.CurrentTime;
        return await (from appointment in db.Appointments.AsNoTracking()
                      join slot in db.ScheduleSlots.AsNoTracking() on appointment.ScheduleSlotId equals slot.Id
                      where slot.DoctorId == doctorId && appointment.Status != (int)AppointmentStatus.Attended &&
                            appointment.Status != (int)AppointmentStatus.Cancelled &&
                            (slot.Date > today || (slot.Date == today && slot.StartTime > now))
                      select appointment.Id).AnyAsync(ct);
    }

    private Task<SpecialtyRow?> LockSpecialtyAsync(int id, CancellationToken ct) =>
        db.Specialties.FromSqlInterpolated($"SELECT * FROM [Specialties] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {id}")
            .AsNoTracking().SingleOrDefaultAsync(ct);

    private Task<DoctorRow?> LockDoctorAsync(int id, CancellationToken ct) =>
        db.Doctors.FromSqlInterpolated($"SELECT * FROM [Doctors] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {id}")
            .AsNoTracking().SingleOrDefaultAsync(ct);

    private async Task<bool> HasFutureUnfinishedAppointmentForSpecialty(int specialtyId, CancellationToken ct)
    {
        var today = clock.Today;
        var now = clock.CurrentTime;
        return await (from appointment in db.Appointments.AsNoTracking()
                      join slot in db.ScheduleSlots.AsNoTracking() on appointment.ScheduleSlotId equals slot.Id
                      join doctor in db.Doctors.AsNoTracking() on slot.DoctorId equals doctor.Id
                      where doctor.SpecialtyId == specialtyId && appointment.Status != (int)AppointmentStatus.Attended &&
                            appointment.Status != (int)AppointmentStatus.Cancelled &&
                            (slot.Date > today || (slot.Date == today && slot.StartTime > now))
                      select appointment.Id).AnyAsync(ct);
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 };

    private static Patient ToDomain(PatientRow row) =>
        Patient.Rehydrate(row.Id, row.Dni, row.FirstName, row.LastName, row.Phone, row.Email);

    private static Doctor ToDomain(DoctorRow row, SpecialtyRow specialty, bool? active = null) =>
        Doctor.Rehydrate(row.Id, row.LicenseNumber, row.FirstName, row.LastName, specialty.Name, active ?? row.IsActive);

    private static DoctorCatalogResponse ToCatalog(DoctorRow row, SpecialtyRow specialty) =>
        new(row.Id, row.LicenseNumber, row.FirstName, row.LastName, specialty.Id, specialty.Name, row.IsActive);

    private static ScheduleSlot ToDomain(ScheduleSlotRow row) =>
        ScheduleSlot.Rehydrate(row.Id, row.DoctorId, row.Date, row.StartTime, row.EndTime, (ScheduleSlotStatus)row.Status);

    private static Appointment ToDomain(AppointmentRow row, AttentionRow? attention) =>
        Appointment.Rehydrate(row.Id, row.ScheduleSlotId, row.PatientId, row.Reason,
            (AppointmentStatus)row.Status, attention?.Diagnosis, attention?.Prescription);
}
