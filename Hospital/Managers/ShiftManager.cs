using Hospital.DatabaseServices;
using System;
using Hospital.Exceptions;
using Hospital.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hospital.Managers
{
    public class ShiftManager : IShiftManager
    {
        private readonly IShiftsDatabaseService _shiftsDatabaseService;
        private List<ShiftModel> _shifts;

        public ShiftManager(IShiftsDatabaseService shiftsDatabaseService)
        {
            _shiftsDatabaseService = shiftsDatabaseService;
            _shifts = new List<ShiftModel>();
        }

        public async Task LoadShifts(int doctorID)
        {
            _shifts = await _shiftsDatabaseService.GetShiftsByDoctorId(doctorID);
        }


        public List<ShiftModel> GetShifts()
        {
            return _shifts;
        }

        public ShiftModel GetShiftByDay(DateTime day)
        {
            ShiftModel? shiftByDate = _shifts.FirstOrDefault(shift => shift.DateTime.Date == day.Date);
            if (shiftByDate == null)
                throw new ShiftNotFoundException(string.Format("Shift not found for date {0}", day.ToString()));
            return shiftByDate;
        }

        public async Task LoadUpcomingDoctorDayshifts(int doctorID)
        {

            _shifts = await _shiftsDatabaseService.GetDoctorDaytimeShifts(doctorID);

        }


    }
}
