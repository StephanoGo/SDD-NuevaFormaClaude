using CitasMedicas.Application.Abstractions.Persistence;
using CitasMedicas.Application.Common;
using CitasMedicas.Domain.Appointments;
using CitasMedicas.Domain.Common;
using CitasMedicas.Domain.Doctors;
using CitasMedicas.Domain.Patients;
using CitasMedicas.Domain.Scheduling;

namespace CitasMedicas.Application.Appointments;

public sealed class AppointmentService
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly ClinicClock _clock;

    public AppointmentService(
        IAppointmentRepository appointmentRepository,
        IPatientRepository patientRepository,
        IDoctorRepository doctorRepository,
        ClinicClock clock)
    {
        _appointmentRepository = appointmentRepository;
        _patientRepository = patientRepository;
        _doctorRepository = doctorRepository;
        _clock = clock;
    }

    public async Task<Result<AppointmentResponse>> ReserveAsync(
        int scheduleSlotId,
        int patientId,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var slot = await _appointmentRepository.GetSlotByIdAsync(scheduleSlotId, cancellationToken);
        if (slot is null)
        {
            return SchedulingErrors.SlotNotFound;
        }

        var patient = await _patientRepository.GetByIdAsync(patientId, cancellationToken);
        if (patient is null)
        {
            return PatientErrors.NotFound;
        }

        var doctor = await _doctorRepository.GetByIdAsync(slot.DoctorId, cancellationToken);
        if (doctor is null)
        {
            return SchedulingErrors.SlotNotAvailable;
        }

        var reserveResult = slot.Reserve(doctor, patient, reason, _clock.Now);
        if (reserveResult.IsFailure)
        {
            return reserveResult.Error;
        }

        var repoResult = await _appointmentRepository.ReserveAsync(slot, reserveResult.Value, cancellationToken);
        if (repoResult.IsFailure)
        {
            return repoResult.Error;
        }

        var createdAppointment = repoResult.Value;
        return new AppointmentResponse(
            createdAppointment.Id,
            createdAppointment.Status.ToString(),
            createdAppointment.Reason,
            createdAppointment.Diagnosis,
            createdAppointment.Prescription,
            new PatientSummaryResponse(patient.Id, patient.Dni.Value, patient.FullName),
            new DoctorSummaryResponse(doctor.Id, doctor.FullName, doctor.Specialty),
            new SlotSummaryResponse(slot.Id, slot.Date.ToString("yyyy-MM-dd"), slot.StartTime.ToString("HH:mm"), slot.EndTime.ToString("HH:mm"))
        );
    }

    public async Task<Result<AppointmentResponse>> RegisterAttentionAsync(
        int appointmentId,
        string? diagnosis,
        string? prescription,
        CancellationToken cancellationToken = default)
    {
        var appointment = await _appointmentRepository.GetAppointmentByIdAsync(appointmentId, cancellationToken);
        if (appointment is null)
        {
            return AppointmentErrors.NotFound;
        }

        var domainResult = appointment.RegisterAttention(diagnosis, prescription);
        if (domainResult.IsFailure)
        {
            return domainResult.Error;
        }

        var repoResult = await _appointmentRepository.RegisterAttentionAsync(appointment, cancellationToken);
        if (repoResult.IsFailure)
        {
            return repoResult.Error;
        }

        var updatedAppointment = repoResult.Value;
        var slot = await _appointmentRepository.GetSlotByIdAsync(updatedAppointment.ScheduleSlotId, cancellationToken);
        var patient = await _patientRepository.GetByIdAsync(updatedAppointment.PatientId, cancellationToken);
        var doctor = slot is not null ? await _doctorRepository.GetByIdAsync(slot.DoctorId, cancellationToken) : null;

        if (slot is null || patient is null || doctor is null)
        {
            return AppointmentErrors.NotFound;
        }

        return new AppointmentResponse(
            updatedAppointment.Id,
            updatedAppointment.Status.ToString(),
            updatedAppointment.Reason,
            updatedAppointment.Diagnosis,
            updatedAppointment.Prescription,
            new PatientSummaryResponse(patient.Id, patient.Dni.Value, patient.FullName),
            new DoctorSummaryResponse(doctor.Id, doctor.FullName, doctor.Specialty),
            new SlotSummaryResponse(slot.Id, slot.Date.ToString("yyyy-MM-dd"), slot.StartTime.ToString("HH:mm"), slot.EndTime.ToString("HH:mm"))
        );
    }

    public async Task<Result<AppointmentResponse>> GetByIdAsync(int appointmentId, CancellationToken cancellationToken = default)
    {
        var appointment = await _appointmentRepository.GetAppointmentByIdAsync(appointmentId, cancellationToken);
        if (appointment is null)
        {
            return AppointmentErrors.NotFound;
        }

        var slot = await _appointmentRepository.GetSlotByIdAsync(appointment.ScheduleSlotId, cancellationToken);
        var patient = await _patientRepository.GetByIdAsync(appointment.PatientId, cancellationToken);
        var doctor = slot is not null ? await _doctorRepository.GetByIdAsync(slot.DoctorId, cancellationToken) : null;

        if (slot is null || patient is null || doctor is null)
        {
            return AppointmentErrors.NotFound;
        }

        return new AppointmentResponse(
            appointment.Id,
            appointment.Status.ToString(),
            appointment.Reason,
            appointment.Diagnosis,
            appointment.Prescription,
            new PatientSummaryResponse(patient.Id, patient.Dni.Value, patient.FullName),
            new DoctorSummaryResponse(doctor.Id, doctor.FullName, doctor.Specialty),
            new SlotSummaryResponse(slot.Id, slot.Date.ToString("yyyy-MM-dd"), slot.StartTime.ToString("HH:mm"), slot.EndTime.ToString("HH:mm"))
        );
    }

    public async Task<Result<IReadOnlyList<AppointmentResponse>>> ListAsync(
        int? patientId, int? doctorId, DateOnly? date, CancellationToken cancellationToken = default)
    {
        if (!patientId.HasValue && !doctorId.HasValue && !date.HasValue)
            return ApiFilterErrors.Required;
        if (patientId is <= 0 || doctorId is <= 0)
            return ApiFilterErrors.Invalid;
        if (patientId.HasValue && await _patientRepository.GetByIdAsync(patientId.Value, cancellationToken) is null)
            return PatientErrors.NotFound;

        var appointments = await _appointmentRepository.ListAppointmentsAsync(patientId, doctorId, date, cancellationToken);
        var result = new List<AppointmentResponse>(appointments.Count);
        foreach (var appointment in appointments)
        {
            var item = await BuildResponseAsync(appointment, cancellationToken);
            if (item is not null) result.Add(item);
        }
        return Result<IReadOnlyList<AppointmentResponse>>.Success(result);
    }

    public async Task<Result<AppointmentResponse>> CancelAsync(int appointmentId, CancellationToken cancellationToken = default)
    {
        var appointment = await _appointmentRepository.GetAppointmentByIdAsync(appointmentId, cancellationToken);
        if (appointment is null) return AppointmentErrors.NotFound;
        var cancelled = appointment.Cancel();
        if (cancelled.IsFailure) return cancelled.Error;
        var persisted = await _appointmentRepository.CancelAsync(appointment, cancellationToken);
        if (persisted.IsFailure) return persisted.Error;
        var response = await BuildResponseAsync(persisted.Value, cancellationToken);
        return response is null ? AppointmentErrors.NotFound : response;
    }

    public async Task<Result<IReadOnlyList<ClinicalHistoryEntryResponse>>> GetClinicalHistoryAsync(
        int patientId, CancellationToken cancellationToken = default)
    {
        if (await _patientRepository.GetByIdAsync(patientId, cancellationToken) is null)
            return PatientErrors.NotFound;
        var appointments = await _appointmentRepository.ListAppointmentsAsync(patientId, null, null, cancellationToken);
        var result = new List<ClinicalHistoryEntryResponse>();
        foreach (var appointment in appointments.Where(x => x.Status == AppointmentStatus.Attended))
        {
            var slot = await _appointmentRepository.GetSlotByIdAsync(appointment.ScheduleSlotId, cancellationToken);
            var doctor = slot is null ? null : await _doctorRepository.GetByIdAsync(slot.DoctorId, cancellationToken);
            if (slot is null || doctor is null || appointment.Diagnosis is null) continue;
            result.Add(new ClinicalHistoryEntryResponse(appointment.Id, appointment.Reason, appointment.Diagnosis,
                appointment.Prescription, new DoctorSummaryResponse(doctor.Id, doctor.FullName, doctor.Specialty),
                new SlotSummaryResponse(slot.Id, slot.Date.ToString("yyyy-MM-dd"), slot.StartTime.ToString("HH:mm"), slot.EndTime.ToString("HH:mm"))));
        }
        return Result<IReadOnlyList<ClinicalHistoryEntryResponse>>.Success(result
            .OrderByDescending(x => x.Slot.Date, StringComparer.Ordinal)
            .ThenByDescending(x => x.Slot.StartTime, StringComparer.Ordinal)
            .ThenByDescending(x => x.AppointmentId).ToList());
    }

    private async Task<AppointmentResponse?> BuildResponseAsync(Appointment appointment, CancellationToken cancellationToken)
    {
        var slot = await _appointmentRepository.GetSlotByIdAsync(appointment.ScheduleSlotId, cancellationToken);
        var patient = await _patientRepository.GetByIdAsync(appointment.PatientId, cancellationToken);
        var doctor = slot is null ? null : await _doctorRepository.GetByIdAsync(slot.DoctorId, cancellationToken);
        if (slot is null || patient is null || doctor is null) return null;
        return new AppointmentResponse(appointment.Id, appointment.Status.ToString(), appointment.Reason,
            appointment.Diagnosis, appointment.Prescription,
            new PatientSummaryResponse(patient.Id, patient.Dni.Value, patient.FullName),
            new DoctorSummaryResponse(doctor.Id, doctor.FullName, doctor.Specialty),
            new SlotSummaryResponse(slot.Id, slot.Date.ToString("yyyy-MM-dd"), slot.StartTime.ToString("HH:mm"), slot.EndTime.ToString("HH:mm")));
    }
}

internal static class ApiFilterErrors
{
    public static readonly Error Required = Error.Validation("VALIDATION_FORMAT", "Se requiere al menos un filtro de búsqueda.");
    public static readonly Error Invalid = Error.Validation("VALIDATION_FORMAT", "Los identificadores deben ser enteros positivos.");
}

public sealed class ClinicalHistoryService(AppointmentService appointments)
{
    public Task<Result<IReadOnlyList<ClinicalHistoryEntryResponse>>> GetByPatientAsync(
        int patientId, CancellationToken cancellationToken = default) =>
        appointments.GetClinicalHistoryAsync(patientId, cancellationToken);
}
