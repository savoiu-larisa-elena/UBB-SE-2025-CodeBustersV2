﻿using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Hospital.Managers;
using Hospital.ViewModels;
using System;
using Microsoft.UI;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Appointments;
using Hospital.Models;
using Microsoft.UI.Xaml.Input;

namespace Hospital.Views
{
    public sealed partial class DoctorScheduleView : Window
    {
        public DoctorScheduleViewModel ViewModel => _viewModel;
        private readonly DoctorScheduleViewModel _viewModel;
        public ObservableCollection<TimeSlotModel> _dailySchedule { get; private set; }

        private MedicalRecordManager _medicalRecordManager;
        private DocumentManager _documentManager;

        public DoctorScheduleView(
            Managers.AppointmentManager appointmentManagerModel,
            ShiftManager shiftManagerModel,
            MedicalRecordManager medicalRecordManager,
            DocumentManager documentManager)
        {
            _viewModel = new DoctorScheduleViewModel(appointmentManagerModel, shiftManagerModel);
            _medicalRecordManager = medicalRecordManager;
            _documentManager = documentManager;
            _dailySchedule = new ObservableCollection<TimeSlotModel>();

            LoadInitialCalendarRange();
            this.InitializeComponent();
            ((FrameworkElement)this.Content).DataContext = _viewModel;
            DailyScheduleList.ItemsSource = _dailySchedule;
            DoctorSchedule.CalendarViewDayItemChanging += CalendarView_DayItemChanging;
            LoadShiftsAndRefreshCalendar();
        }

        private async void LoadShiftsAndRefreshCalendar()
        {
            try
            {
                await _viewModel.LoadShiftsForDoctor();

                if (_viewModel.Shifts == null || !_viewModel.Shifts.Any()) return;

                DoctorSchedule.SelectedDates.Clear();
                DoctorSchedule.InvalidateArrange();
                DoctorSchedule.InvalidateMeasure();
                DoctorSchedule.UpdateLayout();

                await RecreateCalendarView();
            }
            catch (Exception e)
            {
                await ShowErrorDialog($"Failed to load doctor shifts.\n\n{e.Message}");
            }
        }


        private async Task RecreateCalendarView()
        {
            try
            {
                CalendarView newCalendar = new CalendarView
                {
                    MinDate = _viewModel.MinimumDateForSelectingAppointment.DateTime,
                    MaxDate = _viewModel.MaximumDateForSelectingAppointment.DateTime,
                    SelectionMode = CalendarViewSelectionMode.Single,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Top,
                    BorderBrush = new SolidColorBrush(Colors.Green),
                    BorderThickness = new Thickness(2)
                };

                newCalendar.CalendarViewDayItemChanging += CalendarView_DayItemChanging;
                newCalendar.SelectedDatesChanged += CalendarView_SelectedDatesChanged;

                CalendarContainer.Children.Remove(DoctorSchedule);
                DoctorSchedule = newCalendar;
                CalendarContainer.Children.Insert(0, DoctorSchedule);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error recreating calendar: " + ex.Message);
                await ShowErrorDialog("Calendar failed to reload.");
            }
        }

        private void LoadInitialCalendarRange()
        {
            var today = DateTime.Today;
            _viewModel.MinimumDateForSelectingAppointment = new DateTimeOffset(new DateTime(today.Year, today.Month, 1));
            _viewModel.MaximumDateForSelectingAppointment = _viewModel.MinimumDateForSelectingAppointment.AddMonths(1).AddDays(-1);
        }

        private async void CalendarView_SelectedDatesChanged(CalendarView sender, CalendarViewSelectedDatesChangedEventArgs args)
        {
            try
            {
                if (args.AddedDates.Count > 0)
                {
                    DateTime selectedDate = args.AddedDates[0].DateTime.Date;
                    await _viewModel.OnDateSelected(selectedDate);
                }
            }
            catch (Exception e)
            {
                await ShowErrorDialog($"Error selecting date: {e.Message}");
            }
        }

        private void CalendarView_DayItemChanging(CalendarView sender, CalendarViewDayItemChangingEventArgs args)
        {
            if (_viewModel.ShiftDates == null || !_viewModel.ShiftDates.Any()) return;
            var date = args.Item.Date.Date;

            if (_viewModel.ShiftDates.Any(d => d.Date == date.Date))
            {
                args.Item.Background = new SolidColorBrush(Colors.LightGreen);
            }
        }

        private async Task ShowErrorDialog(string message)
        {
            try
            {
                ContentDialog errorDialog = new ContentDialog
                {
                    Title = "Error",
                    Content = message,
                    CloseButtonText = "OK",
                    RequestedTheme = ElementTheme.Default
                };

                if (this.Content is FrameworkElement rootElement)
                {
                    errorDialog.XamlRoot = rootElement.XamlRoot;
                }
                else
                {
                    Console.WriteLine("Error: Unable to find a valid XamlRoot.");
                    return;
                }

                await errorDialog.ShowAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Critical error while showing error dialog: {ex.Message}");
            }
        }


        private void DailyScheduleList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            return;

        }

        private async void TimeSlot_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is StackPanel panel && panel.DataContext is TimeSlotModel slot)
            {
                if (_viewModel.OpenDetailsCommand?.CanExecute(slot) == true)
                {
                    _viewModel.OpenDetailsCommand.Execute(slot);
                    var selected = _viewModel.SelectedSlot;
                    if (selected != null)
                    {
                        if (!string.IsNullOrEmpty(selected.Appointment))
                        {
                            var appointment = _viewModel.Appointments.FirstOrDefault(a => a.ProcedureName == selected.Appointment);
                            string message = $"Appointment: {appointment.ProcedureName}\n" +
                                             $"Date: {appointment.DateAndTime}\n" +
                                             $"Doctor: {appointment.DoctorName}\n" +
                                             $"Patient: {appointment.PatientName}\n";

                            ContentDialog dialog = new ContentDialog
                            {
                                Title = "Appointment Info",
                                XamlRoot = this.Content.XamlRoot,
                                RequestedTheme = ElementTheme.Default
                            };

                            StackPanel dialogContent = new StackPanel();
                            dialogContent.Children.Add(new TextBlock
                            {
                                Text = message,
                                TextWrapping = TextWrapping.Wrap,
                                Margin = new Thickness(0, 0, 0, 20)
                            });

                            StackPanel buttonPanel = new StackPanel
                            {
                                Orientation = Orientation.Horizontal,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                Spacing = 10
                            };

                            Button createRecordBtn = new Button { Content = "Create Medical Record" };
                            createRecordBtn.Click += (s, args) =>
                            {
                                dialog.Hide();

                                MedicalRecordCreationFormViewModel viewModel = new MedicalRecordCreationFormViewModel(_medicalRecordManager, _documentManager);
                                CreateMedicalRecordForm medicalRecordCreateView = new CreateMedicalRecordForm(viewModel, appointment);
                                medicalRecordCreateView.Activate();
                            };

                            Button viewProfileBtn = new Button { Content = "View Profile" };
                            viewProfileBtn.Click += (s, args) =>
                            {
                                dialog.Hide();

                                int patientId = appointment.PatientId;
                                // TODO: Use the patientId to open the patient's profile
                            };

                            Button viewHistoryBtn = new Button { Content = "Medical Records History" };
                            viewHistoryBtn.Click += (s, args) =>
                            {
                                dialog.Hide();

                                MedicalRecordsHistoryView recordsHistoryView = new MedicalRecordsHistoryView(appointment.PatientId, _medicalRecordManager, _documentManager);
                                recordsHistoryView.Activate();
                            };

                            buttonPanel.Children.Add(createRecordBtn);
                            buttonPanel.Children.Add(viewProfileBtn);
                            buttonPanel.Children.Add(viewHistoryBtn);

                            dialogContent.Children.Add(buttonPanel);
                            dialog.Content = dialogContent;
                            dialog.CloseButtonText = "Close";

                            await dialog.ShowAsync();
                        }
                        else if (selected.HighlightColor.Color == Colors.Green)
                        {
                            ContentDialog dialog = new ContentDialog
                            {
                                Title = "Appointment Info",
                                Content = $"No appointments scheduled in this shift slot.\nTime: {selected.Time}",
                                CloseButtonText = "OK",
                                XamlRoot = this.Content.XamlRoot,
                                RequestedTheme = ElementTheme.Default
                            };

                            await dialog.ShowAsync();
                        }

                        _viewModel.SelectedSlot = null;
                    }
                }
            }
        }


    }

}