//using ABI.System;
using Hospital.DatabaseServices;
using Hospital.Exceptions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO.Compression;
using System.IO;
using System.Threading.Tasks;
using DocumentModel = Hospital.Models.DocumentModel;


namespace Hospital.Managers
{
    public class DocumentManager
    {
        public List<DocumentModel> Documents { get; private set; }

        private readonly DocumentDatabaseService _documentDatabaseService;

        public DocumentManager(DocumentDatabaseService documentDatabaseService)
        {
            _documentDatabaseService = documentDatabaseService;
            Documents = new List<DocumentModel>();
        }

        public List<DocumentModel> GetDocuments()
        {
            return Documents;
        }

        public async Task AddDocumentToMedicalRecord(DocumentModel document)
        {
            try
            {
                bool success = await _documentDatabaseService.UploadDocumentToDataBase(document).ConfigureAwait(false);
                if (success)
                {
                    Documents.Add(document);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Error adding document: {exception.Message}");
            }
        }

        public void LoadDocuments(int medicalRecordId)
        {
            Documents = _documentDatabaseService.GetDocumentsByMedicalRecordId(medicalRecordId).Result;
        }

        public async Task DownloadDocuments(int patientId)
        {
            List<string> filePaths = new List<string>();
            foreach (DocumentModel document in Documents)
            {
                filePaths.Add(document.Files);
            }

            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (var filePath in filePaths)
                    {
                        if (System.IO.File.Exists(filePath))
                        {
                            var fileName = Path.GetFileName(filePath);
                            var entry = archive.CreateEntry(fileName, CompressionLevel.Fastest);

                            using (var entryStream = entry.Open())
                            {
                                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                                {
                                    await fileStream.CopyToAsync(entryStream);
                                }
                            }
                        }
                        else
                        {
                            throw new DocumentNotFoundException($"Document not found at path: {filePath}");
                        }
                    }
                }

                memoryStream.Seek(0, SeekOrigin.Begin);
                var zipFile = memoryStream.ToArray();

                // was just :  string zipFileName = $"Documents_{DateTime.Now.ToString("yyyyMMddHHmmss")}.zip";
                string zipFileName = GenerateZipFileName();
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", zipFileName);
                File.WriteAllBytes(path, zipFile);

                Process.Start("explorer.exe", "/select, " + path);
            }
        }

        // did not exist before
        private string GenerateZipFileName()
        {
            string timestampFormat = "yyyyMMddHHmmss";
            return $"Documents_{DateTime.Now.ToString(timestampFormat)}.zip";
        }
    }
}
