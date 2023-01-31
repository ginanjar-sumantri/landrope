namespace ImagingCore
{
    partial class ScanForm
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
			this.selectSource = new System.Windows.Forms.Button();
			this.scan = new System.Windows.Forms.Button();
			this.useAdfCheckBox = new System.Windows.Forms.CheckBox();
			this.useUICheckBox = new System.Windows.Forms.CheckBox();
			this.diagnosticsButton = new System.Windows.Forms.Button();
			this.showProgressIndicatorUICheckBox = new System.Windows.Forms.CheckBox();
			this.useDuplexCheckBox = new System.Windows.Forms.CheckBox();
			this.label5 = new System.Windows.Forms.Label();
			this.panel1 = new System.Windows.Forms.Panel();
			this.panel4 = new System.Windows.Forms.Panel();
			this.itemScroll1 = new mycomponents.ItemScroll();
			this.button4 = new System.Windows.Forms.Button();
			this.button3 = new System.Windows.Forms.Button();
			this.button2 = new System.Windows.Forms.Button();
			this.button1 = new System.Windows.Forms.Button();
			this.comboBox1 = new System.Windows.Forms.ComboBox();
			this.label1 = new System.Windows.Forms.Label();
			this.panel3 = new System.Windows.Forms.Panel();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.statusStrip1 = new System.Windows.Forms.StatusStrip();
			this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
			this.toolStripStatusLabel2 = new System.Windows.Forms.ToolStripStatusLabel();
			this.panel1.SuspendLayout();
			this.panel4.SuspendLayout();
			this.panel3.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.statusStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// selectSource
			// 
			this.selectSource.Location = new System.Drawing.Point(9, 126);
			this.selectSource.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.selectSource.Name = "selectSource";
			this.selectSource.Size = new System.Drawing.Size(117, 32);
			this.selectSource.TabIndex = 0;
			this.selectSource.Text = "Select Source";
			this.selectSource.UseVisualStyleBackColor = true;
			this.selectSource.Click += new System.EventHandler(this.selectSource_Click);
			// 
			// scan
			// 
			this.scan.Location = new System.Drawing.Point(9, 164);
			this.scan.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.scan.Name = "scan";
			this.scan.Size = new System.Drawing.Size(117, 46);
			this.scan.TabIndex = 1;
			this.scan.Text = "Scan";
			this.scan.UseVisualStyleBackColor = true;
			this.scan.Click += new System.EventHandler(this.scan_Click);
			// 
			// useAdfCheckBox
			// 
			this.useAdfCheckBox.AutoSize = true;
			this.useAdfCheckBox.Location = new System.Drawing.Point(9, 17);
			this.useAdfCheckBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.useAdfCheckBox.Name = "useAdfCheckBox";
			this.useAdfCheckBox.Size = new System.Drawing.Size(70, 19);
			this.useAdfCheckBox.TabIndex = 3;
			this.useAdfCheckBox.Text = "Use ADF";
			this.useAdfCheckBox.UseVisualStyleBackColor = true;
			// 
			// useUICheckBox
			// 
			this.useUICheckBox.AutoSize = true;
			this.useUICheckBox.Location = new System.Drawing.Point(9, 92);
			this.useUICheckBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.useUICheckBox.Name = "useUICheckBox";
			this.useUICheckBox.Size = new System.Drawing.Size(59, 19);
			this.useUICheckBox.TabIndex = 4;
			this.useUICheckBox.Text = "Use UI";
			this.useUICheckBox.UseVisualStyleBackColor = true;
			// 
			// diagnosticsButton
			// 
			this.diagnosticsButton.Location = new System.Drawing.Point(14, 504);
			this.diagnosticsButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.diagnosticsButton.Name = "diagnosticsButton";
			this.diagnosticsButton.Size = new System.Drawing.Size(136, 46);
			this.diagnosticsButton.TabIndex = 3;
			this.diagnosticsButton.Text = "Diagnostics";
			this.diagnosticsButton.UseVisualStyleBackColor = true;
			// 
			// showProgressIndicatorUICheckBox
			// 
			this.showProgressIndicatorUICheckBox.AutoSize = true;
			this.showProgressIndicatorUICheckBox.Checked = true;
			this.showProgressIndicatorUICheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.showProgressIndicatorUICheckBox.Location = new System.Drawing.Point(9, 67);
			this.showProgressIndicatorUICheckBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.showProgressIndicatorUICheckBox.Name = "showProgressIndicatorUICheckBox";
			this.showProgressIndicatorUICheckBox.Size = new System.Drawing.Size(103, 19);
			this.showProgressIndicatorUICheckBox.TabIndex = 11;
			this.showProgressIndicatorUICheckBox.Text = "Show Progress";
			this.showProgressIndicatorUICheckBox.UseVisualStyleBackColor = true;
			// 
			// useDuplexCheckBox
			// 
			this.useDuplexCheckBox.AutoSize = true;
			this.useDuplexCheckBox.Location = new System.Drawing.Point(9, 42);
			this.useDuplexCheckBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.useDuplexCheckBox.Name = "useDuplexCheckBox";
			this.useDuplexCheckBox.Size = new System.Drawing.Size(84, 19);
			this.useDuplexCheckBox.TabIndex = 13;
			this.useDuplexCheckBox.Text = "Use Duplex";
			this.useDuplexCheckBox.UseVisualStyleBackColor = true;
			// 
			// label5
			// 
			this.label5.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.label5.Location = new System.Drawing.Point(14, 420);
			this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(140, 2);
			this.label5.TabIndex = 19;
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.selectSource);
			this.panel1.Controls.Add(this.scan);
			this.panel1.Controls.Add(this.useAdfCheckBox);
			this.panel1.Controls.Add(this.useDuplexCheckBox);
			this.panel1.Controls.Add(this.showProgressIndicatorUICheckBox);
			this.panel1.Controls.Add(this.useUICheckBox);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Left;
			this.panel1.Location = new System.Drawing.Point(0, 0);
			this.panel1.Name = "panel1";
			this.panel1.Padding = new System.Windows.Forms.Padding(4);
			this.panel1.Size = new System.Drawing.Size(134, 359);
			this.panel1.TabIndex = 20;
			// 
			// panel4
			// 
			this.panel4.Controls.Add(this.itemScroll1);
			this.panel4.Controls.Add(this.button4);
			this.panel4.Controls.Add(this.button3);
			this.panel4.Controls.Add(this.button2);
			this.panel4.Controls.Add(this.button1);
			this.panel4.Controls.Add(this.comboBox1);
			this.panel4.Controls.Add(this.label1);
			this.panel4.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel4.Location = new System.Drawing.Point(134, 0);
			this.panel4.Name = "panel4";
			this.panel4.Size = new System.Drawing.Size(654, 36);
			this.panel4.TabIndex = 21;
			// 
			// itemScroll1
			// 
			this.itemScroll1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.itemScroll1.Limit = 0;
			this.itemScroll1.Location = new System.Drawing.Point(566, 6);
			this.itemScroll1.Name = "itemScroll1";
			this.itemScroll1.Size = new System.Drawing.Size(76, 23);
			this.itemScroll1.TabIndex = 0;
			this.itemScroll1.Value = 0;
			this.itemScroll1.ValueChange += new mycomponents.ItemScroll.ScrollValueChangeHandler(this.itemScroll1_ValueChange);
			// 
			// button4
			// 
			this.button4.Location = new System.Drawing.Point(321, 6);
			this.button4.Name = "button4";
			this.button4.Size = new System.Drawing.Size(74, 23);
			this.button4.TabIndex = 3;
			this.button4.Text = "Hapus";
			this.button4.UseVisualStyleBackColor = true;
			this.button4.Click += new System.EventHandler(this.button4_Click);
			// 
			// button3
			// 
			this.button3.Location = new System.Drawing.Point(244, 6);
			this.button3.Name = "button3";
			this.button3.Size = new System.Drawing.Size(74, 23);
			this.button3.TabIndex = 3;
			this.button3.Text = "Tutup";
			this.button3.UseVisualStyleBackColor = true;
			this.button3.Click += new System.EventHandler(this.button3_Click);
			// 
			// button2
			// 
			this.button2.Location = new System.Drawing.Point(398, 6);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(74, 23);
			this.button2.TabIndex = 3;
			this.button2.Text = "Kosongkan";
			this.button2.UseVisualStyleBackColor = true;
			this.button2.Click += new System.EventHandler(this.button2_Click);
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(167, 6);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(74, 23);
			this.button1.TabIndex = 3;
			this.button1.Text = "Simpan";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// comboBox1
			// 
			this.comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBox1.FormattingEnabled = true;
			this.comboBox1.Items.AddRange(new object[] {
            "Original",
            "Width",
            "Height",
            "Auto",
            "10%",
            "25%",
            "30%",
            "50%",
            "75%",
            "100%",
            "150%",
            "200%"});
			this.comboBox1.Location = new System.Drawing.Point(48, 6);
			this.comboBox1.Name = "comboBox1";
			this.comboBox1.Size = new System.Drawing.Size(100, 23);
			this.comboBox1.TabIndex = 1;
			this.comboBox1.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(6, 9);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(39, 15);
			this.label1.TabIndex = 0;
			this.label1.Text = "Zoom";
			// 
			// panel3
			// 
			this.panel3.AutoScroll = true;
			this.panel3.BackColor = System.Drawing.SystemColors.ControlDark;
			this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel3.Controls.Add(this.pictureBox1);
			this.panel3.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel3.Location = new System.Drawing.Point(134, 36);
			this.panel3.Name = "panel3";
			this.panel3.Size = new System.Drawing.Size(654, 323);
			this.panel3.TabIndex = 22;
			// 
			// pictureBox1
			// 
			this.pictureBox1.BackColor = System.Drawing.SystemColors.ControlDark;
			this.pictureBox1.Location = new System.Drawing.Point(0, 0);
			this.pictureBox1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(100, 100);
			this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
			this.pictureBox1.TabIndex = 2;
			this.pictureBox1.TabStop = false;
			// 
			// statusStrip1
			// 
			this.statusStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Visible;
			this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.toolStripStatusLabel2});
			this.statusStrip1.Location = new System.Drawing.Point(0, 359);
			this.statusStrip1.Name = "statusStrip1";
			this.statusStrip1.Size = new System.Drawing.Size(788, 22);
			this.statusStrip1.TabIndex = 3;
			// 
			// toolStripStatusLabel1
			// 
			this.toolStripStatusLabel1.AutoSize = false;
			this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
			this.toolStripStatusLabel1.Size = new System.Drawing.Size(200, 17);
			this.toolStripStatusLabel1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// toolStripStatusLabel2
			// 
			this.toolStripStatusLabel2.Name = "toolStripStatusLabel2";
			this.toolStripStatusLabel2.Size = new System.Drawing.Size(573, 17);
			this.toolStripStatusLabel2.Spring = true;
			// 
			// ScanForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(788, 381);
			this.Controls.Add(this.panel3);
			this.Controls.Add(this.panel4);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.diagnosticsButton);
			this.Controls.Add(this.statusStrip1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.MinimumSize = new System.Drawing.Size(700, 420);
			this.Name = "ScanForm";
			this.Text = "Scanning Agent";
			this.Activated += new System.EventHandler(this.ScanForm_Activated);
			this.Load += new System.EventHandler(this.MainForm_Load);
			this.Shown += new System.EventHandler(this.ScanForm_Shown);
			this.Resize += new System.EventHandler(this.MainForm_Resize);
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.panel4.ResumeLayout(false);
			this.panel4.PerformLayout();
			this.panel3.ResumeLayout(false);
			this.panel3.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.statusStrip1.ResumeLayout(false);
			this.statusStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button selectSource;
        private System.Windows.Forms.Button scan;
        private System.Windows.Forms.CheckBox useAdfCheckBox;
        private System.Windows.Forms.CheckBox useUICheckBox;
        private System.Windows.Forms.Button diagnosticsButton;
        private System.Windows.Forms.CheckBox showProgressIndicatorUICheckBox;
        private System.Windows.Forms.CheckBox useDuplexCheckBox;
        private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Panel panel4;
		private System.Windows.Forms.ComboBox comboBox1;
		private System.Windows.Forms.Label label1;
		private mycomponents.ItemScroll itemScroll1;
		private System.Windows.Forms.Panel panel3;
		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.StatusStrip statusStrip1;
		private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
		private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel2;
		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Button button3;
		private System.Windows.Forms.Button button4;
	}
}

