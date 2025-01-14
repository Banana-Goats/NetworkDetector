using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;

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
                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        await connection.OpenAsync();

                        string updateQuery = "UPDATE machinedata SET CommsVersion = @CommsVersion, TillVersion = @TillVersion WHERE MachineName = @MachineName";

                        using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                        {
                            updateCommand.Parameters.AddWithValue("@MachineName", machineName);
                            updateCommand.Parameters.AddWithValue(
                                "@CommsVersion",
                                latestCommsDate.HasValue ? latestCommsDate.Value.ToString("dd/MM/yyyy") : DBNull.Value
                            );
                            updateCommand.Parameters.AddWithValue(
                                "@TillVersion",
                                latestTillDate.HasValue ? latestTillDate.Value.ToString("dd/MM/yyyy") : DBNull.Value
                            );

                            int rowsAffected = await updateCommand.ExecuteNonQueryAsync();

                            if (rowsAffected > 0)
                            {
                                _appendLog($"Successfully updated versions for {machineName}.");
                            }
                            else
                            {
                                _appendLog($"No rows updated for {machineName}. Machine name may not exist in the database.");
                            }
                        }
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
