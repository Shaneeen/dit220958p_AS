using System;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;

namespace dit220958p_AS.Services
{
    public class BackupService
    {
        private readonly string _connectionString;
        private readonly string _backupFolderPath;

        public BackupService(string connectionString)
        {
            _connectionString = connectionString;
            _backupFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "AppData", "Backup");

            // Ensure backup folder exists
            if (!Directory.Exists(_backupFolderPath))
            {
                Directory.CreateDirectory(_backupFolderPath);
            }
        }

        public async Task<string> BackupDatabaseAsync()
        {
            string backupFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "AppData", "Backup");
            string backupFilePath = Path.Combine(backupFolderPath, $"DatabaseBackup-{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}.bak");

            try
            {
                if (!Directory.Exists(backupFolderPath))
                {
                    Directory.CreateDirectory(backupFolderPath);
                }

                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string backupQuery = $"BACKUP DATABASE [AceJobAgencyDB] TO DISK = '{backupFilePath}'";

                    using (SqlCommand cmd = new SqlCommand(backupQuery, conn))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                Console.WriteLine($"✅ Database Backup Created: {backupFilePath}");
                return backupFilePath;  // 🔥 Return the path so the caller can use it
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔥 ERROR: Backup failed - {ex.Message}");
                return null;  // Return null to indicate failure
            }
        }
    }
}
