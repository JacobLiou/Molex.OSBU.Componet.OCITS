namespace TccTempTest
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.lblGuid = new System.Windows.Forms.Label();
            this.txtGuid = new System.Windows.Forms.TextBox();
            this.btnInit = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblCurrent = new System.Windows.Forms.Label();
            this.txtCurrentTemp = new System.Windows.Forms.TextBox();
            this.btnRead = new System.Windows.Forms.Button();
            this.lblSetpoint = new System.Windows.Forms.Label();
            this.txtSetpoint = new System.Windows.Forms.TextBox();
            this.btnSet = new System.Windows.Forms.Button();
            this.btnLt = new System.Windows.Forms.Button();
            this.btnRt = new System.Windows.Forms.Button();
            this.btnHt = new System.Windows.Forms.Button();
            this.chkAutoRead = new System.Windows.Forms.CheckBox();
            this.lblInterval = new System.Windows.Forms.Label();
            this.nudInterval = new System.Windows.Forms.NumericUpDown();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.pollTimer = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.nudInterval)).BeginInit();
            this.SuspendLayout();
            //
            // lblGuid
            //
            this.lblGuid.AutoSize = true;
            this.lblGuid.Location = new System.Drawing.Point(12, 15);
            this.lblGuid.Name = "lblGuid";
            this.lblGuid.Size = new System.Drawing.Size(59, 12);
            this.lblGuid.TabIndex = 0;
            this.lblGuid.Text = "TCC GUID";
            //
            // txtGuid
            //
            this.txtGuid.Location = new System.Drawing.Point(77, 12);
            this.txtGuid.Name = "txtGuid";
            this.txtGuid.Size = new System.Drawing.Size(60, 21);
            this.txtGuid.TabIndex = 1;
            this.txtGuid.Text = "1";
            //
            // btnInit
            //
            this.btnInit.Location = new System.Drawing.Point(155, 10);
            this.btnInit.Name = "btnInit";
            this.btnInit.Size = new System.Drawing.Size(100, 25);
            this.btnInit.TabIndex = 2;
            this.btnInit.Text = "初始化设备";
            this.btnInit.UseVisualStyleBackColor = true;
            this.btnInit.Click += new System.EventHandler(this.btnInit_Click);
            //
            // lblStatus
            //
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(270, 15);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(65, 12);
            this.lblStatus.TabIndex = 3;
            this.lblStatus.Text = "未初始化";
            //
            // lblCurrent
            //
            this.lblCurrent.AutoSize = true;
            this.lblCurrent.Location = new System.Drawing.Point(12, 52);
            this.lblCurrent.Name = "lblCurrent";
            this.lblCurrent.Size = new System.Drawing.Size(53, 12);
            this.lblCurrent.TabIndex = 4;
            this.lblCurrent.Text = "当前温度";
            //
            // txtCurrentTemp
            //
            this.txtCurrentTemp.Location = new System.Drawing.Point(77, 49);
            this.txtCurrentTemp.Name = "txtCurrentTemp";
            this.txtCurrentTemp.ReadOnly = true;
            this.txtCurrentTemp.Size = new System.Drawing.Size(100, 21);
            this.txtCurrentTemp.TabIndex = 5;
            //
            // btnRead
            //
            this.btnRead.Enabled = false;
            this.btnRead.Location = new System.Drawing.Point(195, 47);
            this.btnRead.Name = "btnRead";
            this.btnRead.Size = new System.Drawing.Size(100, 25);
            this.btnRead.TabIndex = 6;
            this.btnRead.Text = "读取温度";
            this.btnRead.UseVisualStyleBackColor = true;
            this.btnRead.Click += new System.EventHandler(this.btnRead_Click);
            //
            // lblSetpoint
            //
            this.lblSetpoint.AutoSize = true;
            this.lblSetpoint.Location = new System.Drawing.Point(12, 89);
            this.lblSetpoint.Name = "lblSetpoint";
            this.lblSetpoint.Size = new System.Drawing.Size(53, 12);
            this.lblSetpoint.TabIndex = 7;
            this.lblSetpoint.Text = "设定温度";
            //
            // txtSetpoint
            //
            this.txtSetpoint.Location = new System.Drawing.Point(77, 86);
            this.txtSetpoint.Name = "txtSetpoint";
            this.txtSetpoint.Size = new System.Drawing.Size(100, 21);
            this.txtSetpoint.TabIndex = 8;
            this.txtSetpoint.Text = "25";
            //
            // btnSet
            //
            this.btnSet.Enabled = false;
            this.btnSet.Location = new System.Drawing.Point(195, 84);
            this.btnSet.Name = "btnSet";
            this.btnSet.Size = new System.Drawing.Size(100, 25);
            this.btnSet.TabIndex = 9;
            this.btnSet.Text = "设置温度";
            this.btnSet.UseVisualStyleBackColor = true;
            this.btnSet.Click += new System.EventHandler(this.btnSet_Click);
            //
            // btnLt
            //
            this.btnLt.Enabled = false;
            this.btnLt.Location = new System.Drawing.Point(320, 84);
            this.btnLt.Name = "btnLt";
            this.btnLt.Size = new System.Drawing.Size(60, 25);
            this.btnLt.TabIndex = 10;
            this.btnLt.Text = "-5°C";
            this.btnLt.UseVisualStyleBackColor = true;
            this.btnLt.Click += new System.EventHandler(this.btnLt_Click);
            //
            // btnRt
            //
            this.btnRt.Enabled = false;
            this.btnRt.Location = new System.Drawing.Point(390, 84);
            this.btnRt.Name = "btnRt";
            this.btnRt.Size = new System.Drawing.Size(60, 25);
            this.btnRt.TabIndex = 11;
            this.btnRt.Text = "25°C";
            this.btnRt.UseVisualStyleBackColor = true;
            this.btnRt.Click += new System.EventHandler(this.btnRt_Click);
            //
            // btnHt
            //
            this.btnHt.Enabled = false;
            this.btnHt.Location = new System.Drawing.Point(460, 84);
            this.btnHt.Name = "btnHt";
            this.btnHt.Size = new System.Drawing.Size(60, 25);
            this.btnHt.TabIndex = 12;
            this.btnHt.Text = "55°C";
            this.btnHt.UseVisualStyleBackColor = true;
            this.btnHt.Click += new System.EventHandler(this.btnHt_Click);
            //
            // chkAutoRead
            //
            this.chkAutoRead.AutoSize = true;
            this.chkAutoRead.Enabled = false;
            this.chkAutoRead.Location = new System.Drawing.Point(12, 125);
            this.chkAutoRead.Name = "chkAutoRead";
            this.chkAutoRead.Size = new System.Drawing.Size(72, 16);
            this.chkAutoRead.TabIndex = 13;
            this.chkAutoRead.Text = "定时读温";
            this.chkAutoRead.UseVisualStyleBackColor = true;
            this.chkAutoRead.CheckedChanged += new System.EventHandler(this.chkAutoRead_CheckedChanged);
            //
            // lblInterval
            //
            this.lblInterval.AutoSize = true;
            this.lblInterval.Location = new System.Drawing.Point(100, 126);
            this.lblInterval.Name = "lblInterval";
            this.lblInterval.Size = new System.Drawing.Size(41, 12);
            this.lblInterval.TabIndex = 14;
            this.lblInterval.Text = "间隔秒";
            //
            // nudInterval
            //
            this.nudInterval.DecimalPlaces = 0;
            this.nudInterval.Location = new System.Drawing.Point(147, 123);
            this.nudInterval.Maximum = new decimal(new int[] { 3600, 0, 0, 0 });
            this.nudInterval.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudInterval.Name = "nudInterval";
            this.nudInterval.Size = new System.Drawing.Size(60, 21);
            this.nudInterval.TabIndex = 15;
            this.nudInterval.Value = new decimal(new int[] { 2, 0, 0, 0 });
            this.nudInterval.ValueChanged += new System.EventHandler(this.nudInterval_ValueChanged);
            //
            // txtLog
            //
            this.txtLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLog.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtLog.Location = new System.Drawing.Point(12, 155);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(560, 294);
            this.txtLog.TabIndex = 16;
            //
            // pollTimer
            //
            this.pollTimer.Tick += new System.EventHandler(this.pollTimer_Tick);
            //
            // MainForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 461);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.nudInterval);
            this.Controls.Add(this.lblInterval);
            this.Controls.Add(this.chkAutoRead);
            this.Controls.Add(this.btnHt);
            this.Controls.Add(this.btnRt);
            this.Controls.Add(this.btnLt);
            this.Controls.Add(this.btnSet);
            this.Controls.Add(this.txtSetpoint);
            this.Controls.Add(this.lblSetpoint);
            this.Controls.Add(this.btnRead);
            this.Controls.Add(this.txtCurrentTemp);
            this.Controls.Add(this.lblCurrent);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.btnInit);
            this.Controls.Add(this.txtGuid);
            this.Controls.Add(this.lblGuid);
            this.MinimumSize = new System.Drawing.Size(500, 400);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "TCC 温度测试 (DeviceControl)";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.nudInterval)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblGuid;
        private System.Windows.Forms.TextBox txtGuid;
        private System.Windows.Forms.Button btnInit;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblCurrent;
        private System.Windows.Forms.TextBox txtCurrentTemp;
        private System.Windows.Forms.Button btnRead;
        private System.Windows.Forms.Label lblSetpoint;
        private System.Windows.Forms.TextBox txtSetpoint;
        private System.Windows.Forms.Button btnSet;
        private System.Windows.Forms.Button btnLt;
        private System.Windows.Forms.Button btnRt;
        private System.Windows.Forms.Button btnHt;
        private System.Windows.Forms.CheckBox chkAutoRead;
        private System.Windows.Forms.Label lblInterval;
        private System.Windows.Forms.NumericUpDown nudInterval;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.Timer pollTimer;
    }
}
