using Hospital.DatabaseServices;
using Hospital.Models;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HospitalTest
{
    [TestFixture]
    public class DepartmentsDatabaseServiceTests
    {
        private Mock<IDepartmentsDatabaseService> _mockDatabaseService;
        private DepartmentsDatabaseService _service;

        [SetUp]
        public void SetUp()
        {
            _mockDatabaseService = new Mock<IDepartmentsDatabaseService>();
            _service = new DepartmentsDatabaseService();
        }

        #region GetDepartmentsFromDataBase Tests

        [Test]
        public async Task GetDepartmentsFromDataBase_ValidData_DepartmentsLoaded()
        {
            // Arrange
            var expectedDepartments = new List<DepartmentModel>
            {
                new DepartmentModel(1, "Cardiology"),
                new DepartmentModel(2, "Neurology")
            };

            _mockDatabaseService.Setup(s => s.GetDepartmentsFromDataBase())
                .ReturnsAsync(expectedDepartments);

            // Act
            var result = await _service.GetDepartmentsFromDataBase();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(expectedDepartments.Count));
            Assert.That(result[0].DepartmentId, Is.EqualTo(expectedDepartments[0].DepartmentId));
            Assert.That(result[0].DepartmentName, Is.EqualTo(expectedDepartments[0].DepartmentName));
            Assert.That(result[1].DepartmentId, Is.EqualTo(expectedDepartments[1].DepartmentId));
            Assert.That(result[1].DepartmentName, Is.EqualTo(expectedDepartments[1].DepartmentName));
        }

        [Test]
        public async Task GetDepartmentsFromDataBase_EmptyResult_ReturnsEmptyList()
        {
            // Arrange
            _mockDatabaseService.Setup(s => s.GetDepartmentsFromDataBase())
                .ReturnsAsync(new List<DepartmentModel>());

            // Act
            var result = await _service.GetDepartmentsFromDataBase();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetDepartmentsFromDataBase_DatabaseError_ThrowsException()
        {
            // Arrange
            _mockDatabaseService.Setup(s => s.GetDepartmentsFromDataBase())
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            var exception = Assert.ThrowsAsync<Exception>(() => _service.GetDepartmentsFromDataBase());
            Assert.That(exception.Message, Does.Contain("Error loading departments"));
        }

        #endregion
    }
}
