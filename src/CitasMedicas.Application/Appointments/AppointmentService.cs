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
}
