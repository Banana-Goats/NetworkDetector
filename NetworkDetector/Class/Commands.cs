using System;
using System.Data;
using System.Windows.Forms;
using NetworkDetector.Helpers;
using System.Threading.Tasks;


namespace NetworkDetector.Class
{
    public partial class Commands : Form
    {
        private readonly CommandExecutor commandExecutor;
        private System.Windows.Forms.Timer refreshTimer;

        public Commands()
        {
            InitializeComponent();

            try
            {
                commandExecutor = new CommandExecutor();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
                return;
            }

            InitializeDataGridView();
            LoadCommandsIntoGrid();

            InitializeRefreshTimer();

            // Subscribe to the CellContentClick event
            dataGridViewCommands.CellContentClick += DataGridViewCommands_CellContentClick;
        }

        private void InitializeRefreshTimer()
        {
            refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 30000; // 30,000 milliseconds = 30 seconds
            refreshTimer.Tick += RefreshTimer_Tick;
            refreshTimer.Start();
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            LoadCommandsIntoGrid();
            dataGridViewCommands.ClearSelection();
        }

        private void InitializeDataGridView()
        {
            // Clear any existing columns
            dataGridViewCommands.Columns.Clear();

            // Create and add the Type column
            DataGridViewTextBoxColumn typeColumn = new DataGridViewTextBoxColumn
            {
                Name = "Type",
                HeaderText = "Type",
                DataPropertyName = "Type",
                ReadOnly = true,
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };
            dataGridViewCommands.Columns.Add(typeColumn);

            // Create and add the Description column
            DataGridViewTextBoxColumn descriptionColumn = new DataGridViewTextBoxColumn
            {
                Name = "Description",
                HeaderText = "Description",
                DataPropertyName = "Description",
                ReadOnly = true,
                Width = 250,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };
            dataGridViewCommands.Columns.Add(descriptionColumn);

            // Create and add the Command column
            DataGridViewTextBoxColumn commandColumn = new DataGridViewTextBoxColumn
            {
                Name = "Command",
                HeaderText = "Command",
                DataPropertyName = "Command",
                ReadOnly = true,
                Visible = false,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };
            dataGridViewCommands.Columns.Add(commandColumn);

            // Create and add the CurrentValue column
            DataGridViewTextBoxColumn currentValueColumn = new DataGridViewTextBoxColumn
            {
                Name = "CurrentValue",
                HeaderText = "Current Value",
                DataPropertyName = "CurrentValue",
                ReadOnly = true,
                Width = 200,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };
            dataGridViewCommands.Columns.Add(currentValueColumn);

            // Create and add the DesiredValue column (from 'Value' in SQL)
            DataGridViewTextBoxColumn desiredValueColumn = new DataGridViewTextBoxColumn
            {
                Name = "Value",
                HeaderText = "Desired Value",
                DataPropertyName = "Value",
                ReadOnly = true,
                Width = 200,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };
            dataGridViewCommands.Columns.Add(desiredValueColumn);

            // Create and add the Set Button column
            DataGridViewButtonColumn setButtonColumn = new DataGridViewButtonColumn
            {
                Name = "SetButton",
                HeaderText = "Set",
                Text = "Set",
                UseColumnTextForButtonValue = true,
                Width = 75,
                FlatStyle = FlatStyle.Flat, // Set FlatStyle to Flat
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    BackColor = SystemColors.Control, // Default button color
                    ForeColor = SystemColors.ControlText
                }
            };
            dataGridViewCommands.Columns.Add(setButtonColumn);

            // Create and add the RegistryValueType column (Hidden)
            DataGridViewTextBoxColumn registryValueTypeColumn = new DataGridViewTextBoxColumn
            {
                Name = "RegistryValueType",
                HeaderText = "Registry Value Type",
                DataPropertyName = "RegistryValueType",
                ReadOnly = true,
                Visible = false // Hide this column
            };
            dataGridViewCommands.Columns.Add(registryValueTypeColumn);

            // Set DataGridView properties
            dataGridViewCommands.AutoGenerateColumns = false;
            dataGridViewCommands.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridViewCommands.MultiSelect = false;
            dataGridViewCommands.AllowUserToAddRows = false;
            dataGridViewCommands.ReadOnly = true;

            // Center align column headers
            dataGridViewCommands.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            // Enable sorting for visible columns
            typeColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            descriptionColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            commandColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            currentValueColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            desiredValueColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            setButtonColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            registryValueTypeColumn.SortMode = DataGridViewColumnSortMode.NotSortable; // Hidden column
        }

        private async void DataGridViewCommands_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dataGridViewCommands.Columns[e.ColumnIndex].Name == "SetButton")
            {
                DataGridViewRow row = dataGridViewCommands.Rows[e.RowIndex];
                string type = row.Cells["Type"].Value.ToString();
                string command = row.Cells["Command"].Value.ToString();
                string description = row.Cells["Description"].Value.ToString();
                string desiredValue = row.Cells["Value"].Value?.ToString();
                string registryValueType = row.Cells["RegistryValueType"].Value?.ToString();

                // Create a Progress<string> object
                var progress = new Progress<string>(message =>
                {

                });

                if (type.Equals("Reg", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(desiredValue))
                    {
                        MessageBox.Show($"Desired value for '{description}' is not set.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Updated call with progress and description
                    bool success = commandExecutor.SetRegistryValue(command, desiredValue, registryValueType, progress, description);

                    if (success)
                    {
                        LoadCommandsIntoGrid(); // Reload to show updated values
                        //MessageBox.Show($"Successfully set registry value for '{description}'.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Failed to set registry value for '{description}'.", "Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else if (type.Equals("CMD", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(command))
                    {
                        MessageBox.Show($"Command for '{description}' is not set.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Updated call with progress
                    bool success = await commandExecutor.ExecuteMultipleCommandsAsAdminAsync(command, description, progress);

                    if (success)
                    {
                        LoadCommandsIntoGrid(); // Reload if necessary
                        //MessageBox.Show($"Successfully executed CMD commands for '{description}'.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Failed to execute CMD commands for '{description}'.", "Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show($"Unsupported Type: {type}", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private async void LoadCommandsIntoGrid()
        {
            try
            {
                var commands = await commandExecutor.LoadCommandsAsync();
                DataTable dt = ConvertCommandsToDataTable(commands);
                dataGridViewCommands.DataSource = dt;

                // Hide the ID column if present
                if (dataGridViewCommands.Columns.Contains("ID"))
                    dataGridViewCommands.Columns["ID"].Visible = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading commands: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private DataTable ConvertCommandsToDataTable(List<Command> commands)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("ID", typeof(int));
            dt.Columns.Add("Type", typeof(string));
            dt.Columns.Add("Description", typeof(string));
            dt.Columns.Add("Command", typeof(string));
            dt.Columns.Add("Value", typeof(string));
            dt.Columns.Add("RegistryValueType", typeof(string));
            dt.Columns.Add("CurrentValue", typeof(string));

            foreach (var cmd in commands)
            {
                dt.Rows.Add(cmd.ID, cmd.Type, cmd.Description, cmd.CommandText, cmd.DesiredValue, cmd.RegistryValueType, cmd.CurrentValue);
            }

            return dt;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (refreshTimer != null)
            {
                refreshTimer.Stop();
                refreshTimer.Tick -= RefreshTimer_Tick;
                refreshTimer.Dispose();
            }
            base.OnFormClosing(e);
        }
    }
}
