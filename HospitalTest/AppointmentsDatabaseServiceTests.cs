using Hospital.DatabaseServices;
using Hospital.Models;
using Hospital.Exceptions;
using Hospital.Configs;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Hospital.Tests.DatabaseServices
{
    [TestFixture]
    public class AppointmentsDatabaseServiceTests
    {
        private IAppointmentsDatabaseService _service;
        private ApplicationConfiguration _config;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Initialize the singleton configuration
            _config = ApplicationConfiguration.GetInstance();
        }

        [SetUp]
        public void SetUp()
        {
            _service = new AppointmentsDatabaseService();
        }

        [Test]
        public async Task AddAppointmentToDataBase_ValidAppointment_ReturnsTrue()
        {
            // Arrange
            var appointment = new AppointmentModel(
                appointmentId: 0,
                patientId: 1,
                doctorId: 1,
                dateAndTime: DateTime.Now.AddDays(1),
                finished: false,
                procedureId: 1
            );

            // Act
            var result = await _service.AddAppointmentToDataBase(appointment);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void AddAppointmentToDataBase_DatabaseError_ThrowsException()
        {
            // Arrange
            var appointment = new AppointmentModel(
                appointmentId: 0,
                patientId: -1,
                doctorId: -1,
                dateAndTime: DateTime.Now.AddDays(1),
                finished: false,
                procedureId: -1
            );

            // Act & Assert
            Assert.ThrowsAsync<DatabaseOperationException>(
                async () => await _service.AddAppointmentToDataBase(appointment)
            );
        }

        [Test]
        public void AddAppointmentToDataBase_PastDate_ThrowsException()
        {
            // Arrange
            var appointment = new AppointmentModel(
                appointmentId: 0,
                patientId: 1,
                doctorId: 1,
                dateAndTime: DateTime.Now.AddDays(-1),
                finished: false,
                procedureId: 1
            );

            // Act & Assert
            var exception = Assert.ThrowsAsync<InvalidAppointmentException>(
                async () => await _service.AddAppointmentToDataBase(appointment)
            );
            Assert.That(exception.Message, Does.Contain("Cannot create appointments in the past"));
        }

        [Test]
        public async Task GetAppointmentsForDoctor_ValidDoctorId_ReturnsAppointments()
        {
            // Arrange
            int doctorId = 1;

            // Act
            var result = await _service.GetAppointmentsForDoctor(doctorId);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                CollectionAssert.AllItemsAreInstancesOfType(result, typeof(AppointmentJointModel));
            });
        }

        [Test]
        public async Task GetAppointmentsForDoctor_InvalidId_ReturnsEmptyList()
        {
            // Arrange
            int doctorId = -1;

            // Act
            var result = await _service.GetAppointmentsForDoctor(doctorId);

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetAppointmentsForPatient_ValidPatientId_ReturnsAppointments()
        {
            // Arrange
            int patientId = 1;

            // Act
            var result = await _service.GetAppointmentsForPatient(patientId);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                CollectionAssert.AllItemsAreInstancesOfType(result, typeof(AppointmentJointModel));
            });
        }

        [Test]
        public async Task GetAppointmentsForPatient_InvalidId_ReturnsEmptyList()
        {
            // Arrange
            int patientId = -1;

            // Act
            var result = await _service.GetAppointmentsForPatient(patientId);

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetAppointmentsByDoctorAndDate_ValidInput_ReturnsAppointments()
        {
            // Arrange
            int doctorId = 1;
            DateTime date = DateTime.Today;

            // Act
            var result = await _service.GetAppointmentsByDoctorAndDate(doctorId, date);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                CollectionAssert.AllItemsAreInstancesOfType(result, typeof(AppointmentJointModel));
            });
        }

        [Test]
        public void GetAppointmentsByDoctorAndDate_InvalidId_ThrowsException()
        {
            // Arrange
            int doctorId = -1;
            DateTime date = DateTime.Today;

            // Act & Assert
            Assert.ThrowsAsync<DatabaseOperationException>(
                async () => await _service.GetAppointmentsByDoctorAndDate(doctorId, date)
            );
        }

        [Test]
        public void GetAppointmentsByDoctorAndDate_PastDate_ThrowsException()
        {
            // Arrange
            int doctorId = 1;
            DateTime date = DateTime.Today.AddDays(-1);

            // Act & Assert
            Assert.ThrowsAsync<InvalidAppointmentException>(
                async () => await _service.GetAppointmentsByDoctorAndDate(doctorId, date)
            );
        }

        [Test]
        public async Task GetAppointment_ValidId_ReturnsAppointment()
        {
            // Arrange
            // First create an appointment
            var appointment = new AppointmentModel(
                appointmentId: 0,
                patientId: 1,
                doctorId: 1,
                dateAndTime: DateTime.Now.AddDays(1),
                finished: false,
                procedureId: 1
            );
            await _service.AddAppointmentToDataBase(appointment);

            // Get a valid appointment ID
            var appointments = await _service.GetAppointmentsForDoctor(1);
            var appointmentId = appointments[0].AppointmentId;

            // Act
            var result = await _service.GetAppointment(appointmentId);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Is.InstanceOf<AppointmentJointModel>());
            });
        }

        [Test]
        public async Task GetAppointment_InvalidId_ReturnsNull()
        {
            // Arrange
            int appointmentId = -1;

            // Act
            var result = await _service.GetAppointment(appointmentId);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task RemoveAppointmentFromDataBase_ValidId_ReturnsTrue()
        {
            // Arrange
            // First create an appointment
            var appointment = new AppointmentModel(
                appointmentId: 0,
                patientId: 1,
                doctorId: 1,
                dateAndTime: DateTime.Now.AddDays(1),
                finished: false,
                procedureId: 1
            );
            await _service.AddAppointmentToDataBase(appointment);

            // Get a valid appointment ID
            var appointments = await _service.GetAppointmentsForDoctor(1);
            var validAppointmentId = appointments[0].AppointmentId;

            // Act
            var result = await _service.RemoveAppointmentFromDataBase(validAppointmentId);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task RemoveAppointmentFromDataBase_InvalidId_ReturnsFalse()
        {
            // Arrange
            int invalidId = -1;

            // Act
            var result = await _service.RemoveAppointmentFromDataBase(invalidId);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void RemoveAppointmentFromDataBase_DatabaseError_ThrowsException()
        {
            // Arrange
            int invalidId = 0;

            // Act & Assert
            Assert.ThrowsAsync<DatabaseOperationException>(
                async () => await _service.RemoveAppointmentFromDataBase(invalidId)
            );
        }
    }
}