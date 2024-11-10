namespace NetworkDetector
{
    partial class NetworkDetector
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NetworkDetector));
            machineNameTextBox = new TextBox();
            wanIpTextBox = new TextBox();
            ispTextBox = new TextBox();
            checkBox1 = new CheckBox();
            label12 = new Label();
            label13 = new Label();
            buildNumberTextBox = new TextBox();
            windowsOsTextBox = new TextBox();
            ipAddressTextBox = new TextBox();
            button3 = new Button();
            label9 = new Label();
            label10 = new Label();
            label11 = new Label();
            storageInfoTextBox = new TextBox();
            ramInfoTextBox = new TextBox();
            cpuInfoTextBox = new TextBox();
            label4 = new Label();
            label3 = new Label();
            label2 = new Label();
            label5 = new Label();
            Version = new TextBox();
            button5 = new Button();
            button4 = new Button();
            panel1 = new Panel();
            logTextBox = new TextBox();
            panel3 = new Panel();
            storenametxt = new TextBox();
            Button6 = new Button();
            button7 = new Button();
            button8 = new Button();
            panel1.SuspendLayout();
            panel3.SuspendLayout();
            SuspendLayout();
            // 
            // machineNameTextBox
            // 
            machineNameTextBox.BackColor = Color.FromArgb(46, 51, 73);
            machineNameTextBox.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            machineNameTextBox.ForeColor = SystemColors.Control;
            machineNameTextBox.Location = new Point(117, 6);
            machineNameTextBox.Name = "machineNameTextBox";
            machineNameTextBox.ReadOnly = true;
            machineNameTextBox.Size = new Size(207, 23);
            machineNameTextBox.TabIndex = 2;
            // 
            // wanIpTextBox
            // 
            wanIpTextBox.BackColor = Color.FromArgb(46, 51, 73);
            wanIpTextBox.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            wanIpTextBox.ForeColor = SystemColors.Control;
            wanIpTextBox.Location = new Point(117, 35);
            wanIpTextBox.Name = "wanIpTextBox";
            wanIpTextBox.ReadOnly = true;
            wanIpTextBox.Size = new Size(207, 23);
            wanIpTextBox.TabIndex = 3;
            // 
            // ispTextBox
            // 
            ispTextBox.BackColor = Color.FromArgb(46, 51, 73);
            ispTextBox.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            ispTextBox.ForeColor = SystemColors.Control;
            ispTextBox.Location = new Point(117, 64);
            ispTextBox.Name = "ispTextBox";
            ispTextBox.ReadOnly = true;
            ispTextBox.Size = new Size(207, 23);
            ispTextBox.TabIndex = 4;
            // 
            // checkBox1
            // 
            checkBox1.AutoSize = true;
            checkBox1.Location = new Point(156, 246);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new Size(15, 14);
            checkBox1.TabIndex = 24;
            checkBox1.UseVisualStyleBackColor = true;
            checkBox1.CheckedChanged += checkBox1_CheckedChanged;
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            label12.ForeColor = SystemColors.Control;
            label12.Location = new Point(69, 212);
            label12.Name = "label12";
            label12.Size = new Size(41, 15);
            label12.TabIndex = 23;
            label12.Text = "Build :";
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            label13.ForeColor = SystemColors.Control;
            label13.Location = new Point(80, 183);
            label13.Name = "label13";
            label13.Size = new Size(29, 15);
            label13.TabIndex = 22;
            label13.Text = "OS :";
            // 
            // buildNumberTextBox
            // 
            buildNumberTextBox.BackColor = Color.FromArgb(46, 51, 73);
            buildNumberTextBox.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            buildNumberTextBox.ForeColor = SystemColors.Control;
            buildNumberTextBox.Location = new Point(117, 209);
            buildNumberTextBox.Name = "buildNumberTextBox";
            buildNumberTextBox.ReadOnly = true;
            buildNumberTextBox.Size = new Size(207, 23);
            buildNumberTextBox.TabIndex = 21;
            // 
            // windowsOsTextBox
            // 
            windowsOsTextBox.BackColor = Color.FromArgb(46, 51, 73);
            windowsOsTextBox.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            windowsOsTextBox.ForeColor = SystemColors.Control;
            windowsOsTextBox.Location = new Point(117, 180);
            windowsOsTextBox.Name = "windowsOsTextBox";
            windowsOsTextBox.ReadOnly = true;
            windowsOsTextBox.Size = new Size(207, 23);
            windowsOsTextBox.TabIndex = 20;
            // 
            // ipAddressTextBox
            // 
            ipAddressTextBox.Location = new Point(177, 241);
            ipAddressTextBox.Name = "ipAddressTextBox";
            ipAddressTextBox.ReadOnly = true;
            ipAddressTextBox.Size = new Size(147, 23);
            ipAddressTextBox.TabIndex = 19;
            ipAddressTextBox.Text = "195.62.221.62";
            ipAddressTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // button3
            // 
            button3.BackColor = Color.FromArgb(46, 51, 73);
            button3.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            button3.ForeColor = SystemColors.Control;
            button3.Location = new Point(15, 241);
            button3.Name = "button3";
            button3.Size = new Size(134, 23);
            button3.TabIndex = 18;
            button3.Text = "Send";
            button3.UseVisualStyleBackColor = false;
            button3.Click += SendButton;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            label9.ForeColor = SystemColors.Control;
            label9.Location = new Point(73, 154);
            label9.Name = "label9";
            label9.Size = new Size(40, 15);
            label9.TabIndex = 16;
            label9.Text = "HDD :";
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            label10.ForeColor = SystemColors.Control;
            label10.Location = new Point(73, 125);
            label10.Name = "label10";
            label10.Size = new Size(40, 15);
            label10.TabIndex = 15;
            label10.Text = "RAM :";
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            label11.ForeColor = SystemColors.Control;
            label11.Location = new Point(73, 96);
            label11.Name = "label11";
            label11.Size = new Size(36, 15);
            label11.TabIndex = 14;
            label11.Text = "CPU :";
            // 
            // storageInfoTextBox
            // 
            storageInfoTextBox.BackColor = Color.FromArgb(46, 51, 73);
            storageInfoTextBox.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            storageInfoTextBox.ForeColor = SystemColors.Control;
            storageInfoTextBox.Location = new Point(117, 151);
            storageInfoTextBox.Name = "storageInfoTextBox";
            storageInfoTextBox.ReadOnly = true;
            storageInfoTextBox.Size = new Size(207, 23);
            storageInfoTextBox.TabIndex = 13;
            // 
            // ramInfoTextBox
            // 
            ramInfoTextBox.BackColor = Color.FromArgb(46, 51, 73);
            ramInfoTextBox.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            ramInfoTextBox.ForeColor = SystemColors.Control;
            ramInfoTextBox.Location = new Point(117, 122);
            ramInfoTextBox.Name = "ramInfoTextBox";
            ramInfoTextBox.ReadOnly = true;
            ramInfoTextBox.Size = new Size(207, 23);
            ramInfoTextBox.TabIndex = 12;
            // 
            // cpuInfoTextBox
            // 
            cpuInfoTextBox.BackColor = Color.FromArgb(46, 51, 73);
            cpuInfoTextBox.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            cpuInfoTextBox.ForeColor = SystemColors.Control;
            cpuInfoTextBox.Location = new Point(117, 93);
            cpuInfoTextBox.Name = "cpuInfoTextBox";
            cpuInfoTextBox.ReadOnly = true;
            cpuInfoTextBox.Size = new Size(207, 23);
            cpuInfoTextBox.TabIndex = 11;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            label4.ForeColor = SystemColors.Control;
            label4.Location = new Point(80, 67);
            label4.Name = "label4";
            label4.Size = new Size(31, 15);
            label4.TabIndex = 10;
            label4.Text = "ISP :";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            label3.ForeColor = SystemColors.Control;
            label3.Location = new Point(55, 38);
            label3.Name = "label3";
            label3.Size = new Size(56, 15);
            label3.TabIndex = 9;
            label3.Text = "WAN IP :";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            label2.ForeColor = SystemColors.Control;
            label2.Location = new Point(15, 9);
            label2.Name = "label2";
            label2.Size = new Size(96, 15);
            label2.TabIndex = 8;
            label2.Text = "Machine Name :";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.ForeColor = SystemColors.Control;
            label5.Location = new Point(12, 278);
            label5.Name = "label5";
            label5.Size = new Size(98, 15);
            label5.TabIndex = 1;
            label5.Text = "Version Number :";
            // 
            // Version
            // 
            Version.Location = new Point(116, 275);
            Version.Name = "Version";
            Version.Size = new Size(66, 23);
            Version.TabIndex = 0;
            Version.Text = "8.0.1";
            Version.TextAlign = HorizontalAlignment.Center;
            // 
            // button5
            // 
            button5.BackColor = Color.FromArgb(46, 51, 74);
            button5.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            button5.ForeColor = SystemColors.Control;
            button5.Location = new Point(3, 37);
            button5.Name = "button5";
            button5.Size = new Size(143, 28);
            button5.TabIndex = 1;
            button5.Text = "Set 24H2";
            button5.UseVisualStyleBackColor = false;
            button5.Click += button5_Click;
            // 
            // button4
            // 
            button4.BackColor = Color.FromArgb(46, 51, 74);
            button4.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            button4.ForeColor = SystemColors.Control;
            button4.Location = new Point(3, 3);
            button4.Name = "button4";
            button4.Size = new Size(143, 28);
            button4.TabIndex = 0;
            button4.Text = "OneDrive Task";
            button4.UseVisualStyleBackColor = false;
            button4.Click += button4_Click;
            // 
            // panel1
            // 
            panel1.Controls.Add(logTextBox);
            panel1.Controls.Add(machineNameTextBox);
            panel1.Controls.Add(storageInfoTextBox);
            panel1.Controls.Add(label11);
            panel1.Controls.Add(ramInfoTextBox);
            panel1.Controls.Add(label10);
            panel1.Controls.Add(cpuInfoTextBox);
            panel1.Controls.Add(label9);
            panel1.Controls.Add(checkBox1);
            panel1.Controls.Add(label4);
            panel1.Controls.Add(label3);
            panel1.Controls.Add(button3);
            panel1.Controls.Add(label12);
            panel1.Controls.Add(label2);
            panel1.Controls.Add(ipAddressTextBox);
            panel1.Controls.Add(label13);
            panel1.Controls.Add(windowsOsTextBox);
            panel1.Controls.Add(ispTextBox);
            panel1.Controls.Add(wanIpTextBox);
            panel1.Controls.Add(buildNumberTextBox);
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(336, 305);
            panel1.TabIndex = 25;
            // 
            // logTextBox
            // 
            logTextBox.BackColor = Color.FromArgb(46, 51, 73);
            logTextBox.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            logTextBox.ForeColor = SystemColors.Control;
            logTextBox.Location = new Point(15, 270);
            logTextBox.Name = "logTextBox";
            logTextBox.Size = new Size(309, 23);
            logTextBox.TabIndex = 17;
            // 
            // panel3
            // 
            panel3.Controls.Add(storenametxt);
            panel3.Controls.Add(label5);
            panel3.Controls.Add(Version);
            panel3.Controls.Add(button4);
            panel3.Controls.Add(button5);
            panel3.Location = new Point(0, 0);
            panel3.Name = "panel3";
            panel3.Size = new Size(336, 305);
            panel3.TabIndex = 27;
            // 
            // storenametxt
            // 
            storenametxt.Location = new Point(156, 6);
            storenametxt.Name = "storenametxt";
            storenametxt.PlaceholderText = "Store Name";
            storenametxt.Size = new Size(168, 23);
            storenametxt.TabIndex = 2;
            storenametxt.TextAlign = HorizontalAlignment.Center;
            // 
            // Button6
            // 
            Button6.BackColor = Color.FromArgb(46, 51, 73);
            Button6.BackgroundImageLayout = ImageLayout.None;
            Button6.FlatStyle = FlatStyle.Flat;
            Button6.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Button6.ForeColor = SystemColors.Control;
            Button6.Location = new Point(14, 307);
            Button6.Name = "Button6";
            Button6.Size = new Size(99, 28);
            Button6.TabIndex = 28;
            Button6.Text = "Home";
            Button6.UseVisualStyleBackColor = false;
            Button6.Click += Button6_Click;
            // 
            // button7
            // 
            button7.BackColor = Color.FromArgb(46, 51, 73);
            button7.BackgroundImageLayout = ImageLayout.None;
            button7.FlatStyle = FlatStyle.Flat;
            button7.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button7.ForeColor = SystemColors.Control;
            button7.Location = new Point(119, 307);
            button7.Name = "button7";
            button7.Size = new Size(99, 28);
            button7.TabIndex = 29;
            button7.Text = "Update";
            button7.UseVisualStyleBackColor = false;
            button7.Click += button7_Click;
            // 
            // button8
            // 
            button8.BackColor = Color.FromArgb(46, 51, 73);
            button8.BackgroundImageLayout = ImageLayout.None;
            button8.FlatStyle = FlatStyle.Flat;
            button8.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button8.ForeColor = SystemColors.Control;
            button8.Location = new Point(224, 307);
            button8.Name = "button8";
            button8.Size = new Size(99, 28);
            button8.TabIndex = 30;
            button8.Text = "Script";
            button8.UseVisualStyleBackColor = false;
            button8.Click += button8_Click;
            // 
            // NetworkDetector
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(46, 51, 73);
            ClientSize = new Size(336, 346);
            Controls.Add(panel1);
            Controls.Add(button8);
            Controls.Add(button7);
            Controls.Add(Button6);
            Controls.Add(panel3);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(4, 3, 4, 3);
            MaximizeBox = false;
            Name = "NetworkDetector";
            Text = "Network Manager 10.0.0";
            Load += NetworkDetector_Load;
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            panel3.ResumeLayout(false);
            panel3.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
        private TextBox machineNameTextBox;
        private TextBox wanIpTextBox;
        private TextBox ispTextBox;
        private Label label3;
        private Label label2;
        private Label label4;
        private Label label5;
        private TextBox Version;
        private Label label9;
        private Label label10;
        private Label label11;
        private TextBox storageInfoTextBox;
        private TextBox ramInfoTextBox;
        private TextBox cpuInfoTextBox;
        
        private Button button3;
        private TextBox ipAddressTextBox;
        private Label label12;
        private Label label13;
        private TextBox buildNumberTextBox;
        private TextBox windowsOsTextBox;
        private CheckBox checkBox1;
        private Button button4;
        private Button button5;
        private Panel panel1;
        private Panel panel3;
        private Button Button6;
        private Button button7;
        private Button button8;
        private TextBox logTextBox;
        private TextBox storenametxt;
    }
}
