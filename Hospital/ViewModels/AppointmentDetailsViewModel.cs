﻿using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Hospital.Managers;
using Hospital.Models;
using Hospital.Commands;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Hospital.Exceptions;

namespace Hospital.ViewModels
{
    public class AppointmentDetailsViewModel : INotifyPropertyChanged
    {
        private readonly AppointmentManager _appointmentManager;
        private readonly Action _closeWindowAction;
        private readonly Action _refreshAppointmentsAction;
        private readonly AppointmentJointModel _appointment;

        public event PropertyChangedEventHandler PropertyChanged;

        public string AppointmentDate { get; private set; }

        public string DoctorName { get; private set; }

        public string Department { get; private set; }

        public string ProcedureName { get; private set; }

        public string ProcedureDuration { get; private set; }


        private bool _canCancelAppointment;

        public bool CanCancelAppointment
        {
            get => _canCancelAppointment;
            private set
            {
                if (_canCancelAppointment != value)
                {
                    _canCancelAppointment = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand CancelAppointmentCommand { get; }

        public AppointmentDetailsViewModel(
            AppointmentJointModel appointment,
            AppointmentManager appointmentManager,
            Action closeWindow,
            Action refreshAppointments)
        {
            _appointment = appointment;
            _appointmentManager = appointmentManager;
            _closeWindowAction = closeWindow;
            _refreshAppointmentsAction = refreshAppointments;

            AppointmentDate = appointment.DateAndTime.ToString("f");
            DoctorName = $"Dr. {appointment.DoctorName}";
            Department = appointment.DepartmentName;
            ProcedureName = appointment.ProcedureName;
            ProcedureDuration = $"{appointment.ProcedureDuration.TotalMinutes} minutes";

            // Make sure to update the eligibility before binding
            UpdateCancellationEligibility();
            OnPropertyChanged(nameof(CanCancelAppointment));

            CancelAppointmentCommand = new RelayCommand(
                async _ => await CancelAppointment(),
                _ => CanCancelAppointment
            );
        }


        private int _minimumHoursBeforeCancellation = 24;

        private void UpdateCancellationEligibility()
        {
            TimeSpan remainingTime = _appointment.DateAndTime.ToLocalTime() - DateTime.Now;
            CanCancelAppointment = remainingTime.TotalHours >= _minimumHoursBeforeCancellation;
        }

        public DateTime AppointmentDateTime => _appointment.DateAndTime;

        private async Task CancelAppointment()
        {
            try
            {
                bool removed = await _appointmentManager.RemoveAppointment(_appointment.AppointmentId);
                if (removed)
                {
                    _refreshAppointmentsAction?.Invoke();
                    _closeWindowAction?.Invoke();
                }
            }
            catch (Exception exception)
            {
                throw new CancellationNotAllowedException($"Failed to cancel appointment: {exception.Message}");
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
