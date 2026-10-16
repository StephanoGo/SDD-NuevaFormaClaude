using CitasMedicas.Application.Abstractions.Persistence;
using CitasMedicas.Application.Catalogs;
using CitasMedicas.Application.Common;
using CitasMedicas.Domain.Appointments;
using CitasMedicas.Domain.Doctors;
using CitasMedicas.Domain.Common;
using CitasMedicas.Domain.Patients;
using CitasMedicas.Domain.Scheduling;
using CitasMedicas.Infrastructure.Persistence.SqlServer;
using CitasMedicas.Tests.Support;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace CitasMedicas.Tests.Infrastructure;

public sealed class SqlServerDatabaseFixture : IAsyncLifetime
{
    private const string RequiredDatabaseName = "BD_MedicinaGen_Tests";
    private readonly string _runId = Guid.NewGuid().ToString("N");
    private int _nextDni = RandomNumberGenerator.GetInt32(10_000_000, 99_999_000);

    public string ConnectionString { get; private set; } = "";
    public bool FreshSchemaStartedEmpty { get; private set; }
    public bool InitialMigrationInsertedNoSeed { get; private set; }
    public string? BlockedReason { get; private set; }

    public async Task InitializeAsync()
    {
        var configuredConnection = Environment.GetEnvironmentVariable("CLINIC_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(configuredConnection))
        {
            BlockedReason = "Blocked: CLINIC_TEST_CONNECTION_STRING is not configured; SQL tests were skipped.";
            return;
        }

        if (!SqlTestCatalogPolicy.TryParseApprovedConnectionString(configuredConnection, out var builder))
        {
            BlockedReason = $"Blocked: SQL tests require a valid connection to Server=localhost and the pre-provisioned {RequiredDatabaseName} database; the configured connection was rejected.";
            return;
        }

        ConnectionString = builder!.ConnectionString;
        try
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            var (userTableCount, historyExists, historyInDbo, historyOutsideDbo) = await InspectCatalogAsync(connection);
            FreshSchemaStartedEmpty = SqlTestCatalogPolicy.IsFreshEmptyCatalog(userTableCount, historyExists);

            if (FreshSchemaStartedEmpty)
            {
                // The explicitly configured database already exists; only its initial schema may be created here.
            }
            else
            {
                if (!historyExists || !historyInDbo || historyOutsideDbo || userTableCount == 0)
                {
                    BlockedReason = "Blocked: the test catalog has user tables without a valid dbo EF migration history, or an EF migration history table outside dbo; migrations were not applied.";
                    return;
                }

                var recordedMigrationIds = await ReadMigrationIdsAsync(connection);
                using var migrationContext = CreateContext();
                var recognizedMigrationIds = new HashSet<string>(migrationContext.Database.GetMigrations(), StringComparer.Ordinal);
                if (SqlTestCatalogPolicy.ShouldBlockBeforeMigration(
                        inspectionSucceeded: true, userTableCount, historyExists, historyInDbo, historyOutsideDbo,
                        recordedMigrationIds, recognizedMigrationIds))
                {
                    BlockedReason = "Blocked: the test catalog has an empty or unknown EF migration history; migrations were not applied.";
                    return;
                }
            }
        }
        catch (Exception)
        {
            ConnectionString = "";
            BlockedReason = $"Blocked: the configured {RequiredDatabaseName} database is unavailable or its ownership/state could not be confirmed; SQL tests were skipped.";
            return;
        }

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        InitialMigrationInsertedNoSeed = await context.Patients.CountAsync() == 0 &&
            await context.Doctors.CountAsync() == 0 && await context.Specialties.CountAsync() == 0 &&
            await context.ScheduleSlots.CountAsync() == 0 && await context.Appointments.CountAsync() == 0 &&
            await context.Attentions.CountAsync() == 0;
    }

    private static async Task<(int UserTableCount, bool HistoryExists, bool HistoryInDbo, bool HistoryOutsideDbo)> InspectCatalogAsync(SqlConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                (SELECT COUNT(*)
                 FROM sys.tables AS t
                 INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
                 WHERE t.is_ms_shipped = 0
                   AND NOT (s.name = N'dbo' AND t.name = N'__EFMigrationsHistory')),
                CASE WHEN EXISTS (
                    SELECT 1 FROM sys.tables AS t
                    WHERE t.name = N'__EFMigrationsHistory'
                ) THEN 1 ELSE 0 END,
                CASE WHEN EXISTS (
                    SELECT 1 FROM sys.tables AS t
                    INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
                    WHERE s.name = N'dbo' AND t.name = N'__EFMigrationsHistory'
                ) THEN 1 ELSE 0 END,
                CASE WHEN EXISTS (
                    SELECT 1 FROM sys.tables AS t
                    INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
                    WHERE s.name <> N'dbo' AND t.name = N'__EFMigrationsHistory'
                ) THEN 1 ELSE 0 END
            """;
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) throw new InvalidOperationException("Could not inspect the SQL test catalog.");
        return (reader.GetInt32(0), reader.GetInt32(1) == 1, reader.GetInt32(2) == 1, reader.GetInt32(3) == 1);
    }

    private static async Task<List<string>> ReadMigrationIdsAsync(SqlConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT MigrationId FROM dbo.__EFMigrationsHistory";
        var migrationIds = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) migrationIds.Add(reader.GetString(0));
        return migrationIds;
    }

    public void EnsureAvailable()
    {
        if (BlockedReason is not null) throw Xunit.Sdk.SkipException.ForSkip(BlockedReason);
    }

    public string Unique(string value) => $"{value}-{_runId}";

    public async Task<string> NewDniAsync()
    {
        await using var context = CreateContext();
        while (true)
        {
            var candidate = Interlocked.Increment(ref _nextDni).ToString();
            if (!await context.Patients.AnyAsync(patient => patient.Dni == candidate)) return candidate;
        }
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
        await Task.CompletedTask;
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
        fixture.EnsureAvailable();
        if (!fixture.FreshSchemaStartedEmpty)
            // A trusted existing EF history is valid for other persistence tests, but this
            // fresh-schema-only acceptance scenario does not apply to that catalog.
            throw Xunit.Sdk.SkipException.ForSkip("Blocked: AC-PER-04 requires a person-provisioned, pre-existing empty test database.");
        fixture.InitialMigrationInsertedNoSeed.Should().BeTrue();
        await using var db = fixture.CreateContext();
        var tableNames = await ReadTableNamesAsync(db);
        tableNames.Should().Contain(new[]
        {
            "Patients", "Specialties", "Doctors", "ScheduleSlots", "Appointments", "Attentions",
            "__EFMigrationsHistory"
        });
        tableNames.Should().NotContain("Users").And.NotContain("Roles");
    }

    [Fact]
    [Trait("RF", "RF-PER-01")]
    [Trait("RF", "RF-PER-02")]
    [Trait("RF", "RF-PER-06")]
    [Trait("RF", "RF-PER-11")]
    public async Task Repository_RoundTrip_PreservesPatientAndStoresSingleAttention()
    {
        fixture.EnsureAvailable();
        var repo = fixture.CreateRepository(out var repoContext);
        await using var disposeRepoContext = repoContext;
        var specialtyRepo = (ISpecialtyRepository)repo;
        var specialty = (await specialtyRepo.AddAsync(new SpecialtyWriteRequest(fixture.Unique("Test specialty")))).Value;
        var doctor = (await repo.AddAsync(new DoctorWriteRequest(fixture.Unique("TEST-LIC"), fixture.Unique("Test"), fixture.Unique("Doctor"), specialty.Id))).Value;
        var patient = Patient.Create(await fixture.NewDniAsync(), fixture.Unique("Test"), fixture.Unique("Patient"), fixture.Unique("555"), null).Value;
        var storedPatient = (await repo.AddAsync(patient)).Value;
        var doctorDomain = await ((IDoctorRepository)repo).GetByIdAsync(doctor.Id);
        var slot = ScheduleSlot.Create(doctorDomain!, new DateOnly(2026, 10, 16),
            TimeRange.Create(new TimeOnly(9, 0), new TimeOnly(9, 30)).Value,
            TestBuilders.ReferenceNow, []).Value;
        var storedSlot = (await repo.AddSlotAsync(slot)).Value;
        var appointment = Appointment.Rehydrate(0, storedSlot.Id, storedPatient.Id, fixture.Unique("Test visit"), AppointmentStatus.Pending, null, null);
        var storedAppointment = (await repo.ReserveAsync(storedSlot, appointment)).Value;
        storedAppointment.RegisterAttention(fixture.Unique("Test diagnosis"), fixture.Unique("Test prescription")).IsSuccess.Should().BeTrue();
        (await repo.RegisterAttentionAsync(storedAppointment)).IsSuccess.Should().BeTrue();

        var reloaded = await repo.GetAppointmentByIdAsync(storedAppointment.Id);
        reloaded!.Diagnosis.Should().Be(fixture.Unique("Test diagnosis"));
        reloaded.Prescription.Should().Be(fixture.Unique("Test prescription"));
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
        fixture.EnsureAvailable();
        var repo = fixture.CreateRepository(out var repoContext);
        await using var disposeRepoContext = repoContext;
        var specialtyRepo = (ISpecialtyRepository)repo;
        var specialty = (await specialtyRepo.AddAsync(new SpecialtyWriteRequest(fixture.Unique("Inactive test")))).Value;
        var doctor = (await repo.AddAsync(new DoctorWriteRequest(fixture.Unique("TEST-INACTIVE"), fixture.Unique("Test"), fixture.Unique("Doctor"), specialty.Id))).Value;
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

        var patient = (await repo.AddAsync(Patient.Create(await fixture.NewDniAsync(), fixture.Unique("Test"), fixture.Unique("Patient"), fixture.Unique("555"), null).Value)).Value;
        var appointment = Appointment.Rehydrate(0, storedSlot.Id, patient.Id, fixture.Unique("Visit"), AppointmentStatus.Pending, null, null);
        (await repo.ReserveAsync(storedSlot, appointment)).Error.Code.Should().Be("SLOT_NOT_AVAILABLE");
        await using var verify = fixture.CreateContext();
        (await verify.ScheduleSlots.CountAsync(x => x.DoctorId == doctor.Id)).Should().Be(1);
        (await verify.Appointments.CountAsync(x => x.ScheduleSlotId == storedSlot.Id)).Should().Be(0);
    }

    [Fact]
    [Trait("RF", "RF-PER-05")]
    [Trait("RF", "RF-PER-09")]
    [Trait("RF", "RF-PER-10")]
    public async Task CatalogService_ValidatesInputsFiltersInactiveAndBlocksDeactivationUntilFutureAppointmentsAreFinished()
    {
        fixture.EnsureAvailable();
        var repo = fixture.CreateRepository(out var repoContext);
        await using var disposeRepoContext = repoContext;
        var service = new CatalogService(repo, repo);
        (await service.AddSpecialtyAsync(new SpecialtyWriteRequest("  "))).Error.Code.Should().Be("SPECIALTY_NAME_REQUIRED");
        (await service.AddDoctorAsync(new DoctorWriteRequest(" ", "", "", null))).Error.Code.Should().Be("DOCTOR_LICENSE_REQUIRED");

        var specialty = (await service.AddSpecialtyAsync(new SpecialtyWriteRequest(fixture.Unique("Future test")))).Value;
        var doctor = (await service.AddDoctorAsync(new DoctorWriteRequest(fixture.Unique("FUTURE-LIC"), fixture.Unique("Test"), fixture.Unique("Doctor"), specialty.Id))).Value;
        var patient = (await repo.AddAsync(Patient.Create(await fixture.NewDniAsync(), fixture.Unique("Test"), fixture.Unique("Patient"), fixture.Unique("555"), null).Value)).Value;
        var doctorDomain = await ((IDoctorRepository)repo).GetByIdAsync(doctor.Id);
        var slot = ScheduleSlot.Create(doctorDomain!, new DateOnly(2026, 10, 16),
            TimeRange.Create(new TimeOnly(9, 0), new TimeOnly(9, 30)).Value,
            TestBuilders.ReferenceNow, []).Value;
        var storedSlot = (await repo.AddSlotAsync(slot)).Value;
        var appointment = Appointment.Rehydrate(0, storedSlot.Id, patient.Id, fixture.Unique("Future visit"), AppointmentStatus.Pending, null, null);
        var storedAppointment = (await repo.ReserveAsync(storedSlot, appointment)).Value;

        (await service.DeactivateDoctorAsync(doctor.Id)).Error.Code.Should().Be("DOCTOR_FUTURE_APPOINTMENTS_EXIST");
        (await service.DeactivateSpecialtyAsync(specialty.Id)).Error.Code.Should().Be("SPECIALTY_FUTURE_APPOINTMENTS_EXIST");
        await using (var verifyBlocked = fixture.CreateContext())
        {
            (await verifyBlocked.Doctors.SingleAsync(x => x.Id == doctor.Id)).IsActive.Should().BeTrue();
            (await verifyBlocked.Specialties.SingleAsync(x => x.Id == specialty.Id)).IsActive.Should().BeTrue();
        }
        (await service.ListDoctorsAsync(false)).Should().ContainSingle(x => x.Id == doctor.Id);
        (await service.ListSpecialtiesAsync(false)).Should().ContainSingle(x => x.Id == specialty.Id);

        storedAppointment.RegisterAttention(fixture.Unique("Finished"), null);
        (await repo.RegisterAttentionAsync(storedAppointment)).IsSuccess.Should().BeTrue();
        (await service.DeactivateDoctorAsync(doctor.Id)).IsSuccess.Should().BeTrue();
        (await service.DeactivateSpecialtyAsync(specialty.Id)).IsSuccess.Should().BeTrue();
        (await service.ListDoctorsAsync(false)).Should().NotContain(x => x.Id == doctor.Id);
        (await service.ListDoctorsAsync(true)).Should().ContainSingle(x => x.Id == doctor.Id && !x.IsActive);
        (await service.ListSpecialtiesAsync(false)).Should().NotContain(x => x.Id == specialty.Id);
        (await service.ListSpecialtiesAsync(true)).Should().ContainSingle(x => x.Id == specialty.Id && !x.IsActive);
        await using var verify = fixture.CreateContext();
        (await verify.Doctors.CountAsync(x => x.Id == doctor.Id && !x.IsActive)).Should().Be(1);
        (await verify.Specialties.CountAsync(x => x.Id == specialty.Id && !x.IsActive)).Should().Be(1);
        (await verify.Appointments.CountAsync(x => x.Id == storedAppointment.Id)).Should().Be(1);
    }

    [Fact]
    [Trait("RF", "RF-PER-02")]
    public async Task ConcurrentReservations_OnSqlServer_ConfirmAtMostOneReservation()
    {
        fixture.EnsureAvailable();
        var setup = fixture.CreateRepository(out var setupContext);
        await using var disposeSetup = setupContext;
        var specialty = (await ((ISpecialtyRepository)setup).AddAsync(new SpecialtyWriteRequest(fixture.Unique("Concurrency test")))).Value;
        var doctor = (await ((IDoctorRepository)setup).AddAsync(new DoctorWriteRequest(fixture.Unique("CONCURRENT-LIC"), fixture.Unique("Test"), fixture.Unique("Doctor"), specialty.Id))).Value;
        var patientIds = new List<int>();
        for (var i = 0; i < 12; i++)
            patientIds.Add((await setup.AddAsync(Patient.Create(await fixture.NewDniAsync(), fixture.Unique("Concurrent"), fixture.Unique($"Patient{i}"), fixture.Unique("555"), null).Value)).Value.Id);
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
            var candidate = Appointment.Rehydrate(0, storedSlot.Id, patientId, fixture.Unique("Concurrent"), AppointmentStatus.Pending, null, null);
            return await repository.ReserveAsync(storedSlot, candidate);
        });
        var results = await Task.WhenAll(attempts);
        results.Count(x => x.IsSuccess).Should().Be(1);
        results.Count(x => x.IsFailure && x.Error.Code == "SLOT_NOT_AVAILABLE").Should().Be(patientIds.Count - 1);
    }

    [Fact]
    [Trait("RF", "RF-PER-01")]
    public async Task PatientSearch_OnSqlServer_PreservesDniUniquenessAndNormalizedOrdering()
    {
        fixture.EnsureAvailable();
        var repository = fixture.CreateRepository(out var context);
        await using var disposeContext = context;
        var surname = fixture.Unique("Muñoz");
        var first = Patient.Create(await fixture.NewDniAsync(), "Ana", surname, "555", null).Value;
        var second = Patient.Create(await fixture.NewDniAsync(), "Beto", surname, "555", null).Value;
        (await repository.AddAsync(first)).IsSuccess.Should().BeTrue();
        (await repository.AddAsync(second)).IsSuccess.Should().BeTrue();
        (await repository.AddAsync(first)).Error.Code.Should().Be("PATIENT_DNI_DUPLICATE");
        var results = await repository.SearchByLastNameAsync(surname.Replace("ñ", "n").ToUpperInvariant());
        results.Where(x => x.Dni.Value == first.Dni.Value || x.Dni.Value == second.Dni.Value)
            .Select(x => x.FirstName).Should().Equal("Ana", "Beto");
        (await repository.GetByDniAsync(first.Dni.Value))!.Dni.Value.Should().Be(first.Dni.Value);
    }

    [Fact]
    [Trait("RF", "RF-PER-09")]
    public async Task CancelledFutureAppointment_DoesNotBlockDeactivationAndRetainsRows()
    {
        fixture.EnsureAvailable();
        var repository = fixture.CreateRepository(out var context);
        await using var disposeContext = context;
        var specialties = (ISpecialtyRepository)repository;
        var specialty = (await specialties.AddAsync(new SpecialtyWriteRequest(fixture.Unique("Cancelled test")))).Value;
        var doctor = (await repository.AddAsync(new DoctorWriteRequest(fixture.Unique("CANCELLED"), "Test", "Doctor", specialty.Id))).Value;
        var domainDoctor = await ((IDoctorRepository)repository).GetByIdAsync(doctor.Id);
        var slot = ScheduleSlot.Create(domainDoctor!, new DateOnly(2026, 10, 17),
            TimeRange.Create(new TimeOnly(11, 0), new TimeOnly(11, 30)).Value,
            TestBuilders.ReferenceNow, []).Value;
        var storedSlot = (await repository.AddSlotAsync(slot)).Value;
        var patient = (await repository.AddAsync(Patient.Create(await fixture.NewDniAsync(),
            "Test", fixture.Unique("Patient"), "555", null).Value)).Value;
        var appointment = Appointment.Rehydrate(0, storedSlot.Id, patient.Id, fixture.Unique("Cancelled visit"),
            AppointmentStatus.Pending, null, null);
        var stored = (await repository.ReserveAsync(storedSlot, appointment)).Value;
        // There is no cancellation API yet: mark only this test-owned appointment terminal.
        await context.Appointments.Where(x => x.Id == stored.Id).ExecuteUpdateAsync(
            update => update.SetProperty(x => x.Status, (int)AppointmentStatus.Cancelled));
        (await repository.DeactivateAsync(doctor.Id)).IsSuccess.Should().BeTrue();
        (await specialties.DeactivateAsync(specialty.Id)).IsSuccess.Should().BeTrue();
        await using var verify = fixture.CreateContext();
        (await verify.Appointments.SingleAsync(x => x.Id == stored.Id)).Status.Should().Be((int)AppointmentStatus.Cancelled);
        (await verify.Doctors.SingleAsync(x => x.Id == doctor.Id)).IsActive.Should().BeFalse();
        (await verify.Specialties.SingleAsync(x => x.Id == specialty.Id)).IsActive.Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("RF", "RF-PER-09")]
    public async Task DeactivationAndReservationRace_NeverLeavesFutureBookingForDeactivatedCatalog(bool deactivateSpecialty)
    {
        fixture.EnsureAvailable();
        var setup = fixture.CreateRepository(out var setupContext);
        await using var disposeSetup = setupContext;
        var specialty = (await ((ISpecialtyRepository)setup).AddAsync(
            new SpecialtyWriteRequest(fixture.Unique("Race specialty")))).Value;
        var doctor = (await setup.AddAsync(new DoctorWriteRequest(fixture.Unique("RACE-LIC"),
            "Test", "Doctor", specialty.Id))).Value;
        var patient = (await setup.AddAsync(Patient.Create(await fixture.NewDniAsync(),
            "Test", fixture.Unique("Race patient"), "555", null).Value)).Value;
        var doctorDomain = await ((IDoctorRepository)setup).GetByIdAsync(doctor.Id);
        var slot = ScheduleSlot.Create(doctorDomain!, new DateOnly(2026, 10, 17),
            TimeRange.Create(new TimeOnly(12, 0), new TimeOnly(12, 30)).Value,
            TestBuilders.ReferenceNow, []).Value;
        var storedSlot = (await setup.AddSlotAsync(slot)).Value;
        var candidate = Appointment.Rehydrate(0, storedSlot.Id, patient.Id,
            fixture.Unique("Race visit"), AppointmentStatus.Pending, null, null);

        async Task<Result<Appointment>> Reserve()
        {
            await using var context = fixture.CreateContext();
            var repo = new SqlServerClinicRepository(context,
                new ClinicClock(FixedTimeProvider.CreateForLima(TestBuilders.ReferenceNow)));
            return await repo.ReserveAsync(storedSlot, candidate);
        }

        async Task<Result> Deactivate()
        {
            await using var context = fixture.CreateContext();
            var repo = new SqlServerClinicRepository(context,
                new ClinicClock(FixedTimeProvider.CreateForLima(TestBuilders.ReferenceNow)));
            return deactivateSpecialty
                ? await ((ISpecialtyRepository)repo).DeactivateAsync(specialty.Id)
                : await repo.DeactivateAsync(doctor.Id);
        }

        var reserveTask = Reserve();
        var deactivateTask = Deactivate();
        await Task.WhenAll(reserveTask, deactivateTask);
        var reservation = await reserveTask;
        var deactivation = await deactivateTask;
        (reservation.IsSuccess ^ deactivation.IsSuccess).Should().BeTrue();
        if (reservation.IsFailure) reservation.Error.Code.Should().Be("SLOT_NOT_AVAILABLE");
        if (deactivation.IsFailure) deactivation.Error.Code.Should().Be(deactivateSpecialty
            ? "SPECIALTY_FUTURE_APPOINTMENTS_EXIST" : "DOCTOR_FUTURE_APPOINTMENTS_EXIST");

        await using var verify = fixture.CreateContext();
        (await verify.Appointments.AnyAsync(x => x.ScheduleSlotId == storedSlot.Id)).Should().Be(reservation.IsSuccess);
        (await verify.ScheduleSlots.SingleAsync(x => x.Id == storedSlot.Id)).Status.Should().Be(
            reservation.IsSuccess ? (int)ScheduleSlotStatus.Reserved : (int)ScheduleSlotStatus.Available);
        var isActive = deactivateSpecialty
            ? (await verify.Specialties.SingleAsync(x => x.Id == specialty.Id)).IsActive
            : (await verify.Doctors.SingleAsync(x => x.Id == doctor.Id)).IsActive;
        isActive.Should().Be(reservation.IsSuccess);
    }

    [Fact]
    [Trait("RF", "RF-PER-02")]
    public async Task AddSlot_OnSqlServer_RejectsOverlapAndAllowsContiguousIntervals()
    {
        fixture.EnsureAvailable();
        var repo = fixture.CreateRepository(out var repoContext);
        await using var disposeRepoContext = repoContext;
        var specialty = (await ((ISpecialtyRepository)repo).AddAsync(new SpecialtyWriteRequest(fixture.Unique("Overlap test")))).Value;
        var doctor = (await ((IDoctorRepository)repo).AddAsync(new DoctorWriteRequest(fixture.Unique("OVERLAP-LIC"), fixture.Unique("Test"), fixture.Unique("Doctor"), specialty.Id))).Value;
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
        fixture.EnsureAvailable();
        var setup = fixture.CreateRepository(out var setupContext);
        await using var disposeSetup = setupContext;
        var specialty = (await ((ISpecialtyRepository)setup).AddAsync(new SpecialtyWriteRequest(fixture.Unique("Attention race")))).Value;
        var doctor = (await ((IDoctorRepository)setup).AddAsync(new DoctorWriteRequest(fixture.Unique("ATTENTION-LIC"), fixture.Unique("Test"), fixture.Unique("Doctor"), specialty.Id))).Value;
        var patient = (await setup.AddAsync(Patient.Create(await fixture.NewDniAsync(), fixture.Unique("Attention"), fixture.Unique("Patient"), fixture.Unique("555"), null).Value)).Value;
        var doctorDomain = await ((IDoctorRepository)setup).GetByIdAsync(doctor.Id);
        var slot = ScheduleSlot.Create(doctorDomain!, new DateOnly(2026, 10, 16),
            TimeRange.Create(new TimeOnly(9, 0), new TimeOnly(9, 30)).Value, TestBuilders.ReferenceNow, []).Value;
        var storedSlot = (await setup.AddSlotAsync(slot)).Value;
        var candidate = Appointment.Rehydrate(0, storedSlot.Id, patient.Id, fixture.Unique("Attention race"), AppointmentStatus.Pending, null, null);
        var storedAppointment = (await setup.ReserveAsync(storedSlot, candidate)).Value;
        var attempts = Enumerable.Range(0, 10).Select(async _ =>
        {
            await using var context = fixture.CreateContext();
            var repository = new SqlServerClinicRepository(context,
                new ClinicClock(FixedTimeProvider.CreateForLima(TestBuilders.ReferenceNow)));
            var attended = Appointment.Rehydrate(storedAppointment.Id, storedAppointment.ScheduleSlotId,
                storedAppointment.PatientId, storedAppointment.Reason, AppointmentStatus.Attended,
                fixture.Unique("Concurrent diagnosis"), fixture.Unique("Concurrent prescription"));
            return await repository.RegisterAttentionAsync(attended);
        });
        var results = await Task.WhenAll(attempts);
        results.Count(x => x.IsSuccess).Should().Be(1);
        results.Count(x => x.IsFailure && x.Error.Code == "APPOINTMENT_NOT_PENDING").Should().Be(9);
        await using var verify = fixture.CreateContext();
        (await verify.Attentions.CountAsync(x => x.AppointmentId == storedAppointment.Id)).Should().Be(1);
        var persisted = await verify.Attentions.SingleAsync(x => x.AppointmentId == storedAppointment.Id);
        persisted.Diagnosis.Should().Be(fixture.Unique("Concurrent diagnosis"));
        persisted.Prescription.Should().Be(fixture.Unique("Concurrent prescription"));
        (await verify.Appointments.SingleAsync(x => x.Id == storedAppointment.Id)).Status.Should().Be((int)AppointmentStatus.Attended);
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

}

public static class SqlTestCatalogPolicy
{
    public static bool IsApprovedDestination(string server, string catalog) =>
        string.Equals(server, "localhost", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(catalog, "BD_MedicinaGen_Tests", StringComparison.OrdinalIgnoreCase);

    public static bool TryParseApprovedConnectionString(
        string? configuredConnection,
        out SqlConnectionStringBuilder? builder)
    {
        builder = null;
        if (string.IsNullOrWhiteSpace(configuredConnection)) return false;

        try
        {
            var parsed = new SqlConnectionStringBuilder(configuredConnection);
            if (!IsApprovedDestination(parsed.DataSource, parsed.InitialCatalog)) return false;
            builder = parsed;
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static bool IsFreshEmptyCatalog(int userTableCount, bool historyExists) =>
        userTableCount == 0 && !historyExists;

    public static bool HasRecognizedMigrationHistory(
        IReadOnlyCollection<string> recordedMigrationIds,
        IReadOnlySet<string> recognizedMigrationIds) =>
        recordedMigrationIds.Count > 0 && recordedMigrationIds.All(recognizedMigrationIds.Contains);

    public static bool ShouldBlockBeforeMigration(
        bool inspectionSucceeded,
        int userTableCount,
        bool historyExists,
        bool historyInDbo,
        bool historyOutsideDbo,
        IReadOnlyCollection<string> recordedMigrationIds,
        IReadOnlySet<string> recognizedMigrationIds)
    {
        if (!inspectionSucceeded || historyOutsideDbo) return true;
        if (IsFreshEmptyCatalog(userTableCount, historyExists)) return false;
        return userTableCount == 0 || !historyExists || !historyInDbo ||
            !HasRecognizedMigrationHistory(recordedMigrationIds, recognizedMigrationIds);
    }
}
