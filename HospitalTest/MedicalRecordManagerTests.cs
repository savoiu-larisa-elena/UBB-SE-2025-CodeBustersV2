using Hospital.DatabaseServices;
using Hospital.Exceptions;
using Hospital.Managers;
using Hospital.Models;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Hospital.Tests.Managers
{
    [TestFixture]
    public class MedicalRecordManagerTests
    {
        private Mock<IMedicalRecordsDatabaseService> _mockDbService;
        private MedicalRecordManager _manager;

        [SetUp]
        public void SetUp()
        {
            _mockDbService = new Mock<IMedicalRecordsDatabaseService>();
            _manager = new MedicalRecordManager(_mockDbService.Object);
        }

        [Test]
        public async Task LoadMedicalRecordsForPatient_LoadsList()
        {
            var records = new List<MedicalRecordJointModel>
            {
                new MedicalRecordJointModel(1, 1, "John", 1, "Dr. A", 1, "Cardio", 1, "MRI", DateTime.Now, "Fine")
            };

            _mockDbService.Setup(s => s.GetMedicalRecordsForPatient(1)).ReturnsAsync(records);

            await _manager.LoadMedicalRecordsForPatient(1);

            Assert.That(_manager.MedicalRecords.Count, Is.EqualTo(1));
        }

        [Test]
        public void GetMedicalRecordById_RecordExists_ReturnsRecord()
        {
            var record = new MedicalRecordJointModel(1, 1, "John", 1, "Dr. A", 1, "Cardio", 1, "MRI", DateTime.Now, "Fine");
            _mockDbService.Setup(s => s.GetMedicalRecordById(1)).Returns(Task.FromResult(record));

            var result = _manager.GetMedicalRecordById(1);

            Assert.That(result.PatientName, Is.EqualTo("John"));
        }

        [Test]
        public void GetMedicalRecordById_RecordMissing_ThrowsCustomException()
        {
            _mockDbService.Setup(s => s.GetMedicalRecordById(1)).Throws<MedicalRecordNotFoundException>();

            Assert.Throws<MedicalRecordNotFoundException>(() => _manager.GetMedicalRecordById(1));
        }

        [Test]
        public async Task CreateMedicalRecord_ValidData_AddsToList()
        {
            var appointment = new AppointmentJointModel(1, false, DateTime.Now, 1, "Cardio", 2, "Dr. A", 3, "John", 4, "MRI", TimeSpan.FromMinutes(30));
            var expectedId = 99;

            _mockDbService.Setup(s => s.AddMedicalRecord(It.IsAny<MedicalRecordModel>())).ReturnsAsync(expectedId);
            _mockDbService.Setup(s => s.GetMedicalRecordById(expectedId)).ReturnsAsync(
                new MedicalRecordJointModel(expectedId, 3, "John", 2, "Dr. A", 1, "Cardio", 4, "MRI", DateTime.Now, "Test OK"));

            var result = await _manager.CreateMedicalRecord(appointment, "Test OK");

            Assert.That(result, Is.EqualTo(expectedId));
            Assert.That(_manager.MedicalRecords.Count, Is.EqualTo(1));
        }

        [Test]
        public void ValidateConclusion_Valid_ReturnsTrue()
        {
            Assert.IsTrue(_manager.ValidateConclusion("This is fine."));
        }

        [Test]
        public void ValidateConclusion_Empty_ReturnsFalse()
        {
            Assert.IsFalse(_manager.ValidateConclusion(""));
        }

        [Test]
        public void ValidateConclusion_TooLong_ReturnsFalse()
        {
            var longText = new string('a', 256);
            Assert.IsFalse(_manager.ValidateConclusion(longText));
        }

        [Test]
        public void CreateMedicalRecordWithAppointment_InvalidConclusion_ThrowsValidationException()
        {
            var appointment = new AppointmentJointModel(1, false, DateTime.Now, 1, "Cardio", 2, "Dr. A", 3, "John", 4, "MRI", TimeSpan.FromMinutes(30));

            Assert.ThrowsAsync<ValidationException>(() => _manager.CreateMedicalRecordWithAppointment(appointment, ""));
        }

        [Test]
        public async Task CreateMedicalRecordWithAppointment_ValidConclusion_UpdatesAndCreates()
        {
            var appointment = new AppointmentJointModel(1, false, DateTime.Now.AddDays(-1), 1, "Cardio", 2, "Dr. A", 3, "John", 4, "MRI", TimeSpan.FromMinutes(30));

            _mockDbService.Setup(s => s.AddMedicalRecord(It.IsAny<MedicalRecordModel>())).ReturnsAsync(99);
            _mockDbService.Setup(s => s.GetMedicalRecordById(99)).ReturnsAsync(
                new MedicalRecordJointModel(99, 3, "John", 2, "Dr. A", 1, "Cardio", 4, "MRI", DateTime.Now, "Healthy"));

            var result = await _manager.CreateMedicalRecordWithAppointment(appointment, "Healthy");

            Assert.That(result, Is.EqualTo(99));
            Assert.That(appointment.Finished, Is.True);
        }

        [Test]
        public async Task LoadMedicalRecordsForDoctor_AddsRecords()
        {
            var records = new List<MedicalRecordJointModel>
            {
                new MedicalRecordJointModel(1, 1, "John", 2, "Dr. A", 1, "Cardio", 4, "MRI", DateTime.Now, "Good")
            };

            _mockDbService.Setup(s => s.GetMedicalRecordsForDoctor(2)).ReturnsAsync(records);

            await _manager.LoadMedicalRecordsForDoctor(2);

            Assert.That(_manager.MedicalRecords.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task GetMedicalRecords_ReturnsList()
        {
            var result = await _manager.GetMedicalRecords();
            Assert.IsInstanceOf<List<MedicalRecordJointModel>>(result);
        }
    }
}
