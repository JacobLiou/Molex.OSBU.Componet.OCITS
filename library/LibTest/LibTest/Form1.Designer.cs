namespace LibTest
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.button1 = new System.Windows.Forms.Button();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.SNBox = new System.Windows.Forms.TextBox();
            this.BtnOpen = new System.Windows.Forms.Button();
            this.txtCom = new System.Windows.Forms.TextBox();
            this.BtnOpenCom = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.btnGetPDL = new System.Windows.Forms.Button();
            this.m_PWMDataShow = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.button3 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.m_PWMDataShow)).BeginInit();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(115, 49);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 21);
            this.button1.TabIndex = 0;
            this.button1.Text = "button1";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // dataGridView1
            // 
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(794, 322);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowTemplate.Height = 23;
            this.dataGridView1.Size = new System.Drawing.Size(133, 87);
            this.dataGridView1.TabIndex = 1;
            this.dataGridView1.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellClick);
            // 
            // SNBox
            // 
            this.SNBox.Location = new System.Drawing.Point(12, 11);
            this.SNBox.Name = "SNBox";
            this.SNBox.Size = new System.Drawing.Size(100, 21);
            this.SNBox.TabIndex = 2;
            // 
            // BtnOpen
            // 
            this.BtnOpen.Location = new System.Drawing.Point(118, 9);
            this.BtnOpen.Name = "BtnOpen";
            this.BtnOpen.Size = new System.Drawing.Size(75, 21);
            this.BtnOpen.TabIndex = 3;
            this.BtnOpen.Text = "Open";
            this.BtnOpen.UseVisualStyleBackColor = true;
            this.BtnOpen.Click += new System.EventHandler(this.BtnOpen_Click);
            // 
            // txtCom
            // 
            this.txtCom.Location = new System.Drawing.Point(306, 11);
            this.txtCom.Name = "txtCom";
            this.txtCom.Size = new System.Drawing.Size(100, 21);
            this.txtCom.TabIndex = 4;
            // 
            // BtnOpenCom
            // 
            this.BtnOpenCom.Location = new System.Drawing.Point(423, 9);
            this.BtnOpenCom.Name = "BtnOpenCom";
            this.BtnOpenCom.Size = new System.Drawing.Size(75, 23);
            this.BtnOpenCom.TabIndex = 5;
            this.BtnOpenCom.Text = "打开串口";
            this.BtnOpenCom.UseVisualStyleBackColor = true;
            this.BtnOpenCom.Click += new System.EventHandler(this.BtnOpenCom_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(580, 8);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 6;
            this.button2.Text = "button2";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // btnGetPDL
            // 
            this.btnGetPDL.Location = new System.Drawing.Point(430, 46);
            this.btnGetPDL.Name = "btnGetPDL";
            this.btnGetPDL.Size = new System.Drawing.Size(75, 23);
            this.btnGetPDL.TabIndex = 7;
            this.btnGetPDL.Text = "GetPDL";
            this.btnGetPDL.UseVisualStyleBackColor = true;
            this.btnGetPDL.Click += new System.EventHandler(this.btnGetPDL_Click);
            // 
            // m_PWMDataShow
            // 
            chartArea1.Name = "ChartArea1";
            this.m_PWMDataShow.ChartAreas.Add(chartArea1);
            legend1.Name = "Legend1";
            this.m_PWMDataShow.Legends.Add(legend1);
            this.m_PWMDataShow.Location = new System.Drawing.Point(32, 109);
            this.m_PWMDataShow.Name = "m_PWMDataShow";
            series1.ChartArea = "ChartArea1";
            series1.Legend = "Legend1";
            series1.Name = "Series1";
            this.m_PWMDataShow.Series.Add(series1);
            this.m_PWMDataShow.Size = new System.Drawing.Size(756, 300);
            this.m_PWMDataShow.TabIndex = 8;
            this.m_PWMDataShow.Text = "chart1";
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(593, 54);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(75, 23);
            this.button3.TabIndex = 9;
            this.button3.Text = "button3";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(936, 422);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.m_PWMDataShow);
            this.Controls.Add(this.btnGetPDL);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.BtnOpenCom);
            this.Controls.Add(this.txtCom);
            this.Controls.Add(this.BtnOpen);
            this.Controls.Add(this.SNBox);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.button1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form1_FormClosed);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.m_PWMDataShow)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.TextBox SNBox;
        private System.Windows.Forms.Button BtnOpen;
        private System.Windows.Forms.TextBox txtCom;
        private System.Windows.Forms.Button BtnOpenCom;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button btnGetPDL;
        private System.Windows.Forms.DataVisualization.Charting.Chart m_PWMDataShow;
        private System.Windows.Forms.Button button3;
    }
}

