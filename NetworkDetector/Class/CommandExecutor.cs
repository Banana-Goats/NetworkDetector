using System;
using System.Collections.Generic;
using System.Data;
using System.Configuration;
using Microsoft.Win32;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NetworkDetector.Helpers
{
    public class Command
    {
        public int ID { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string CommandText { get; set; }
        public string DesiredValue { get; set; }
        public string RegistryValueType { get; set; }
        public string CurrentValue { get; set; }
        public string AutoUpdate { get; set; }
    }

    public class CommandExecutor
    {
        private readonly string connectionString;

        public CommandExecutor()
        {
            
        }

        public async Task<List<Command>> LoadCommandsAsync(IProgress<string> progress = null)
        {
            List<Command> commands = new List<Command>();

            try
            {
                // Create an instance of the centralized database service.
                var dbService = new DatabaseService();

                // Define the query. No parameters are needed for this simple query.
                string query = "SELECT ID, Type, Description, Command, Value, RegistryValueType, AutoUpdate FROM Commands";

                // Execute the query asynchronously.
                DataTable dt = await dbService.ExecuteQueryAsync(query, null).ConfigureAwait(false);

                // Map each DataRow to a Command object.
                foreach (DataRow row in dt.Rows)
                {
                    Command cmd = new Command
                    {
                        ID = Convert.ToInt32(row["ID"]),
                        Type = row["Type"].ToString(),
                        Description = row["Description"].ToString(),
                        CommandText = row["Command"].ToString(),
                        DesiredValue = row["Value"]?.ToString(),
                        RegistryValueType = row["RegistryValueType"]?.ToString(),
                        AutoUpdate = row["AutoUpdate"]?.ToString(),
                        CurrentValue = GetCurrentValue(row["Type"].ToString(), row["Command"].ToString())
                    };
                    commands.Add(cmd);
                }
            }
            catch (Exception ex)
            {
                progress?.Report($"Error loading commands: {ex.Message}");
            }

            return commands;
        }


        private string GetCurrentValue(string type, string command)
        {
            if (type.Equals("Reg", StringComparison.OrdinalIgnoreCase))
            {
                return GetRegistryValue(command);
            }
            else
            {
                return "N/A"; // For other types, define default behavior
            }
        }

        public async Task<bool> ExecuteAllCommandsAsync(IProgress<string> progress = null)
        {
 
            SSID();

            List<Command> commands = await LoadCommandsAsync(progress);

            var commandsToExecute = commands
                .Where(cmd => !string.IsNullOrWhiteSpace(cmd.AutoUpdate) &&
                              cmd.AutoUpdate.Trim().Equals("Yes", StringComparison.OrdinalIgnoreCase))
                .ToList();

            bool allSuccessful = true;

            foreach (var cmd in commandsToExecute)
            {
                if (cmd.Type.Equals("Reg", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(cmd.DesiredValue))
                    {
                        progress?.Report($"Desired value for '{cmd.Description}' is not set.");
                        allSuccessful = false;
                        continue;
                    }

                    bool success = SetRegistryValue(cmd.CommandText, cmd.DesiredValue, cmd.RegistryValueType, progress, cmd.Description);
                    if (success)
                    {
                        progress?.Report($"Successfully set registry value for '{cmd.Description}'.");
                    }
                    else
                    {
                        progress?.Report($"Failed to set registry value for '{cmd.Description}'.");
                        allSuccessful = false;
                    }
                }
                else if (cmd.Type.Equals("CMD", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(cmd.CommandText))
                    {
                        progress?.Report($"Command for '{cmd.Description}' is not set.");
                        allSuccessful = false;
                        continue;
                    }

                    bool success = await ExecuteMultipleCommandsAsAdminAsync(cmd.CommandText, cmd.Description, progress);
                    if (success)
                    {
                        progress?.Report($"Successfully executed CMD commands for '{cmd.Description}'.");
                    }
                    else
                    {
                        progress?.Report($"Failed to execute CMD commands for '{cmd.Description}'.");
                        allSuccessful = false;
                    }
                }
                else
                {
                    progress?.Report($"Unsupported Type: {cmd.Type} for '{cmd.Description}'.");
                    allSuccessful = false;
                }
            }

            return allSuccessful;
        }

        public async Task<bool> ExecuteMultipleCommandsAsAdminAsync(string commands, string description, IProgress<string> progress)
        {
            char delimiter = ';'; // Semicolon
            string[] commandList = commands.Split(new char[] { delimiter }, StringSplitOptions.RemoveEmptyEntries);
            bool allSuccessful = true;

            foreach (string cmd in commandList)
            {
                string trimmedCommand = cmd.Trim();
                if (string.IsNullOrWhiteSpace(trimmedCommand))
                    continue;

                bool success = await ExecuteSingleCommandAsAdminAsync(trimmedCommand, description, progress);
                if (!success)
                {
                    allSuccessful = false;
                    // Optionally, decide whether to continue executing remaining commands or break
                    // break;
                }
            }

            return allSuccessful;
        }

        public async Task<bool> ExecuteSingleCommandAsAdminAsync(string command, string description, IProgress<string> progress)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Report starting execution
                    progress?.Report($"Executing command: {command}");

                    // Initialize the process start info
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/C " + command, // /C carries out the command and then terminates
                        Verb = "runas", // Run as administrator
                        UseShellExecute = true, // Required for 'runas' verb
                        CreateNoWindow = false // Show the command prompt window
                    };

                    // Start the process
                    using (Process proc = Process.Start(psi))
                    {
                        proc.WaitForExit(); // Wait for the command to complete

                        bool isSuccess = proc.ExitCode == 0; // Determine success based on exit code
                        if (isSuccess)
                        {
                            progress?.Report($"Command executed successfully: {command}");
                        }
                        else
                        {
                            progress?.Report($"Command failed with exit code {proc.ExitCode}: {command}");
                        }
                        return isSuccess;
                    }
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    // This exception is thrown when the user cancels the UAC prompt
                    if (ex.ErrorCode == -2147467259) // ERROR_CANCELLED
                    {
                        progress?.Report($"UAC prompt canceled for '{description}'.");
                    }
                    else
                    {
                        progress?.Report($"Win32Exception for '{description}': {ex.Message}");
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    progress?.Report($"Exception executing command '{description}': {ex.Message}");
                    return false;
                }
            });
        }

        public bool SetRegistryValue(string registryPath, string desiredValue, string registryValueType, IProgress<string> progress, string description)
        {
            try
            {
                string[] pathParts = registryPath.Split('\\');
                if (pathParts.Length < 2)
                {
                    progress?.Report($"Invalid registry path: {registryPath}");
                    return false;
                }

                string hiveName = pathParts[0];
                string subKey = string.Join("\\", pathParts, 1, pathParts.Length - 2);
                string valueName = pathParts[pathParts.Length - 1];

                RegistryKey baseKey = GetRegistryHive(hiveName);
                if (baseKey == null)
                {
                    progress?.Report($"Unknown registry hive: {hiveName}");
                    return false;
                }

                using (RegistryKey key = baseKey.OpenSubKey(subKey, true))
                {
                    if (key != null)
                    {
                        RegistryValueKind valueKind = GetRegistryValueKind(registryValueType);
                        object convertedValue = ConvertRegistryValue(desiredValue, valueKind);
                        key.SetValue(valueName, convertedValue, valueKind);
                        return true;
                    }
                    else
                    {
                        // Optionally, create the subkey if it doesn't exist
                        using (RegistryKey newKey = baseKey.CreateSubKey(subKey))
                        {
                            if (newKey != null)
                            {
                                RegistryValueKind valueKind = GetRegistryValueKind(registryValueType);
                                object convertedValue = ConvertRegistryValue(desiredValue, valueKind);
                                newKey.SetValue(valueName, convertedValue, valueKind);
                                return true;
                            }
                            else
                            {
                                progress?.Report($"Failed to create registry subkey: {subKey}");
                                return false;
                            }
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                progress?.Report($"Access denied when setting registry for '{description}'. Please run the application as an administrator.");
                return false;
            }
            catch (Exception ex)
            {
                progress?.Report($"Error setting registry for '{description}': {ex.Message}");
                return false;
            }
        }

        private string GetRegistryValue(string registryPath)
        {
            try
            {
                string[] pathParts = registryPath.Split('\\');
                if (pathParts.Length < 2)
                    return "Invalid Path";

                string hiveName = pathParts[0];
                string subKey = string.Join("\\", pathParts, 1, pathParts.Length - 2);
                string valueName = pathParts[pathParts.Length - 1];

                RegistryKey baseKey = GetRegistryHive(hiveName);
                if (baseKey == null)
                    return "Unknown Hive";

                using (RegistryKey key = baseKey.OpenSubKey(subKey))
                {
                    if (key != null)
                    {
                        object value = key.GetValue(valueName);
                        if (value != null)
                        {
                            RegistryValueKind valueKind = key.GetValueKind(valueName);
                            return ConvertRegistryValueToString(value, valueKind);
                        }
                        else
                        {
                            return "Not Set";
                        }
                    }
                    else
                    {
                        return "Not Set";
                    }
                }
            }
            catch
            {
                return "Error";
            }
        }

        private string ConvertRegistryValueToString(object value, RegistryValueKind valueKind)
        {
            try
            {
                switch (valueKind)
                {
                    case RegistryValueKind.DWord:
                    case RegistryValueKind.QWord:
                        return value.ToString();
                    case RegistryValueKind.Binary:
                        byte[] bytes = (byte[])value;
                        return BitConverter.ToString(bytes).Replace("-", " ");
                    case RegistryValueKind.String:
                    case RegistryValueKind.ExpandString:
                        return value.ToString();
                    case RegistryValueKind.MultiString:
                        string[] multiStrings = (string[])value;
                        return string.Join(", ", multiStrings);
                    default:
                        return value.ToString();
                }
            }
            catch
            {
                return "Error";
            }
        }

        private RegistryKey GetRegistryHive(string hiveName)
        {
            switch (hiveName.ToUpper())
            {
                case "HKEY_CLASSES_ROOT":
                    return Registry.ClassesRoot;
                case "HKEY_CURRENT_USER":
                    return Registry.CurrentUser;
                case "HKEY_LOCAL_MACHINE":
                    return Registry.LocalMachine;
                case "HKEY_USERS":
                    return Registry.Users;
                case "HKEY_CURRENT_CONFIG":
                    return Registry.CurrentConfig;
                default:
                    return null;
            }
        }

        private RegistryValueKind GetRegistryValueKind(string registryValueType)
        {
            switch (registryValueType.ToUpper())
            {
                case "DWORD":
                    return RegistryValueKind.DWord;
                case "QWORD":
                    return RegistryValueKind.QWord;
                case "BINARY":
                    return RegistryValueKind.Binary;
                case "EXPANDSTRING":
                    return RegistryValueKind.ExpandString;
                case "MULTISTRING":
                    return RegistryValueKind.MultiString;
                case "STRING":
                    return RegistryValueKind.String;
                default:
                    return RegistryValueKind.String; // Default to String if type is unknown
            }
        }

        private object ConvertRegistryValue(string desiredValue, RegistryValueKind valueKind)
        {
            try
            {
                switch (valueKind)
                {
                    case RegistryValueKind.DWord:
                        if (int.TryParse(desiredValue, out int intValue))
                            return intValue;
                        else
                            return 0; // Default value
                    case RegistryValueKind.QWord:
                        if (long.TryParse(desiredValue, out long longValue))
                            return longValue;
                        else
                            return 0L; // Default value
                    case RegistryValueKind.Binary:
                        // Convert hex string to byte array (e.g., "DE AD BE EF")
                        string[] hexValues = desiredValue.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        byte[] bytes = new byte[hexValues.Length];
                        for (int i = 0; i < hexValues.Length; i++)
                        {
                            bytes[i] = Convert.ToByte(hexValues[i], 16);
                        }
                        return bytes;
                    case RegistryValueKind.String:
                    case RegistryValueKind.ExpandString:
                        return desiredValue;
                    case RegistryValueKind.MultiString:
                        // Assuming values are separated by semicolons (e.g., "Value1;Value2")
                        return desiredValue.Split(';');
                    default:
                        return desiredValue;
                }
            }
            catch
            {
                // In case of conversion failure, return the original string
                return desiredValue;
            }
        }

        private void SSID()
        {
            // Define your Wi-Fi network details.
            string ssid = "Ableworld-Store";
            string keyMaterial = "Reactive-Swordfish";

            // Build the XML profile content.
            string xmlContent = $@"<?xml version=""1.0""?>
<WLANProfile xmlns=""http://www.microsoft.com/networking/WLAN/profile/v1"">
  <name>{ssid}</name>
  <SSIDConfig>
    <SSID>
      <name>{ssid}</name>
    </SSID>
  </SSIDConfig>
  <connectionType>ESS</connectionType>
  <connectionMode>auto</connectionMode>
  <MSM>
    <security>
      <authEncryption>
        <authentication>WPA2PSK</authentication>
        <encryption>AES</encryption>
        <useOneX>false</useOneX>
      </authEncryption>
      <sharedKey>
        <keyType>passPhrase</keyType>
        <protected>false</protected>
        <keyMaterial>{keyMaterial}</keyMaterial>
      </sharedKey>
    </security>
  </MSM>
  <MacRandomization xmlns=""http://www.microsoft.com/networking/WLAN/profile/v3"">
    <enableRandomization>false</enableRandomization>
  </MacRandomization>
</WLANProfile>";

            // Create a temporary file path.
            string tempPath = Path.Combine(Path.GetTempPath(), $"{ssid}.xml");

            try
            {
                // Write the XML content to the temporary file.
                File.WriteAllText(tempPath, xmlContent);

                // Build the netsh command.
                // The command is: netsh wlan add profile filename="tempPath" user=all
                ProcessStartInfo psi = new ProcessStartInfo("netsh", $"wlan add profile filename=\"{tempPath}\" user=all")
                {
                    Verb = "runas",           // Run as administrator.
                    CreateNoWindow = true,
                    UseShellExecute = true
                };

                // Start the process and wait for it to finish.
                using (Process process = Process.Start(psi))
                {
                    process.WaitForExit();
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error creating Wi-Fi profile: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                try
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
                catch
                {
                }
            }
        }
        
    }
}
