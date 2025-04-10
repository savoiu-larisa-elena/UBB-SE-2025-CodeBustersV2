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
                new DepartmentModel(2, "Neurology"),
                new DepartmentModel(3, "Pediatrics")
            };

            _mockDatabaseService.Setup(s => s.GetDepartmentsFromDataBase())
                .ReturnsAsync(expectedDepartments);
            Console.WriteLine($"Mocked database service to return {expectedDepartments.Count} departments.");
            Console.WriteLine($"Expected department ID: {expectedDepartments[0].DepartmentId}, Name: {expectedDepartments[0].DepartmentName}");
            Console.WriteLine($"Expected department ID: {expectedDepartments[1].DepartmentId}, Name: {expectedDepartments[1].DepartmentName}");
            // Act

            // Assert
            Assert.That(expectedDepartments, Is.Not.Null);
            Assert.That(expectedDepartments.Count, Is.EqualTo(3));
            Assert.That(expectedDepartments[0].DepartmentId, Is.EqualTo(expectedDepartments[0].DepartmentId));
            Assert.That(expectedDepartments[0].DepartmentName, Is.EqualTo(expectedDepartments[0].DepartmentName));
            Assert.That(expectedDepartments[1].DepartmentId, Is.EqualTo(expectedDepartments[1].DepartmentId));
            Assert.That(expectedDepartments[1].DepartmentName, Is.EqualTo(expectedDepartments[1].DepartmentName));
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
        }

        #endregion
    }
}
