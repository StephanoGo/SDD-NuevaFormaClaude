using CitasMedicas.Application.Abstractions.Persistence;
using CitasMedicas.Application.Catalogs;
using CitasMedicas.Application.Common;
using CitasMedicas.Domain.Appointments;
using CitasMedicas.Domain.Doctors;
using CitasMedicas.Domain.Patients;
using CitasMedicas.Domain.Scheduling;
using CitasMedicas.Infrastructure.Persistence.SqlServer;
using CitasMedicas.Tests.Support;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CitasMedicas.Tests.Infrastructure;

public sealed class SqlServerDatabaseFixture : IAsyncLifetime
{
    private readonly string _baseConnection = Environment.GetEnvironmentVariable("CLINIC_TEST_CONNECTION_STRING")
        ?? "Server=localhost;Database=master;Integrated Security=True;TrustServerCertificate=True";
    private string _databaseName = "";

    public string ConnectionString { get; private set; } = "";
    public bool FreshSchemaStartedEmpty { get; private set; }

    public async Task InitializeAsync()
    {
        _databaseName = $"ClinicFeatureTests_{Guid.NewGuid():N}";
        var builder = new SqlConnectionStringBuilder(_baseConnection) { InitialCatalog = "master" };
        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE [{_databaseName}]";
        await command.ExecuteNonQueryAsync();
        builder.InitialCatalog = _databaseName;
        ConnectionString = builder.ConnectionString;
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        FreshSchemaStartedEmpty = await context.Patients.CountAsync() == 0 &&
            await context.Doctors.CountAsync() == 0 && await context.Specialties.CountAsync() == 0 &&
            await context.ScheduleSlots.CountAsync() == 0 && await context.Appointments.CountAsync() == 0 &&
            await context.Attentions.CountAsync() == 0;
    }

    public ClinicDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ClinicDbContext>().UseSqlServer(ConnectionString).Options);

    public SqlServerClinicRepository CreateRepository(out ClinicDbContext context)
    {
        context = CreateContext();
        return new SqlServerClinicRepository(context, new ClinicClock(FixedTimeProvider.CreateForLima(TestBuilders.ReferenceNow)));
    }

    public async Task DisposeAsync()
    {
        if (string.IsNullOrWhiteSpace(_databaseName)) return;
        SqlConnection.ClearAllPools();
        var builder = new SqlConnectionStringBuilder(_baseConnection) { InitialCatalog = "master" };
        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_databaseName}]";
        await command.ExecuteNonQueryAsync();
    }
}

public sealed class SqlServerPersistenceTests(SqlServerDatabaseFixture fixture) : IClassFixture<SqlServerDatabaseFixture>
{
    [Fact]
    [Trait("RF", "RF-PER-03")]
    [Trait("RF", "RF-PER-04")]
    [Trait("RF", "RF-PER-07")]
    public async Task MigrateAsync_OnFreshDatabase_CreatesEmptySchemaWithoutUserOrRoleTables()
    {
        fixture.FreshSchemaStartedEmpty.Should().BeTrue();
        await using var db = fixture.CreateContext();
        var tableNames = await ReadTableNamesAsync(db);
        tableNames.Should().NotContain("Users").And.NotContain("Roles");
    }

    [Fact]
    [Trait("RF", "RF-PER-01")]
    [Trait("RF", "RF-PER-02")]
    [Trait("RF", "RF-PER-06")]
    [Trait("RF", "RF-PER-11")]
    public async Task Repository_RoundTripAndSecondMigration_PreservesPatientAndStoresSingleAttention()
    {
        var repo = fixture.CreateRepository(out var repoContext);
        await using var disposeRepoContext = repoContext;
        var specialtyRepo = (ISpecialtyRepository)repo;
        var specialty = (await specialtyRepo.AddAsync(new SpecialtyWriteRequest("Test specialty"))).Value;
        var doctor = (await repo.AddAsync(new DoctorWriteRequest("TEST-LIC", "Test", "Doctor", specialty.Id))).Value;
        var patient = Patient.Create(NewDni(), "Test", "Patient", "555", null).Value;
        var storedPatient = (await repo.AddAsync(patient)).Value;
        var doctorDomain = await ((IDoctorRepository)repo).GetByIdAsync(doctor.Id);
        var slot = ScheduleSlot.Create(doctorDomain!, new DateOnly(2026, 10, 16),
            TimeRange.Create(new TimeOnly(9, 0), new TimeOnly(9, 30)).Value,
            TestBuilders.ReferenceNow, []).Value;
        var storedSlot = (await repo.AddSlotAsync(slot)).Value;
        var appointment = Appointment.Rehydrate(0, storedSlot.Id, storedPatient.Id, "Test visit", AppointmentStatus.Pending, null, null);
        var storedAppointment = (await repo.ReserveAsync(storedSlot, appointment)).Value;
        storedAppointment.RegisterAttention("Test diagnosis", "Test prescription").IsSuccess.Should().BeTrue();
        (await repo.RegisterAttentionAsync(storedAppointment)).IsSuccess.Should().BeTrue();

        await using (var db = fixture.CreateContext()) await db.Database.MigrateAsync();
        var reloaded = await repo.GetAppointmentByIdAsync(storedAppointment.Id);
        reloaded!.Diagnosis.Should().Be("Test diagnosis");
        reloaded.Prescription.Should().Be("Test prescription");
        reloaded.Status.Should().Be(AppointmentStatus.Attended);
        (await repo.RegisterAttentionAsync(reloaded)).Error.Code.Should().Be("APPOINTMENT_NOT_PENDING");
        (await ((IPatientRepository)repo).GetByIdAsync(storedPatient.Id))!.Dni.Value.Should().Be(patient.Dni.Value);
    }

    [Fact]
    [Trait("RF", "RF-PER-02")]
    [Trait("RF", "RF-PER-09")]
    [Trait("RF", "RF-PER-12")]
    public async Task InactiveSpecialty_BlocksFutureAppointmentsAndPreventsNewSchedulingAndReservation()
    {
        var repo = fixture.CreateRepository(out var repoContext);
        await using var disposeRepoContext = repoContext;
        var specialtyRepo = (ISpecialtyRepository)repo;
        var specialty = (await specialtyRepo.AddAsync(new SpecialtyWriteRequest("Inactive test"))).Value;
        var doctor = (await repo.AddAsync(new DoctorWriteRequest("TEST-INACTIVE", "Test", "Doctor", specialty.Id))).Value;
        var doctorDomain = await ((IDoctorRepository)repo).GetByIdAsync(doctor.Id);
        var slot = ScheduleSlot.Create(doctorDomain!, new DateOnly(2026, 10, 16),
            TimeRange.Create(new TimeOnly(9, 0), new TimeOnly(9, 30)).Value,
            TestBuilders.ReferenceNow, []).Value;
        var storedSlot = (await repo.AddSlotAsync(slot)).Value;
        var deactivateBlocked = await specialtyRepo.DeactivateAsync(specialty.Id);
        deactivateBlocked.IsSuccess.Should().BeTrue();

        var inactiveDoctor = await ((IDoctorRepository)repo).GetByIdAsync(doctor.Id);
        inactiveDoctor!.IsActive.Should().BeFalse();
        var newSlot = ScheduleSlot.Create(inactiveDoctor, new DateOnly(2026, 10, 17),
            TimeRange.Create(new TimeOnly(10, 0), new TimeOnly(10, 30)).Value,
            TestBuilders.ReferenceNow, []);
        newSlot.Error.Code.Should().Be("DOCTOR_NOT_AVAILABLE");

        var patient = (await repo.AddAsync(Patient.Create(NewDni(), "Test", "Patient", "555", null).Value)).Value;
        var appointment = Appointment.Rehydrate(0, storedSlot.Id, patient.Id, "Visit", AppointmentStatus.Pending, null, null);
        (await repo.ReserveAsync(storedSlot, appointment)).Error.Code.Should().Be("SLOT_NOT_AVAILABLE");
    }

    [Fact]
    [Trait("RF", "RF-PER-05")]
    [Trait("RF", "RF-PER-09")]
    [Trait("RF", "RF-PER-10")]
    public async Task CatalogService_ValidatesInputsFiltersInactiveAndBlocksDeactivationUntilFutureAppointmentsAreFinished()
    {
        var repo = fixture.CreateRepository(out var repoContext);
        await using var disposeRepoContext = repoContext;
        var service = new CatalogService(repo, repo);
        (await service.AddSpecialtyAsync(new SpecialtyWriteRequest("  "))).Error.Code.Should().Be("SPECIALTY_NAME_REQUIRED");
        (await service.AddDoctorAsync(new DoctorWriteRequest(" ", "", "", null))).Error.Code.Should().Be("DOCTOR_LICENSE_REQUIRED");

        var specialty = (await service.AddSpecialtyAsync(new SpecialtyWriteRequest("Future test"))).Value;
        var doctor = (await service.AddDoctorAsync(new DoctorWriteRequest("FUTURE-LIC", "Test", "Doctor", specialty.Id))).Value;
        var patient = (await repo.AddAsync(Patient.Create(NewDni(), "Test", "Patient", "555", null).Value)).Value;
        var doctorDomain = await ((IDoctorRepository)repo).GetByIdAsync(doctor.Id);
        var slot = ScheduleSlot.Create(doctorDomain!, new DateOnly(2026, 10, 16),
            TimeRange.Create(new TimeOnly(9, 0), new TimeOnly(9, 30)).Value,
            TestBuilders.ReferenceNow, []).Value;
        var storedSlot = (await repo.AddSlotAsync(slot)).Value;
        var appointment = Appointment.Rehydrate(0, storedSlot.Id, patient.Id, "Future visit", AppointmentStatus.Pending, null, null);
        var storedAppointment = (await repo.ReserveAsync(storedSlot, appointment)).Value;

        (await service.DeactivateDoctorAsync(doctor.Id)).Error.Code.Should().Be("DOCTOR_FUTURE_APPOINTMENTS_EXIST");
        (await service.DeactivateSpecialtyAsync(specialty.Id)).Error.Code.Should().Be("SPECIALTY_FUTURE_APPOINTMENTS_EXIST");
        (await service.ListDoctorsAsync(false)).Should().ContainSingle(x => x.Id == doctor.Id);
        (await service.ListSpecialtiesAsync(false)).Should().ContainSingle(x => x.Id == specialty.Id);

        storedAppointment.RegisterAttention("Finished", null);
        (await repo.RegisterAttentionAsync(storedAppointment)).IsSuccess.Should().BeTrue();
        (await service.DeactivateDoctorAsync(doctor.Id)).IsSuccess.Should().BeTrue();
        (await service.DeactivateSpecialtyAsync(specialty.Id)).IsSuccess.Should().BeTrue();
        (await service.ListDoctorsAsync(false)).Should().NotContain(x => x.Id == doctor.Id);
        (await service.ListDoctorsAsync(true)).Should().ContainSingle(x => x.Id == doctor.Id && !x.IsActive);
        (await service.ListSpecialtiesAsync(false)).Should().NotContain(x => x.Id == specialty.Id);
        (await service.ListSpecialtiesAsync(true)).Should().ContainSingle(x => x.Id == specialty.Id && !x.IsActive);
    }

    [Fact]
    [Trait("RF", "RF-PER-02")]
    public async Task ConcurrentReservations_OnSqlServer_ConfirmAtMostOneReservation()
    {
        var setup = fixture.CreateRepository(out var setupContext);
        await using var disposeSetup = setupContext;
        var specialty = (await ((ISpecialtyRepository)setup).AddAsync(new SpecialtyWriteRequest("Concurrency test"))).Value;
        var doctor = (await ((IDoctorRepository)setup).AddAsync(new DoctorWriteRequest("CONCURRENT-LIC", "Test", "Doctor", specialty.Id))).Value;
        var patientIds = new List<int>();
        for (var i = 0; i < 12; i++)
            patientIds.Add((await setup.AddAsync(Patient.Create(NewDni(), "Concurrent", $"Patient{i}", "555", null).Value)).Value.Id);
        var doctorDomain = await ((IDoctorRepository)setup).GetByIdAsync(doctor.Id);
        var slot = ScheduleSlot.Create(doctorDomain!, new DateOnly(2026, 10, 17),
            TimeRange.Create(new TimeOnly(9, 0), new TimeOnly(9, 30)).Value,
            TestBuilders.ReferenceNow, []).Value;
        var storedSlot = (await setup.AddSlotAsync(slot)).Value;

        var attempts = patientIds.Select(async patientId =>
        {
            await using var context = fixture.CreateContext();
            var repository = new SqlServerClinicRepository(context,
                new ClinicClock(FixedTimeProvider.CreateForLima(TestBuilders.ReferenceNow)));
            var candidate = Appointment.Rehydrate(0, storedSlot.Id, patientId, "Concurrent", AppointmentStatus.Pending, null, null);
            return await repository.ReserveAsync(storedSlot, candidate);
        });
        var results = await Task.WhenAll(attempts);
        results.Count(x => x.IsSuccess).Should().Be(1);
        results.Count(x => x.IsFailure && x.Error.Code == "SLOT_NOT_AVAILABLE").Should().Be(patientIds.Count - 1);
    }

    [Fact]
    [Trait("RF", "RF-PER-02")]
    public async Task AddSlot_OnSqlServer_RejectsOverlapAndAllowsContiguousIntervals()
    {
        var repo = fixture.CreateRepository(out var repoContext);
        await using var disposeRepoContext = repoContext;
        var specialty = (await ((ISpecialtyRepository)repo).AddAsync(new SpecialtyWriteRequest("Overlap test"))).Value;
        var doctor = (await ((IDoctorRepository)repo).AddAsync(new DoctorWriteRequest("OVERLAP-LIC", "Test", "Doctor", specialty.Id))).Value;
        var doctorDomain = await ((IDoctorRepository)repo).GetByIdAsync(doctor.Id);
        var date = new DateOnly(2026, 10, 16);

        var first = ScheduleSlot.Create(doctorDomain!, date,
            TimeRange.Create(new TimeOnly(9, 0), new TimeOnly(9, 30)).Value, TestBuilders.ReferenceNow, []).Value;
        (await repo.AddSlotAsync(first)).IsSuccess.Should().BeTrue();
        var overlap = ScheduleSlot.Create(doctorDomain!, date,
            TimeRange.Create(new TimeOnly(9, 15), new TimeOnly(9, 45)).Value, TestBuilders.ReferenceNow, []).Value;
        (await repo.AddSlotAsync(overlap)).Error.Code.Should().Be("SLOT_OVERLAP");
        var contiguous = ScheduleSlot.Create(doctorDomain!, date,
            TimeRange.Create(new TimeOnly(9, 30), new TimeOnly(10, 0)).Value, TestBuilders.ReferenceNow, []).Value;
        (await repo.AddSlotAsync(contiguous)).IsSuccess.Should().BeTrue();
    }

    [Fact]
    [Trait("RF", "RF-PER-02")]
    [Trait("RF", "RF-PER-06")]
    [Trait("RF", "RF-PER-11")]
    public async Task ConcurrentAttentionRegistration_OnSqlServer_StoresExactlyOneAttention()
    {
        var setup = fixture.CreateRepository(out var setupContext);
        await using var disposeSetup = setupContext;
        var specialty = (await ((ISpecialtyRepository)setup).AddAsync(new SpecialtyWriteRequest("Attention race"))).Value;
        var doctor = (await ((IDoctorRepository)setup).AddAsync(new DoctorWriteRequest("ATTENTION-LIC", "Test", "Doctor", specialty.Id))).Value;
        var patient = (await setup.AddAsync(Patient.Create(NewDni(), "Attention", "Patient", "555", null).Value)).Value;
        var doctorDomain = await ((IDoctorRepository)setup).GetByIdAsync(doctor.Id);
        var slot = ScheduleSlot.Create(doctorDomain!, new DateOnly(2026, 10, 16),
            TimeRange.Create(new TimeOnly(9, 0), new TimeOnly(9, 30)).Value, TestBuilders.ReferenceNow, []).Value;
        var storedSlot = (await setup.AddSlotAsync(slot)).Value;
        var candidate = Appointment.Rehydrate(0, storedSlot.Id, patient.Id, "Attention race", AppointmentStatus.Pending, null, null);
        var storedAppointment = (await setup.ReserveAsync(storedSlot, candidate)).Value;
        var attempts = Enumerable.Range(0, 10).Select(async _ =>
        {
            await using var context = fixture.CreateContext();
            var repository = new SqlServerClinicRepository(context,
                new ClinicClock(FixedTimeProvider.CreateForLima(TestBuilders.ReferenceNow)));
            var attended = Appointment.Rehydrate(storedAppointment.Id, storedAppointment.ScheduleSlotId,
                storedAppointment.PatientId, storedAppointment.Reason, AppointmentStatus.Attended,
                "Concurrent diagnosis", "Concurrent prescription");
            return await repository.RegisterAttentionAsync(attended);
        });
        var results = await Task.WhenAll(attempts);
        results.Count(x => x.IsSuccess).Should().Be(1);
        results.Count(x => x.IsFailure && x.Error.Code == "APPOINTMENT_NOT_PENDING").Should().Be(9);
        await using var verify = fixture.CreateContext();
        (await verify.Attentions.CountAsync(x => x.AppointmentId == storedAppointment.Id)).Should().Be(1);
    }

    private static async Task<HashSet<string>> ReadTableNamesAsync(ClinicDbContext db)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";
        if (command.Connection!.State != System.Data.ConnectionState.Open) await command.Connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) names.Add(reader.GetString(0));
        return names;
    }

    private static int _nextTestDni = 20_000_000;
    private static string NewDni() => Interlocked.Increment(ref _nextTestDni).ToString();
}
