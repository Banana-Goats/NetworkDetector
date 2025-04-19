namespace NetworkDetector.Class
{
    partial class Commands
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            dataGridViewCommands = new DataGridView();
            ((System.ComponentModel.ISupportInitialize)dataGridViewCommands).BeginInit();
            SuspendLayout();
            // 
            // dataGridViewCommands
            // 
            dataGridViewCommands.AllowUserToAddRows = false;
            dataGridViewCommands.AllowUserToDeleteRows = false;
            dataGridViewCommands.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewCommands.BackgroundColor = Color.FromArgb(46, 51, 73);
            dataGridViewCommands.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCommands.Dock = DockStyle.Fill;
            dataGridViewCommands.Location = new Point(0, 0);
            dataGridViewCommands.Name = "dataGridViewCommands";
            dataGridViewCommands.ReadOnly = true;
            dataGridViewCommands.RowHeadersVisible = false;
            dataGridViewCommands.Size = new Size(800, 450);
            dataGridViewCommands.TabIndex = 0;
            // 
            // Commands
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(46, 51, 73);
            ClientSize = new Size(800, 450);
            Controls.Add(dataGridViewCommands);
            Name = "Commands";
            Text = "Commands";
            ((System.ComponentModel.ISupportInitialize)dataGridViewCommands).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private DataGridView dataGridViewCommands;
    }
}