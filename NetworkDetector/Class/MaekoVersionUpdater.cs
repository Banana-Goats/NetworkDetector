using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;

namespace NetworkDetector.Class
{
    public class MaekoVersionUpdater
    {
        private readonly string _connectionString;
        private readonly Action<string> _logError;
        private readonly Action<string> _appendLog;

        public MaekoVersionUpdater(string connectionString, Action<string> logError, Action<string> appendLog)
        {
            _connectionString = connectionString;
            _logError = logError;
            _appendLog = appendLog;
        }

        public async Task CheckAndUpdateMaekoVersionsAsync(string machineName)
        {
            string maekoFolderPath = @"C:\Maeko";
            if (!Directory.Exists(maekoFolderPath))
            {
                _logError($"The folder {maekoFolderPath} does not exist.");
                return;
            }

            try
            {
                // Get all zip files in the folder
                var zipFiles = Directory.GetFiles(maekoFolderPath, "*.zip");

                // Variables to hold the latest versions
                DateTime? latestCommsDate = null;
                DateTime? latestTillDate = null;

                // Iterate through files to find the latest versions
                foreach (var filePath in zipFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(filePath);

                    if (fileName.StartsWith("COMMS_BR"))
                    {
                        string datePart = ExtractDateFromFileName(fileName, "COMMS_BR-");
                        if (DateTime.TryParse(datePart, out DateTime parsedDate) &&
                            (latestCommsDate == null || parsedDate > latestCommsDate))
                        {
                            latestCommsDate = parsedDate;
                        }
                    }
                    else if (fileName.StartsWith("MAEKOCONNECT_AW"))
                    {
                        string datePart = ExtractDateFromFileName(fileName, "MAEKOCONNECT_AW-");
                        if (DateTime.TryParse(datePart, out DateTime parsedDate) &&
                            (latestTillDate == null || parsedDate > latestTillDate))
                        {
                            latestTillDate = parsedDate;
                        }
                    }
                }

                // Update SQL table if latest versions are found
                if (latestCommsDate != null || latestTillDate != null)
                {
                    // Instantiate the DatabaseService helper.
                    // If you have updated DatabaseService to accept a connection string, pass _connectionString here.
                    var dbService = new DatabaseService();

                    string updateQuery = @"
                UPDATE TBPC
                SET CommsVersion = @CommsVersion,
                    TillVersion = @TillVersion
                WHERE Name = @MachineName";

                    var parameters = new Dictionary<string, object>
            {
                { "@MachineName", machineName },
                { "@CommsVersion", latestCommsDate.HasValue ? (object)latestCommsDate.Value : DBNull.Value },
                { "@TillVersion", latestTillDate.HasValue ? (object)latestTillDate.Value : DBNull.Value }
            };

                    int rowsAffected = await dbService.ExecuteNonQueryAsync(updateQuery, parameters);
                    if (rowsAffected > 0)
                    {
                        _appendLog($"Successfully updated versions for {machineName}. " +
                                   $"CommsVersion = {latestCommsDate?.ToString("yyyy-MM-dd") ?? "null"}, " +
                                   $"TillVersion = {latestTillDate?.ToString("yyyy-MM-dd") ?? "null"}");
                    }
                    else
                    {
                        _appendLog($"No rows updated for {machineName}. Machine name may not exist in the database.");
                    }
                }
                else
                {
                    _appendLog("No valid COMMS_BR or MAEKOCONNECT_AW files found with dates.");
                }
            }
            catch (Exception ex)
            {
                _logError($"Error checking or updating Maeko versions: {ex.Message}");
            }
        }

        private string ExtractDateFromFileName(string fileName, string prefix)
        {
            try
            {
                int prefixLength = prefix.Length;
                int dateLength = 10; // Format: YYYY-MM-DD
                if (fileName.Length >= prefixLength + dateLength)
                {
                    string extractedDate = fileName.Substring(prefixLength, dateLength);
                    if (DateTime.TryParse(extractedDate, out DateTime parsedDate))
                    {
                        return parsedDate.ToString("yyyy-MM-dd"); // Ensure consistent parsing
                    }
                }
            }
            catch (Exception ex)
            {
                _logError($"Error extracting date from file name {fileName}: {ex.Message}");
            }
            return null;
        }
    }
}
