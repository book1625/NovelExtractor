namespace NovelExtractor
{
    partial class MainForm
    {
        /// <summary>
        /// 設計工具所需的變數。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清除任何使用中的資源。
        /// </summary>
        /// <param name="disposing">如果應該處置 Managed 資源則為 true，否則為 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 設計工具產生的程式碼

        /// <summary>
        /// 此為設計工具支援所需的方法 - 請勿使用程式碼編輯器
        /// 修改這個方法的內容。
        /// </summary>
        private void InitializeComponent()
        {
            this.buttonExtract = new System.Windows.Forms.Button();
            this.textBoxUrl = new System.Windows.Forms.TextBox();
            this.textBoxKeyword = new System.Windows.Forms.TextBox();
            this.numericUpDownStartArg1 = new System.Windows.Forms.NumericUpDown();
            this.numericUpDownEndArg1 = new System.Windows.Forms.NumericUpDown();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.richTextBoxPreview = new System.Windows.Forms.RichTextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.textBoxThreadId = new System.Windows.Forms.TextBox();
            this.panel3 = new System.Windows.Forms.Panel();
            this.buttonSave = new System.Windows.Forms.Button();
            this.textBoxFileName = new System.Windows.Forms.TextBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.buttonReProcess = new System.Windows.Forms.Button();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownStartArg1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownEndArg1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.panel3.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonExtract
            // 
            this.buttonExtract.Dock = System.Windows.Forms.DockStyle.Left;
            this.buttonExtract.Location = new System.Drawing.Point(0, 0);
            this.buttonExtract.Margin = new System.Windows.Forms.Padding(6);
            this.buttonExtract.Name = "buttonExtract";
            this.buttonExtract.Size = new System.Drawing.Size(162, 51);
            this.buttonExtract.TabIndex = 0;
            this.buttonExtract.Text = "Extract";
            this.buttonExtract.UseVisualStyleBackColor = true;
            this.buttonExtract.Click += new System.EventHandler(this.buttonExtract_Click);
            // 
            // textBoxUrl
            // 
            this.textBoxUrl.Location = new System.Drawing.Point(9, 6);
            this.textBoxUrl.Margin = new System.Windows.Forms.Padding(6);
            this.textBoxUrl.Name = "textBoxUrl";
            this.textBoxUrl.Size = new System.Drawing.Size(647, 36);
            this.textBoxUrl.TabIndex = 2;
            this.textBoxUrl.Text = "https://ck101.com/forum.php?mod=viewthread&tid={0}&page={1}";
            // 
            // textBoxKeyword
            // 
            this.textBoxKeyword.Location = new System.Drawing.Point(995, 6);
            this.textBoxKeyword.Margin = new System.Windows.Forms.Padding(6);
            this.textBoxKeyword.Name = "textBoxKeyword";
            this.textBoxKeyword.Size = new System.Drawing.Size(136, 36);
            this.textBoxKeyword.TabIndex = 7;
            this.textBoxKeyword.Text = "postmessage";
            // 
            // numericUpDownStartArg1
            // 
            this.numericUpDownStartArg1.Location = new System.Drawing.Point(819, 6);
            this.numericUpDownStartArg1.Margin = new System.Windows.Forms.Padding(4);
            this.numericUpDownStartArg1.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numericUpDownStartArg1.Name = "numericUpDownStartArg1";
            this.numericUpDownStartArg1.Size = new System.Drawing.Size(79, 36);
            this.numericUpDownStartArg1.TabIndex = 10;
            this.numericUpDownStartArg1.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // numericUpDownEndArg1
            // 
            this.numericUpDownEndArg1.Location = new System.Drawing.Point(906, 6);
            this.numericUpDownEndArg1.Margin = new System.Windows.Forms.Padding(4);
            this.numericUpDownEndArg1.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numericUpDownEndArg1.Name = "numericUpDownEndArg1";
            this.numericUpDownEndArg1.Size = new System.Drawing.Size(79, 36);
            this.numericUpDownEndArg1.TabIndex = 11;
            this.numericUpDownEndArg1.Value = new decimal(new int[] {
            4,
            0,
            0,
            0});
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.richTextBoxPreview);
            this.splitContainer1.Panel1.Controls.Add(this.panel1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.panel3);
            this.splitContainer1.Panel2.Controls.Add(this.panel2);
            this.splitContainer1.Size = new System.Drawing.Size(1876, 1125);
            this.splitContainer1.SplitterDistance = 1003;
            this.splitContainer1.TabIndex = 14;
            // 
            // richTextBoxPreview
            // 
            this.richTextBoxPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBoxPreview.Location = new System.Drawing.Point(0, 51);
            this.richTextBoxPreview.Margin = new System.Windows.Forms.Padding(6);
            this.richTextBoxPreview.Name = "richTextBoxPreview";
            this.richTextBoxPreview.Size = new System.Drawing.Size(1876, 952);
            this.richTextBoxPreview.TabIndex = 2;
            this.richTextBoxPreview.Text = "";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.textBoxThreadId);
            this.panel1.Controls.Add(this.textBoxUrl);
            this.panel1.Controls.Add(this.textBoxKeyword);
            this.panel1.Controls.Add(this.numericUpDownEndArg1);
            this.panel1.Controls.Add(this.numericUpDownStartArg1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1876, 51);
            this.panel1.TabIndex = 0;
            // 
            // textBoxThreadId
            // 
            this.textBoxThreadId.Location = new System.Drawing.Point(666, 5);
            this.textBoxThreadId.Name = "textBoxThreadId";
            this.textBoxThreadId.Size = new System.Drawing.Size(146, 36);
            this.textBoxThreadId.TabIndex = 12;
            this.textBoxThreadId.Text = "4757264";
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.buttonSave);
            this.panel3.Controls.Add(this.textBoxFileName);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel3.Location = new System.Drawing.Point(0, 51);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(1876, 47);
            this.panel3.TabIndex = 2;
            // 
            // buttonSave
            // 
            this.buttonSave.Dock = System.Windows.Forms.DockStyle.Left;
            this.buttonSave.Location = new System.Drawing.Point(0, 0);
            this.buttonSave.Margin = new System.Windows.Forms.Padding(4);
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Size = new System.Drawing.Size(198, 47);
            this.buttonSave.TabIndex = 14;
            this.buttonSave.Text = "Save to File";
            this.buttonSave.UseVisualStyleBackColor = true;
            this.buttonSave.Click += new System.EventHandler(this.buttonSave_Click);
            // 
            // textBoxFileName
            // 
            this.textBoxFileName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxFileName.Font = new System.Drawing.Font("新細明體", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.textBoxFileName.Location = new System.Drawing.Point(206, 4);
            this.textBoxFileName.Margin = new System.Windows.Forms.Padding(4);
            this.textBoxFileName.Name = "textBoxFileName";
            this.textBoxFileName.Size = new System.Drawing.Size(252, 39);
            this.textBoxFileName.TabIndex = 15;
            this.textBoxFileName.Text = "FileName";
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.buttonReProcess);
            this.panel2.Controls.Add(this.progressBar1);
            this.panel2.Controls.Add(this.buttonExtract);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(1876, 51);
            this.panel2.TabIndex = 0;
            // 
            // buttonReProcess
            // 
            this.buttonReProcess.Location = new System.Drawing.Point(1307, 0);
            this.buttonReProcess.Name = "buttonReProcess";
            this.buttonReProcess.Size = new System.Drawing.Size(195, 51);
            this.buttonReProcess.TabIndex = 7;
            this.buttonReProcess.Text = "ReExtract";
            this.buttonReProcess.UseVisualStyleBackColor = true;
            this.buttonReProcess.Click += new System.EventHandler(this.buttonReProcess_Click);
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(171, 0);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(1130, 48);
            this.progressBar1.TabIndex = 6;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(13F, 24F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1876, 1125);
            this.Controls.Add(this.splitContainer1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Margin = new System.Windows.Forms.Padding(6);
            this.Name = "MainForm";
            this.Text = "NovelExtractor";
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownStartArg1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownEndArg1)).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button buttonExtract;
        private System.Windows.Forms.TextBox textBoxUrl;
        private System.Windows.Forms.TextBox textBoxKeyword;
        private System.Windows.Forms.NumericUpDown numericUpDownStartArg1;
        private System.Windows.Forms.NumericUpDown numericUpDownEndArg1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Button buttonSave;
        private System.Windows.Forms.TextBox textBoxFileName;
        private System.Windows.Forms.TextBox textBoxThreadId;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Button buttonReProcess;
        private System.Windows.Forms.RichTextBox richTextBoxPreview;
    }
}

