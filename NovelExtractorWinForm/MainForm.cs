using ContentExtractor;
using System;
using System.Windows.Forms;

namespace NovelExtractor
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private FetchJob currJob;

        private void buttonExtract_Click(object sender, EventArgs e)
        {
            currJob = new FetchJob(
                (int)numericUpDownStartArg1.Value,
                (int)numericUpDownEndArg1.Value,
                int.Parse(textBoxThreadId.Text),
                textBoxKeyword.Text,
                textBoxUrl.Text);

            currJob.OnProcessStatus += CurrJob_OnProcessStatus;
            currJob.OnProcessCompleted += CurrJob_OnProcessCompleted;

            this.progressBar1.Value = 0;

            currJob?.ProcessAsync();
        }

        private void CurrJob_OnProcessStatus(double progressRate, string previewMsg)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(
                    (Action<double, string>) CurrJob_OnProcessStatus,
                    new object[]
                    {
                        progressRate,
                        previewMsg
                    });
            }
            else
            {
                this.richTextBoxPreview.Clear();
                this.richTextBoxPreview.AppendText(previewMsg);
                this.progressBar1.Value = (int) Math.Ceiling(progressRate * 100);
            }
        }

        private void CurrJob_OnProcessCompleted(bool isAllDone)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(
                    (Action<bool>)CurrJob_OnProcessCompleted,
                    new object[] {isAllDone});
            }
            else
            {

                if (isAllDone)
                    MessageBox.Show("執行完成，所有工作都已成功");
                else
                    MessageBox.Show("執行完成，但部份工作無法成功");
            }
        }
        
        private void buttonSave_Click(object sender, EventArgs e)
        {
            var result = currJob.SaveToFile(Application.StartupPath, this.textBoxFileName.Text) ? "成功" : "失敗";
            MessageBox.Show($"{textBoxFileName.Text} 存檔結果 {result}");
        }

        private void buttonReProcess_Click(object sender, EventArgs e)
        {
            currJob?.ProcessAsync();
        }
    }
}
