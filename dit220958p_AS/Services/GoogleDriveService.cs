using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace dit220958p_AS.Services
{
    public class GoogleDriveService
    {
        private readonly string[] Scopes = { DriveService.Scope.DriveFile };
        private readonly string ApplicationName = "MyDatabaseBackup";
        private readonly string ServiceAccountKeyPath = Path.Combine(Directory.GetCurrentDirectory(), "AppData", "service-account.json");
        private readonly string FolderId = "1wJKw6Sc88OXILizVl40FJIWcCQIC90xo"; // Your Google Drive Folder ID

        public async Task UploadFile(string filePath)
        {
            try
            {
                Console.WriteLine($"🚀 Starting upload for file: {filePath}");

                if (!File.Exists(filePath))
                {
                    Console.WriteLine("❌ ERROR: Backup file does not exist. Upload canceled.");
                    return;
                }

                // ✅ Load Google Drive API credentials
                GoogleCredential credential;
                using (var stream = new FileStream(ServiceAccountKeyPath, FileMode.Open, FileAccess.Read))
                {
                    credential = GoogleCredential.FromStream(stream).CreateScoped(Scopes);
                }

                var service = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName,
                });

                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = Path.GetFileName(filePath),
                    Parents = new[] { FolderId }
                };

                using (var stream = new FileStream(filePath, FileMode.Open))
                {
                    var request = service.Files.Create(fileMetadata, stream, "application/octet-stream");
                    request.Fields = "id";
                    await request.UploadAsync();
                }

                Console.WriteLine($"✅ File uploaded successfully: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔥 ERROR: Google Drive upload failed - {ex.Message}");
            }
        }
    }
}
