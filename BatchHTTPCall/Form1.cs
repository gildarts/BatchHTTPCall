using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BatchHTTPCall
{
    public partial class Form1 : Form
    {
        private string URL = "https://dl.dropboxusercontent.com/u/16912100/Picture/Beautiful.jpg";

        private string folder = Application.StartupPath;

        private BackgroundWorker bgwork = new BackgroundWorker();

        public Form1()
        {
            InitializeComponent();
        }

        //設定 BackgroundWorker.
        private void Form1_Load(object sender, EventArgs e)
        {
            bgwork.DoWork += bgwork_DoWork;
            bgwork.RunWorkerCompleted += bgwork_RunWorkerCompleted;
            bgwork.ProgressChanged += bgwork_ProgressChanged;
            bgwork.WorkerReportsProgress = true;
        }

        private void run_Click(object sender, EventArgs e)
        {
            if (bgwork.IsBusy)
                return;

            //隨機產生一堆 HTTP 呼叫的 Action。
            List<HttpAction> actions = new List<HttpAction>();
            for (int i = 1; i <= int.Parse(total.Text); i++)
            {
                //Url 屬性存放的是最後要呼叫的 Url，一般狀況是每個 HttpAction 都會不一樣。
                actions.Add(new HttpAction() { IdNumber = i, Url = this.URL });
            }

            progress.Value = 0;
            bgwork.RunWorkerAsync(actions);
        }

        private void bgwork_DoWork(object sender, DoWorkEventArgs e)
        {
            List<HttpAction> actions = e.Argument as List<HttpAction>;

            int successCount = 0; //用於計算進度。

            //平行迴圈，迴圈內部的程式碼有可能同時執行，達到加速效果。
            Parallel.ForEach<HttpAction>(actions, (action) =>
            {
                SendByHttp(action); //依 Action 設定，呼叫 HTTP

                //類似 successCount++，但這是多緒版的，如果不這樣做會結果不正確。
                Interlocked.Increment(ref successCount);

                decimal seed = (decimal)successCount / actions.Count;
                bgwork.ReportProgress((int)(seed * 100), action); //把 action 傳出去是用於在畫面顯示資訊。
            });
        }

        //呼叫 Http 的位置。
        private void SendByHttp(HttpAction action)
        {
            //Dropbox 本來就慢，所以這裡很慢是正常的。
            HttpWebRequest req = WebRequest.Create(action.Url) as HttpWebRequest;
            HttpWebResponse rsp = req.GetResponse() as HttpWebResponse;

            //從 Server 回傳的資料。
            Stream data = rsp.GetResponseStream(); 

            //資料會寫到這個檔案。
            FileStream fs = new FileStream(Path.Combine(folder, action.IdNumber.ToString() + ".jpg"), FileMode.Create);

            int bufferSize = 1024, readSize = 0;
            byte[] buffer = new byte[bufferSize];
            while ((readSize = data.Read(buffer, 0, bufferSize)) > 0)
            {
                fs.Write(buffer, 0, readSize);
            }

            fs.Close();
            rsp.Close();
        }

        private void bgwork_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progress.Value = 100;
        }

        private void bgwork_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            HttpAction ha = e.UserState as HttpAction;

            log.AppendText(string.Format("{0} 已完成！\n", ha.IdNumber));

            progress.Value = e.ProgressPercentage;

        }

        //驗證 TextBox 一定要數字...
        private void total_Validating(object sender, CancelEventArgs e)
        {
            int num;

            if (!int.TryParse(total.Text, out num))
            {
                e.Cancel = true;
                MessageBox.Show("炸！");
            }
        }
    }
}
