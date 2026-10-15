using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CitasMedicas.Infrastructure.Persistence.SqlServer.Migrations;

[DbContext(typeof(ClinicDbContext))]
[Migration("202610150001_InitialClinicSchema")]
public sealed class InitialClinicSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Patients",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Dni = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Email = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_Patients", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Specialties",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Specialties", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Doctors",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                LicenseNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                SpecialtyId = table.Column<int>(type: "int", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Doctors", x => x.Id);
                table.ForeignKey("FK_Doctors_Specialties_SpecialtyId", x => x.SpecialtyId, "Specialties", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ScheduleSlots",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                DoctorId = table.Column<int>(type: "int", nullable: false),
                Date = table.Column<DateOnly>(type: "date", nullable: false),
                StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ScheduleSlots", x => x.Id);
                table.ForeignKey("FK_ScheduleSlots_Doctors_DoctorId", x => x.DoctorId, "Doctors", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Appointments",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                ScheduleSlotId = table.Column<int>(type: "int", nullable: false),
                PatientId = table.Column<int>(type: "int", nullable: false),
                Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Appointments", x => x.Id);
                table.ForeignKey("FK_Appointments_Patients_PatientId", x => x.PatientId, "Patients", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_Appointments_ScheduleSlots_ScheduleSlotId", x => x.ScheduleSlotId, "ScheduleSlots", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Attentions",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                AppointmentId = table.Column<int>(type: "int", nullable: false),
                Diagnosis = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Prescription = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Attentions", x => x.Id);
                table.ForeignKey("FK_Attentions_Appointments_AppointmentId", x => x.AppointmentId, "Appointments", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("IX_Patients_Dni", "Patients", "Dni", unique: true);
        migrationBuilder.CreateIndex("IX_Doctors_SpecialtyId", "Doctors", "SpecialtyId");
        migrationBuilder.CreateIndex("IX_ScheduleSlots_DoctorId_Date_StartTime", "ScheduleSlots", new[] { "DoctorId", "Date", "StartTime" });
        migrationBuilder.CreateIndex("IX_Appointments_PatientId", "Appointments", "PatientId");
        migrationBuilder.CreateIndex("IX_Appointments_ScheduleSlotId", "Appointments", "ScheduleSlotId", unique: true);
        migrationBuilder.CreateIndex("IX_Attentions_AppointmentId", "Attentions", "AppointmentId", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("This migration is intentionally non-reversible to prevent deleting persisted clinical data.");
    }
}
