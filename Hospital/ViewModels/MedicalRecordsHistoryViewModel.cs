using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Hospital.Commands;
using Hospital.Managers;
using Hospital.Models;
using Hospital.Views;

namespace Hospital.ViewModels
{
    public class MedicalRecordsHistoryViewModel
    {
        private readonly MedicalRecordManager _medicalRecordManager;
        private readonly DocumentManager _documentManager;

        public List<MedicalRecordJointModel> MedicalRecords { get; private set; }

        public MedicalRecordsHistoryViewModel(int patientId, MedicalRecordManager medicalRecordManager, DocumentManager documentManager)
        {
            _medicalRecordManager = medicalRecordManager;
            _documentManager = documentManager;
            _medicalRecordManager.LoadMedicalRecordsForPatient(patientId).Wait();
            MedicalRecords = medicalRecordManager.MedicalRecords;
        }

        public void ShowMedicalRecordDetails(MedicalRecordJointModel medicalRecord)
        {
            Console.WriteLine($"Opening details for Medical Record ID: {medicalRecord.MedicalRecordId}");
            MedicalRecordDetailsView medicalRecordDetailsView = new MedicalRecordDetailsView(medicalRecord, _documentManager);
            medicalRecordDetailsView.Activate();
        }
    }
}
