using Hospital.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace Hospital.Managers
{
    public interface IShiftManager
    {
        Task LoadShifts(int doctorID);
        List<ShiftModel> GetShifts();
        ShiftModel GetShiftByDay(DateTime day);
        Task LoadUpcomingDoctorDayshifts(int doctorID);
    }
}
