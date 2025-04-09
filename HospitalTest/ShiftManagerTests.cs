using Hospital.DatabaseServices;
using Hospital.Exceptions;
using Hospital.Managers;
using Hospital.Models;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hospital.Tests.Managers
{
    [TestFixture]
    public class ShiftManagerTests
    {
        private Mock<IShiftsDatabaseService> _mockShiftService;
        private ShiftManager _manager;

        [SetUp]
        public void SetUp()
        {
            _mockShiftService = new Mock<IShiftsDatabaseService>();
            _manager = new ShiftManager(_mockShiftService.Object);
        }

        [Test]
        public async Task LoadShifts_LoadsFromService()
        {
            var shifts = new List<ShiftModel>
            {
                new ShiftModel(1, DateTime.Today, TimeSpan.FromHours(8), TimeSpan.FromHours(16))
            };

            _mockShiftService.Setup(s => s.GetShiftsByDoctorId(1)).ReturnsAsync(shifts);

            await _manager.LoadShifts(1);

            Assert.AreEqual(1, _manager.GetShifts().Count);
        }

        [Test]
        public async Task LoadUpcomingDoctorDayshifts_LoadsFromService()
        {
            var shifts = new List<ShiftModel>
            {
                new ShiftModel(2, DateTime.Today.AddDays(1), TimeSpan.FromHours(8), TimeSpan.FromHours(16))
            };

            _mockShiftService.Setup(s => s.GetDoctorDaytimeShifts(1)).ReturnsAsync(shifts);

            await _manager.LoadUpcomingDoctorDayshifts(1);

            Assert.AreEqual(1, _manager.GetShifts().Count);
        }

        [Test]
        public void GetShifts_ReturnsCurrentList()
        {
            var shift = new ShiftModel(1, DateTime.Today, TimeSpan.FromHours(8), TimeSpan.FromHours(16));
            _manager.GetShifts().Add(shift);

            var result = _manager.GetShifts();

            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public void GetShiftByDay_ShiftExists_ReturnsShift()
        {
            var today = DateTime.Today;
            var shift = new ShiftModel(1, today, TimeSpan.FromHours(8), TimeSpan.FromHours(16));
            _manager.GetShifts().Add(shift);

            var result = _manager.GetShiftByDay(today);

            Assert.AreEqual(today.Date, result.DateTime.Date);
        }

        [Test]
        public void GetShiftByDay_ShiftNotFound_ThrowsException()
        {
            Assert.Throws<ShiftNotFoundException>(() => _manager.GetShiftByDay(DateTime.Today));
        }

        [Test]
        public void GetMonthlyCalendarRange_ReturnsFirstAndLastDayOfMonth()
        {
            var (start, end) = _manager.GetMonthlyCalendarRange();
            var expectedStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var expectedEnd = expectedStart.AddMonths(1).AddDays(-1);

            Assert.AreEqual(expectedStart, start.Date);
            Assert.AreEqual(expectedEnd, end.Date);
        }

        [Test]
        public void GenerateTimeSlots_CreatesCorrectSlots()
        {
            var date = DateTime.Today;
            var shift = new ShiftModel(1, date, TimeSpan.FromHours(8), TimeSpan.FromHours(10));
            var appointment = new AppointmentJointModel(
                1, false, date.AddHours(9), 1, "Cardio", 1, "Dr. A", 1, "Patient A", 1, "MRI", TimeSpan.FromMinutes(30));

            var result = _manager.GenerateTimeSlots(date, new List<ShiftModel> { shift }, new List<AppointmentJointModel> { appointment });

            // Check slot count for 24h with 30 min intervals
            Assert.AreEqual(48, result.Count);

            // Check slot at 9:00 AM is appointment
            var slotAt9 = result.FirstOrDefault(s => s.TimeSlot.Hour == 9 && s.TimeSlot.Minute == 0);
            Assert.NotNull(slotAt9);
            Assert.AreEqual("MRI", slotAt9.Appointment);
        }
    }
}
