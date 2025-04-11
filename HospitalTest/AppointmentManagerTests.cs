using Hospital.DatabaseServices;
using Hospital.Managers;
using Hospital.Models;
using Hospital.Exceptions;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hospital.Tests.Managers
{
    [TestFixture]
    public class AppointmentManagerTests
    {
        private Mock<IAppointmentsDatabaseService> _mockDatabaseService;
        private AppointmentManager _appointmentManager;

        [SetUp]
        public void SetUp()
        {
            _mockDatabaseService = new Mock<IAppointmentsDatabaseService>();
            _appointmentManager = new AppointmentManager(_mockDatabaseService.Object);
        }

        #region LoadDoctorAppointmentsOnDate Tests

        [Test]
        public async Task LoadDoctorAppointmentsOnDate_ValidData_AppointmentsLoaded()
        {
            var doctorId = 1;
            var date = DateTime.Now;
            var appointments = new List<AppointmentJointModel>
            {
                new AppointmentJointModel { DoctorId = doctorId, DateAndTime = date }
            };

            _mockDatabaseService.Setup(s => s.GetAppointmentsByDoctorAndDate(doctorId, date))
                                .ReturnsAsync(appointments);

            await _appointmentManager.LoadDoctorAppointmentsOnDate(doctorId, date);

            Assert.That(_appointmentManager.Appointments.Count, Is.EqualTo(appointments.Count));
        }

        [Test]
        public Task LoadDoctorAppointmentsOnDate_ThrowsException_WhenDatabaseCallFails()
        {
            var doctorId = 1;
            var date = DateTime.Now;
            _mockDatabaseService.Setup(s => s.GetAppointmentsByDoctorAndDate(doctorId, date))
                                .Throws(new Exception("Mocked database exception"));

            Assert.ThrowsAsync<Exception>(() => _appointmentManager.LoadDoctorAppointmentsOnDate(doctorId, date));
            return Task.CompletedTask;
        }

        #endregion

        #region LoadAppointmentsForPatient Tests

        [Test]
        public async Task LoadAppointmentsForPatient_ValidData_AppointmentsLoaded()
        {
            var patientId = 1;
            var appointments = new List<AppointmentJointModel>
            {
                new AppointmentJointModel { PatientId = patientId, DateAndTime = DateTime.Now.AddHours(1), Finished = false },
                new AppointmentJointModel { PatientId = patientId, DateAndTime = DateTime.Now.AddHours(-1), Finished = false }
            };

            _mockDatabaseService.Setup(s => s.GetAppointmentsForPatient(patientId))
                                .ReturnsAsync(appointments);

            await _appointmentManager.LoadAppointmentsForPatient(patientId);

            Assert.That(_appointmentManager.Appointments.Count, Is.EqualTo(1));
        }

        [Test]
        public Task LoadAppointmentsForPatient_ThrowsException_WhenDatabaseCallFails()
        {
            var patientId = 1;
            _mockDatabaseService.Setup(s => s.GetAppointmentsForPatient(patientId))
                                .Throws(new Exception("Mocked database exception"));

            Assert.ThrowsAsync<Exception>(() => _appointmentManager.LoadAppointmentsForPatient(patientId));
            return Task.CompletedTask;
        }

        #endregion

        #region RemoveAppointment Tests

        [Test]
        public async Task RemoveAppointment_ValidAppointment_Removed()
        {
            var appointmentId = 1;
            var appointment = new AppointmentJointModel { DateAndTime = DateTime.Now.AddHours(24.1) };
            _mockDatabaseService.Setup(s => s.GetAppointment(appointmentId))
                                .ReturnsAsync(appointment);
            _mockDatabaseService.Setup(s => s.RemoveAppointmentFromDataBase(appointmentId))
                                .ReturnsAsync(true);

            var result = await _appointmentManager.RemoveAppointment(appointmentId);

            Assert.IsTrue(result);
        }

        [Test]
        public Task RemoveAppointment_AppointmentNotFound_ThrowsException()
        {
            var appointmentId = 1;
            _mockDatabaseService.Setup(s => s.GetAppointment(appointmentId))!
                                .ReturnsAsync((AppointmentJointModel?)null);

            Assert.ThrowsAsync<AppointmentNotFoundException>(() => _appointmentManager.RemoveAppointment(appointmentId));
            return Task.CompletedTask;
        }

        [Test]
        public Task RemoveAppointment_AppointmentWithin24Hours_ThrowsException()
        {
            var appointmentId = 1;
            var appointment = new AppointmentJointModel { DateAndTime = DateTime.Now.AddHours(12) };
            _mockDatabaseService.Setup(s => s.GetAppointment(appointmentId))
                                .ReturnsAsync(appointment);

            Assert.ThrowsAsync<CancellationNotAllowedException>(() => _appointmentManager.RemoveAppointment(appointmentId));
            return Task.CompletedTask;
        }

        #endregion

        #region CreateAppointment Tests

        [Test]
        public Task CreateAppointment_ValidData_AppointmentCreated()
        {
            var newAppointment = new AppointmentModel { DoctorId = 1, PatientId = 1, DateAndTime = DateTime.Now.AddHours(1) };
            _mockDatabaseService.Setup(s => s.GetAppointmentsByDoctorAndDate(newAppointment.DoctorId, newAppointment.DateAndTime))
                                .ReturnsAsync(new List<AppointmentJointModel>());
            _mockDatabaseService.Setup(s => s.GetAppointmentsForPatient(newAppointment.PatientId))
                                .ReturnsAsync(new List<AppointmentJointModel>());
            _mockDatabaseService.Setup(s => s.AddAppointmentToDataBase(newAppointment))
                                .ReturnsAsync(true);

            Assert.DoesNotThrowAsync(() => _appointmentManager.CreateAppointment(newAppointment));
            return Task.CompletedTask;
        }

        [Test]
        public Task CreateAppointment_TimeSlotTaken_ThrowsException()
        {
            var newAppointment = new AppointmentModel { DoctorId = 1, PatientId = 1, DateAndTime = DateTime.Now.AddHours(1) };
            var existingAppointment = new AppointmentJointModel { DoctorId = newAppointment.DoctorId, DateAndTime = newAppointment.DateAndTime };
            _mockDatabaseService.Setup(s => s.GetAppointmentsByDoctorAndDate(newAppointment.DoctorId, newAppointment.DateAndTime))
                                .ReturnsAsync(new List<AppointmentJointModel> { existingAppointment });

            Assert.ThrowsAsync<AppointmentConflictException>(() => _appointmentManager.CreateAppointment(newAppointment));
            return Task.CompletedTask;
        }

        #endregion

        #region CanCancelAppointment Tests

        [Test]
        public void CanCancelAppointment_AppointmentIsCancelable_ReturnsTrue()
        {
            var appointment = new AppointmentJointModel { DateAndTime = DateTime.Now.AddHours(24.01) };

            var result = _appointmentManager.CanCancelAppointment(appointment);

            Assert.IsTrue(result);
        }

        [Test]
        public void CanCancelAppointment_AppointmentIsNotCancelable_ReturnsFalse()
        {
            var appointment = new AppointmentJointModel { DateAndTime = DateTime.Now.AddHours(12) };

            var result = _appointmentManager.CanCancelAppointment(appointment);

            Assert.IsFalse(result);
        }

        #endregion
    }
}
