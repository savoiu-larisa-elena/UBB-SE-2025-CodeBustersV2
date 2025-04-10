using Hospital.Configs;
using Hospital.Models;
using Hospital.Exceptions;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Hospital.DatabaseServices
{
    public class AppointmentsDatabaseService : IAppointmentsDatabaseService
    {
        private readonly ApplicationConfiguration _configuration;
        public AppointmentsDatabaseService()
        {
            _configuration = ApplicationConfiguration.GetInstance();
        }

        public async Task<bool> AddAppointmentToDataBase(AppointmentModel appointment)
        {
            // Validate that the appointment is not in the past
            if (appointment.DateAndTime < DateTime.Now)
            {
                throw new InvalidAppointmentException("Cannot create appointments in the past");
            }

            const string insertAppointmentQuery =
              "INSERT INTO Appointments (PatientId, DoctorId, DateAndTime, ProcedureId, Finished) " +
              "VALUES (@PatientId, @DoctorId, @DateAndTime, @ProcedureId, @Finished)";

            try
            {
                using SqlConnection sqlConnection = new SqlConnection(_configuration.DatabaseConnection);

                // Open the database connection asynchronously
                await sqlConnection.OpenAsync().ConfigureAwait(false);
                Console.WriteLine("Connection established successfully.");

                // Create a command to execute the SQL query
                using SqlCommand sqlCommand = new SqlCommand(insertAppointmentQuery, sqlConnection);

                // Add the parameters to the query with values from the appointment object
                sqlCommand.Parameters.AddWithValue("@PatientId", appointment.PatientId);
                sqlCommand.Parameters.AddWithValue("@DoctorId", appointment.DoctorId);
                sqlCommand.Parameters.AddWithValue("@DateAndTime", appointment.DateAndTime);
                sqlCommand.Parameters.AddWithValue("@ProcedureId", appointment.ProcedureId);
                sqlCommand.Parameters.AddWithValue("@Finished", appointment.Finished);

                // Execute the query asynchronously and check how many rows were affected
                int numberOfRowsAffectedByAddSqlCommand = await sqlCommand.ExecuteNonQueryAsync().ConfigureAwait(false);

                sqlConnection.Close();

                // If at least one row was affected, the insert was successful
                return numberOfRowsAffectedByAddSqlCommand > 0;
            }
            catch (SqlException sqlException)
            {
                throw new DatabaseOperationException($"SQL Error: {sqlException.Message}");
            }
            catch (Exception exception)
            {
                throw new Exception($"General Error: {exception.Message}");
            }
        }

        public async Task<List<AppointmentJointModel>> GetAllAppointments()
        {
            const string selectAppointmentsQuery = @"SELECT 
                    a.AppointmentId,
                    a.Finished,
                    a.DateAndTime,
                    d.DepartmentId,
                    d.DepartmentName,
                    doc.DoctorId,
                    u1.Name as DoctorName,
                    p.PatientId,
                    u2.Name as PatientName,
                    pr.ProcedureId,
                    pr.ProcedureName,
                    pr.ProcedureDuration
                FROM Appointments a
                JOIN Doctors doc ON a.DoctorId = doc.DoctorId
                JOIN Users u1 ON doc.UserId = u1.UserId
                JOIN Departments d ON doc.DepartmentId = d.DepartmentId
                JOIN Patients p ON a.PatientId = p.PatientId
                JOIN Users u2 ON p.UserId = u2.UserId
                JOIN Procedures pr ON a.ProcedureId = pr.ProcedureId
                ORDER BY a.AppointmentId;";

            using DataTable appointmentsDataTable = new DataTable();

            try
            {
                using SqlConnection sqlConnection = new SqlConnection(_configuration.DatabaseConnection);
                // Open the database connection asynchronously.
                await sqlConnection.OpenAsync().ConfigureAwait(false);
                Console.WriteLine("Connection established successfully.");

                // Create a command to execute the SQL query.
                using SqlCommand sqlCommand = new SqlCommand(selectAppointmentsQuery, sqlConnection);

                // Execute the command and obtain a SqlDataReader.
                using SqlDataReader reader = await sqlCommand.ExecuteReaderAsync().ConfigureAwait(false);

                // Load the results into a DataTable.
                await Task.Run(() => appointmentsDataTable.Load(reader)).ConfigureAwait(false);

                // Create a list to store the AppointmentJointModel objects.
                List<AppointmentJointModel> appointments = new List<AppointmentJointModel>();

                foreach (DataRow row in appointmentsDataTable.Rows)
                {
                    appointments.Add(new AppointmentJointModel(
                      (int)row["AppointmentId"],
                      (bool)row["Finished"],
                      (DateTime)row["DateAndTime"],
                      (int)row["DepartmentId"],
                      (string)row["DepartmentName"],
                      (int)row["DoctorId"],
                      (string)row["DoctorName"],
                      (int)row["PatientId"],
                      (string)row["PatientName"],
                      (int)row["ProcedureId"],
                      (string)row["ProcedureName"],
                      (TimeSpan)row["ProcedureDuration"]
                    ));
                }

                sqlConnection.Close();

                return appointments;
            }
            catch (SqlException sqlException)
            {
                Console.WriteLine($"SQL Error: {sqlException.Message}");
                return new List<AppointmentJointModel>();
            }
            catch (Exception exception)
            {
                Console.WriteLine($"General Error: {exception.Message}");
                return new List<AppointmentJointModel>();
            }
        }

        public async Task<List<AppointmentJointModel>> GetAppointmentsForPatient(int patientId)
        {
            const string selectAppointmentsByPatientIdQuery = @"SELECT 
                    a.AppointmentId,
                    a.Finished,
                    a.DateAndTime,
                    d.DepartmentId,
                    d.DepartmentName,
                    doc.DoctorId,
                    u1.Name as DoctorName,
                    p.PatientId,
                    u2.Name as PatientName,
                    pr.ProcedureId,
                    pr.ProcedureName,
                    pr.ProcedureDuration
                FROM Appointments a
                JOIN Doctors doc ON a.DoctorId = doc.DoctorId
                JOIN Users u1 ON doc.UserId = u1.UserId
                JOIN Departments d ON doc.DepartmentId = d.DepartmentId
                JOIN Patients p ON a.PatientId = p.PatientId
                JOIN Users u2 ON p.UserId = u2.UserId
                JOIN Procedures pr ON a.ProcedureId = pr.ProcedureId
                WHERE p.PatientId = @PatientId
                ORDER BY a.DateAndTime;";

            using DataTable appointmentsForPatientDataTable = new DataTable();

            try
            {
                using SqlConnection sqlConnection = new SqlConnection(_configuration.DatabaseConnection);
                await sqlConnection.OpenAsync().ConfigureAwait(false);
                Console.WriteLine($"Connected to DB. Fetching appointments for Patient ID: {patientId}");

                using SqlCommand sqlCommand = new SqlCommand(selectAppointmentsByPatientIdQuery, sqlConnection);
                sqlCommand.Parameters.AddWithValue("@PatientId", patientId);

                using SqlDataReader reader = await sqlCommand.ExecuteReaderAsync().ConfigureAwait(false);
                await Task.Run(() => appointmentsForPatientDataTable.Load(reader)).ConfigureAwait(false);

                List<AppointmentJointModel> appointmentsForPatient = new List<AppointmentJointModel>();
                Console.WriteLine($"Rows returned: {appointmentsForPatientDataTable.Rows.Count}");

                foreach (DataRow row in appointmentsForPatientDataTable.Rows)
                {
                    var appointment = new AppointmentJointModel(
                        (int)row["AppointmentId"],
                        (bool)row["Finished"],
                        (DateTime)row["DateAndTime"],
                        (int)row["DepartmentId"],
                        (string)row["DepartmentName"],
                        (int)row["DoctorId"],
                        (string)row["DoctorName"],
                        (int)row["PatientId"],
                        (string)row["PatientName"],
                        (int)row["ProcedureId"],
                        (string)row["ProcedureName"],
                        (TimeSpan)row["ProcedureDuration"]
                    );

                    Console.WriteLine($"Appointment found: {appointment.AppointmentId} - {appointment.DateAndTime}");
                    appointmentsForPatient.Add(appointment);
                }

                return appointmentsForPatient;
            }
            catch (SqlException sqlException)
            {
                Console.WriteLine($"SQL Error: {sqlException.Message}");
                return new List<AppointmentJointModel>();
            }
            catch (Exception exception)
            {
                Console.WriteLine($"General Error: {exception.Message}");
                return new List<AppointmentJointModel>();
            }
        }

        public async Task<List<AppointmentJointModel>> GetAppointmentsByDoctorAndDate(int doctorId, DateTime date)
        {
            if(doctorId < 0)
            {
                throw new DatabaseOperationException($"Doctor ID {doctorId} is invalid.");
            }
            if(date < DateTime.Now)
            {
                throw new InvalidAppointmentException($"Date {date} is in the past.");
            }
            const string selectAppointmentsByDoctorAndDateQuery = @"SELECT 
                    a.AppointmentId,
                    a.Finished,
                    a.DateAndTime,
                    d.DepartmentId,
                    d.DepartmentName,
                    doc.DoctorId,
                    u1.Name as DoctorName,
                    p.PatientId,
                    u2.Name as PatientName,
                    pr.ProcedureId,
                    pr.ProcedureName,
                    pr.ProcedureDuration
                FROM Appointments a
                JOIN Doctors doc ON a.DoctorId = doc.DoctorId
                JOIN Users u1 ON doc.UserId = u1.UserId
                JOIN Departments d ON doc.DepartmentId = d.DepartmentId
                JOIN Patients p ON a.PatientId = p.PatientId
                JOIN Users u2 ON p.UserId = u2.UserId
                JOIN Procedures pr ON a.ProcedureId = pr.ProcedureId
                WHERE a.DoctorId = @DoctorId
                  AND CONVERT(DATE, a.DateAndTime) = @Date
                ORDER BY a.DateAndTime;";

            using DataTable appointmentsByDoctorAndDateDataTable = new DataTable();

            try
            {
                using SqlConnection sqlConnection = new SqlConnection(_configuration.DatabaseConnection);

                // Open the database connection asynchronously
                await sqlConnection.OpenAsync().ConfigureAwait(false);
                Console.WriteLine("Connection established successfully.");

                // Create a command to execute the SQL query
                using SqlCommand sqlCommand = new SqlCommand(selectAppointmentsByDoctorAndDateQuery, sqlConnection);

                // Add parameters for filtering by doctor and date
                sqlCommand.Parameters.AddWithValue("@DoctorId", doctorId);
                sqlCommand.Parameters.AddWithValue("@Date", date.Date);

                // Get the results from running the command
                using SqlDataReader reader = await sqlCommand.ExecuteReaderAsync().ConfigureAwait(false);

                // Load the results into the DataTable
                await Task.Run(() => appointmentsByDoctorAndDateDataTable.Load(reader)).ConfigureAwait(false);

                // Create a list to store the AppointmentJointModel objects
                List<AppointmentJointModel> appointmentsByDoctorAndDate = new List<AppointmentJointModel>();

                foreach (DataRow row in appointmentsByDoctorAndDateDataTable.Rows)
                {
                    appointmentsByDoctorAndDate.Add(new AppointmentJointModel(
                      (int)row["AppointmentId"],
                      (bool)row["Finished"],
                      (DateTime)row["DateAndTime"],
                      (int)row["DepartmentId"],
                      (string)row["DepartmentName"],
                      (int)row["DoctorId"],
                      (string)row["DoctorName"],
                      (int)row["PatientId"],
                      (string)row["PatientName"],
                      (int)row["ProcedureId"],
                      (string)row["ProcedureName"],
                      (TimeSpan)row["ProcedureDuration"]
                    ));
                }

                sqlConnection.Close();

                return appointmentsByDoctorAndDate;
            }
            catch (SqlException sqlException)
            {
                throw new DatabaseOperationException($"SQL Error: {sqlException.Message}");
            }
            catch (Exception exception)
            {
                throw new Exception($"General Error: {exception.Message}");
            }
        }

        public async Task<List<AppointmentJointModel>> GetAppointmentsForDoctor(int doctorId)
        {
            const string selectAppointmentsForDoctorQuery = @"SELECT 
                    a.AppointmentId,
                    a.Finished,
                    a.DateAndTime,
                    d.DepartmentId,
                    d.DepartmentName,
                    doc.DoctorId,
                    u1.Name as DoctorName,
                    p.PatientId,
                    u2.Name as PatientName,
                    pr.ProcedureId,
                    pr.ProcedureName,
                    pr.ProcedureDuration
                FROM Appointments a
                JOIN Doctors doc ON a.DoctorId = doc.DoctorId
                JOIN Users u1 ON doc.UserId = u1.UserId
                JOIN Departments d ON doc.DepartmentId = d.DepartmentId
                JOIN Patients p ON a.PatientId = p.PatientId
                JOIN Users u2 ON p.UserId = u2.UserId
                JOIN Procedures pr ON a.ProcedureId = pr.ProcedureId
                WHERE a.DoctorId = @DoctorId
                ORDER BY a.DateAndTime;";

            using DataTable selectAppointmentsForDoctorDataTable = new DataTable();

            try
            {
                using SqlConnection sqlConnection = new SqlConnection(_configuration.DatabaseConnection);

                // Open the database connection asynchronously
                await sqlConnection.OpenAsync().ConfigureAwait(false);
                Console.WriteLine("Connection established successfully.");

                // Create a command to execute the SQL query
                using SqlCommand sqlCommand = new SqlCommand(selectAppointmentsForDoctorQuery, sqlConnection);

                // Add parameters for filtering by doctor
                sqlCommand.Parameters.AddWithValue("@DoctorId", doctorId);

                // Get the results from running the command
                using SqlDataReader reader = await sqlCommand.ExecuteReaderAsync().ConfigureAwait(false);

                // Load the results into the DataTable
                await Task.Run(() => selectAppointmentsForDoctorDataTable.Load(reader)).ConfigureAwait(false);

                // Create a list to store the AppointmentJointModel objects
                List<AppointmentJointModel> appointmentsForDoctor = new List<AppointmentJointModel>();
                foreach (DataRow row in selectAppointmentsForDoctorDataTable.Rows)
                {
                    appointmentsForDoctor.Add(new AppointmentJointModel(
                      (int)row["AppointmentId"],
                      (bool)row["Finished"],
                      (DateTime)row["DateAndTime"],
                      (int)row["DepartmentId"],
                      (string)row["DepartmentName"],
                      (int)row["DoctorId"],
                      (string)row["DoctorName"],
                      (int)row["PatientId"],
                      (string)row["PatientName"],
                      (int)row["ProcedureId"],
                      (string)row["ProcedureName"],
                      (TimeSpan)row["ProcedureDuration"]
                    ));
                }

                sqlConnection.Close();

                return appointmentsForDoctor;
            }
            catch (SqlException sqlException)
            {
                Console.WriteLine($"SQL Error: {sqlException.Message}");
                return new List<AppointmentJointModel>();
            }
            catch (Exception exception)
            {
                Console.WriteLine($"General Error: {exception.Message}");
                return new List<AppointmentJointModel>();
            }
        }

        public async Task<AppointmentJointModel> GetAppointment(int appointmentId)
        {
            string getAppointmentByAppointmentIdQuery = @"SELECT 
                    a.AppointmentId,
                    a.Finished,
                    a.DateAndTime,
                    d.DepartmentId,
                    d.DepartmentName,
                    doc.DoctorId,
                    u1.Name as DoctorName,
                    p.PatientId,
                    u2.Name as PatientName,
                    pr.ProcedureId,
                    pr.ProcedureName,
                    pr.ProcedureDuration
                FROM Appointments a
                JOIN Doctors doc ON a.DoctorId = doc.DoctorId
                JOIN Users u1 ON doc.UserId = u1.UserId
                JOIN Departments d ON doc.DepartmentId = d.DepartmentId
                JOIN Patients p ON a.PatientId = p.PatientId
                JOIN Users u2 ON p.UserId = u2.UserId
                JOIN Procedures pr ON a.ProcedureId = pr.ProcedureId
                WHERE a.AppointmentId = @AppointmentId;";

            using DataTable dt = new DataTable();
            try
            {
                using SqlConnection sqlConnection = new SqlConnection(_configuration.DatabaseConnection);
                await sqlConnection.OpenAsync().ConfigureAwait(false);

                Console.WriteLine("Connection established successfully.");
                using SqlCommand sqlCommand = new SqlCommand(getAppointmentByAppointmentIdQuery, sqlConnection);

                sqlCommand.Parameters.AddWithValue("@AppointmentId", appointmentId);

                using SqlDataReader reader = await sqlCommand.ExecuteReaderAsync().ConfigureAwait(false);
                await Task.Run(() => dt.Load(reader)).ConfigureAwait(false);

                AppointmentJointModel appointment = new AppointmentJointModel(
                    (int)dt.Rows[0]["AppointmentId"],
                    (bool)dt.Rows[0]["Finished"],
                    (DateTime)dt.Rows[0]["DateAndTime"],
                    (int)dt.Rows[0]["DepartmentId"],
                    (string)dt.Rows[0]["DepartmentName"],
                    (int)dt.Rows[0]["DoctorId"],
                    (string)dt.Rows[0]["DoctorName"],
                    (int)dt.Rows[0]["PatientId"],
                    (string)dt.Rows[0]["PatientName"],
                    (int)dt.Rows[0]["ProcedureId"],
                    (string)dt.Rows[0]["ProcedureName"],
                    (TimeSpan)dt.Rows[0]["ProcedureDuration"]
                );


                sqlConnection.Close();
                return appointment;
            }
            catch (SqlException sqlException)
            {
                Console.WriteLine($"SQL Error: {sqlException.Message}");
                return null;
            }
            catch (Exception exception)
            {
                Console.WriteLine($"General Error: {exception.Message}");
                return null;

            }

            throw new AppointmentNotFoundException($"Appointment not found for id {appointmentId}");
        }
        public async Task<bool> RemoveAppointmentFromDataBase(int appointmentId)
        {
            try
            {
                Console.WriteLine($"Checking if appointment ID {appointmentId} exists before deletion...");

                const string checkAppointmentExistsQuery = "SELECT COUNT(*) FROM Appointments WHERE AppointmentId = @AppointmentId";

                using SqlConnection sqlConnection = new SqlConnection(_configuration.DatabaseConnection);
                await sqlConnection.OpenAsync().ConfigureAwait(false);

                using SqlCommand checkAppointmentExistsCommand = new SqlCommand(checkAppointmentExistsQuery, sqlConnection);
                checkAppointmentExistsCommand.Parameters.AddWithValue("@AppointmentId", appointmentId);

                int appointmentExists = (int)await checkAppointmentExistsCommand.ExecuteScalarAsync().ConfigureAwait(false);

                if (appointmentExists == 0)
                {
                    throw new DatabaseOperationException($"Appointment ID {appointmentId} does NOT exist in DB. Throwing exception.");
                    throw new AppointmentNotFoundException($"Appointment {appointmentId} not found.");
                }

                Console.WriteLine($"Appointment ID {appointmentId} exists. Proceeding with deletion.");

                const string deleteAppointmentQuery = "DELETE FROM Appointments WHERE AppointmentId = @AppointmentId";
                using SqlCommand deleteAppointmentCommand = new SqlCommand(deleteAppointmentQuery, sqlConnection);

                deleteAppointmentCommand.Parameters.AddWithValue("@AppointmentId", appointmentId);

                int numberOfRowsAffectedByDeleteSqlCommand = await deleteAppointmentCommand.ExecuteNonQueryAsync().ConfigureAwait(false);

                if (numberOfRowsAffectedByDeleteSqlCommand > 0)
                {
                    Console.WriteLine($"Successfully deleted appointment ID {appointmentId}.");
                    return true;
                }
                else
                {
                    throw new DatabaseOperationException($"Deletion failed for appointment ID {appointmentId}. No rows affected.");
                    return false;
                }
            }
            catch (SqlException sqlException)
            {
                throw new DatabaseOperationException($"SQL Error while deleting appointment: {sqlException.Message}");
                return false;
            }
            catch (Exception exception)
            {
                throw new DatabaseOperationException($"General Error while deleting appointment: {exception.Message}");
                return false;
            }
        }



    }
}
