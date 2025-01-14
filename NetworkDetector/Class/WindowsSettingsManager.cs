using System;
using System.Windows.Forms;
using Microsoft.Win32;

namespace NetworkDetector.Class
{
    public class WindowsSettingsManager
    {
        private readonly NetworkDetector _mainForm;

        public WindowsSettingsManager(NetworkDetector mainForm)
        {
            _mainForm = mainForm;
        }

        public void WindowsUpdate()
        {
            // Make sure we still use the form's "IsUserAdministrator()" check
            if (!_mainForm.IsUserAdministrator())
            {
                MessageBox.Show(
                    "This operation requires administrator privileges. Please run the application as an administrator.",
                    "Admin Access Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate",
                    writable: true))
                {
                    if (key == null)
                    {
                        DialogResult result = MessageBox.Show(
                            "Windows Update policy key does not exist. Create it?",
                            "Create Registry Key",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question
                        );

                        if (result == DialogResult.Yes)
                        {
                            using (RegistryKey newKey = Registry.LocalMachine.CreateSubKey(
                                @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate"))
                            {
                                newKey.SetValue("TargetReleaseVersion", 1, RegistryValueKind.DWord);
                                newKey.SetValue("TargetReleaseVersionInfo", "24H2", RegistryValueKind.String);
                                newKey.SetValue("ProductVersion", "Windows 11", RegistryValueKind.String);
                                newKey.SetValue("AllowOptionalContent", 1, RegistryValueKind.DWord);
                            }
                        }
                    }
                    else
                    {
                        key.SetValue("TargetReleaseVersion", 1, RegistryValueKind.DWord);
                        key.SetValue("TargetReleaseVersionInfo", "24H2", RegistryValueKind.String);
                        key.SetValue("ProductVersion", "Windows 11", RegistryValueKind.String);
                        key.SetValue("AllowOptionalContent", 1, RegistryValueKind.DWord);
                    }
                }
            }
            catch (Exception ex)
            {
                // LogError is on the main form, so use that
                _mainForm.LogError($"An error occurred: {ex.Message}");

                MessageBox.Show(
                    $"An error occurred: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        public void CheckGpoSettings()
        {
            UpdateOptionalUpdatesCheckBox();
            UpdateProductVersionAndReleaseInfo();
        }

        private void UpdateOptionalUpdatesCheckBox()
        {
            const string subKeyPath = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate";
            const string allowOptionalContentValue = "AllowOptionalContent";

            bool isEnabled = false;

            try
            {
                // Open the registry key
                using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (var policyKey = baseKey.OpenSubKey(subKeyPath, writable: false))
                {
                    if (policyKey != null)
                    {
                        // Check if the value exists and is set to 1
                        object policyValue = policyKey.GetValue(allowOptionalContentValue);
                        if (policyValue is int value && value == 1)
                        {
                            isEnabled = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading AllowOptionalContent: {ex.Message}");
                isEnabled = false;
            }

            // Access the checkbox on the main form
            _mainForm.chkOptionalUpdates.Checked = isEnabled;
        }

        private void UpdateProductVersionAndReleaseInfo()
        {
            const string subKeyPath = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate";
            const string productVersionValue = "ProductVersion";
            const string targetReleaseVersionInfoValue = "TargetReleaseVersionInfo";

            string productVersion = "Not Found";
            string targetReleaseVersionInfo = "Not Found";

            try
            {
                using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (var policyKey = baseKey.OpenSubKey(subKeyPath, writable: false))
                {
                    if (policyKey != null)
                    {
                        // Check for ProductVersion
                        object productVersionObj = policyKey.GetValue(productVersionValue);
                        if (productVersionObj is string pv)
                        {
                            productVersion = pv;
                        }

                        // Check for TargetReleaseVersionInfo
                        object targetReleaseVersionInfoObj = policyKey.GetValue(targetReleaseVersionInfoValue);
                        if (targetReleaseVersionInfoObj is string trvInfo)
                        {
                            targetReleaseVersionInfo = trvInfo;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading ProductVersion or TargetReleaseVersionInfo: {ex.Message}");
            }

            // Update the text boxes on the main form
            _mainForm.txtProductVersion.Text = productVersion;
            _mainForm.txtFeatureVersion.Text = targetReleaseVersionInfo;
        }

        public void ResetFeatureUpdateVersionPolicy()
        {
            try
            {
                string registryPath = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate";

                using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(registryPath, writable: true))
                {
                    if (registryKey != null)
                    {
                        if (registryKey.GetValue("TargetReleaseVersion") != null)
                        {
                            registryKey.DeleteValue("TargetReleaseVersion");
                        }

                        if (registryKey.GetValue("TargetReleaseVersionInfo") != null)
                        {
                            registryKey.DeleteValue("TargetReleaseVersionInfo");
                        }

                        if (registryKey.GetValue("ProductVersion") != null)
                        {
                            registryKey.DeleteValue("ProductVersion");
                        }
                    }
                    else
                    {
                        MessageBox.Show(
                            "Registry path not found. Ensure the policy is applied.",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error removing registry values: " + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }
}
