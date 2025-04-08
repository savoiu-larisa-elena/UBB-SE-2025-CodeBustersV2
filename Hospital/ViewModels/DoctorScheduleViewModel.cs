using Hospital.Models;
using Hospital.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hospital.Managers;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Hospital.Commands;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Hospital.Views;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System.Reflection.Metadata;

namespace Hospital.ViewModels
{
    public class DoctorScheduleViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        // Managers
        private readonly AppointmentManager _appointmentManager;
        private readonly ShiftManager _shiftManager;

        // Observable Collections
        public ObservableCollection<TimeSlotModel> DailySchedule { get; set; } = new();

        public ObservableCollection<DateTimeOffset> ShiftDates { get; set; }

        public List<AppointmentJointModel> Appointments { get; set; }
        public List<ShiftModel> Shifts { get; set; }

        public ICommand OpenDetailsCommand { get; set; }


        private const int TimeSlotIntervalInMinutes = 30; // The time interval for each slot (in minutes)

        private const int DefaultDoctorId = 1; // Default ID for testing
        public int DoctorId { get; set; } = DefaultDoctorId;


        private DateTimeOffset _minimumDateForSelectingAppointment;
        public DateTimeOffset MinimumDateForSelectingAppointment
        {
            get => _minimumDateForSelectingAppointment;
            set
            {
                _minimumDateForSelectingAppointment = value;
                OnPropertyChanged();
            }
        }

        private DateTimeOffset _maximumDateForSelectingAppointment;
        public DateTimeOffset MaximumDateForSelectingAppointment
        {
            get => _maximumDateForSelectingAppointment;
            set
            {
                _maximumDateForSelectingAppointment = value;
                OnPropertyChanged();
            }
        }

        private TimeSlotModel _selectedSlot;
        public TimeSlotModel SelectedSlot
        {
            get => _selectedSlot;
            set
            {
                _selectedSlot = value;
                OnPropertyChanged();
            }
        }

        public DoctorScheduleViewModel(AppointmentManager appointmentManager, ShiftManager shiftManager)
        {
            _appointmentManager = appointmentManager;
            _shiftManager = shiftManager;
            Appointments = new List<AppointmentJointModel>();
            Shifts = new List<ShiftModel>();
            ShiftDates = new ObservableCollection<DateTimeOffset>();

            OpenDetailsCommand = new RelayCommand(OpenAppointmentForDoctor);
        }

        private void OpenAppointmentForDoctor(object objectTimeSlot)
        {
            if (objectTimeSlot is not TimeSlotModel selectedSlot) return;

            if (string.IsNullOrEmpty(selectedSlot.Appointment) && selectedSlot.HighlightColor.Color == Colors.Transparent)
                return;

            SelectedSlot = selectedSlot;
        }



        public async Task LoadAppointmentsForDoctor()
        {
            try
            {
                await _appointmentManager.LoadAppointmentsForDoctor(DoctorId);
                var appointments = _appointmentManager.Appointments;

                Appointments.Clear();
                foreach (var appointment in appointments)
                {
                    Appointments.Add(appointment);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Error loading appointments: {exception.Message}");
                throw new Exception($"Error loading appointments: {exception.Message}");
            }
        }

        public async Task LoadShiftsForDoctor()
        {
            try
            {
                await _shiftManager.LoadShifts(this.DoctorId);
                Shifts = _shiftManager.GetShifts();

                ShiftDates.Clear();
                foreach (var shift in Shifts)
                {
                    var shiftStartDate = shift.DateTime.Date;
                    var shiftEndDate = shift.DateTime.Date;

                    if (shift.EndTime <= shift.StartTime)
                    {
                        shiftEndDate = shiftEndDate.AddDays(1);
                    }

                    ShiftDates.Add(new DateTimeOffset(shiftStartDate, TimeSpan.Zero));

                    if (shiftEndDate > shiftStartDate)
                    {
                        ShiftDates.Add(new DateTimeOffset(shiftEndDate, TimeSpan.Zero));
                    }
                }

                OnPropertyChanged(nameof(ShiftDates));
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Error loading shifts: {exception.Message}");
                throw new Exception($"Error loading shifts: {exception.Message}");
            }
        }

        public async Task OnDateSelected(DateTime date)
        {
            DailySchedule.Clear();
            try
            {
                await _appointmentManager.LoadDoctorAppointmentsOnDate(DoctorId, date);
                Appointments = _appointmentManager.Appointments;
                await _shiftManager.LoadShifts(DoctorId);
                Shifts = _shiftManager.GetShifts();

                var slots = GenerateTimeSlots(date);
                foreach (var slot in slots)
                {
                    DailySchedule.Add(slot);
                }
                OnPropertyChanged(nameof(DailySchedule));
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Database access failed: {exception.Message}");
            }
        }

        private List<TimeSlotModel> GenerateTimeSlots(DateTime date)
        {
            List<TimeSlotModel> slots = new();
            DateTime startTime = date.Date;
            DateTime endTime = startTime.AddDays(1);
            const string TimeFormat = "hh:mm tt";

            var selectedAppointments = Appointments
                .Where(a => a.DateAndTime.Date == date.Date)
                .ToList();

            var selectedShifts = Shifts
                .Where(shift =>
                {
                    var shiftStart = shift.DateTime.Date + shift.StartTime;
                    var shiftEnd = shift.DateTime.Date + shift.EndTime;

                    if (shift.EndTime <= shift.StartTime)
                        shiftEnd = shiftEnd.AddDays(1);

                    return shiftStart < endTime && shiftEnd > startTime;
                })
                .ToList();

            while (startTime < endTime)
            {
                var slot = new TimeSlotModel
                {
                    TimeSlot = startTime,
                    Time = startTime.ToString(TimeFormat),
                    Appointment = "",
                    HighlightColor = new SolidColorBrush(Colors.Transparent)
                };

                SolidColorBrush highlightColor = new SolidColorBrush(Colors.Transparent);

                bool isInShift = selectedShifts.Any(shift =>
                {
                    DateTime shiftStart = shift.DateTime.Date + shift.StartTime;
                    DateTime shiftEnd = shift.DateTime.Date + shift.EndTime;
                    if (shift.EndTime <= shift.StartTime)
                        shiftEnd = shiftEnd.AddDays(1);

                    return startTime >= shiftStart && startTime < shiftEnd;
                });

                var matchingAppointment = selectedAppointments.FirstOrDefault(appointment =>
                    appointment.DateAndTime == startTime && isInShift);

                if (matchingAppointment != null)
                {
                    slot.Appointment = matchingAppointment.ProcedureName;
                    highlightColor = new SolidColorBrush(Colors.Orange);
                }
                else if (isInShift)
                {
                    highlightColor = new SolidColorBrush(Colors.Green);
                }

                slot.HighlightColor = highlightColor;
                slots.Add(slot);
                startTime = startTime.AddMinutes(TimeSlotIntervalInMinutes);
            }

            return slots;
        }



    }
}
