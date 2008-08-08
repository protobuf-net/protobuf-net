using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WcfClient.NWind;
using System.Diagnostics;

namespace WcfClient
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private string ConfigName
        {
            get {
                string config = checkBox1.Checked ? "nwindMtom" : "nwindText";
                if (checkBox2.Checked) config += "Remote";
                return config;
            }
        }

        private volatile int cheekyCount;
        private void button2_Click(object sender, EventArgs e)
        {
            
            numericUpDown1.Enabled = checkBox2.Enabled =
                checkBox1.Enabled = button2.Enabled = false;
            progressBar1.Value = 0;
            progressBar1.Visible = true;
            Text = "Stress started";
            cheekyCount = (int)numericUpDown1.Value;
            backgroundWorker1.RunWorkerAsync(ConfigName);
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            string config = Convert.ToString(e.Argument);
            using (var client = new NWindServiceClient(config))
            {
                OrderSet set = client.LoadFoo(), copy;

                int count = cheekyCount;
                copy = set;
                string key = "Foo via " + config;
                Stopwatch fooWatch = Stopwatch.StartNew();
                for (int i = 0; i < count; i++)
                {
                    copy = client.RoundTripFoo(copy);
                    if (i % 10 == 0)
                    {
                        backgroundWorker1.ReportProgress((i * 100) / count, key);
                    }
                }
                fooWatch.Stop();
                copy = set;
                key = "Bar via " + config;
                Stopwatch barWatch = Stopwatch.StartNew();
                for (int i = 0; i < count; i++)
                {
                    copy = client.RoundTripBar(copy);
                    if (i % 10 == 0)
                    {
                        backgroundWorker1.ReportProgress((i * 100) / count, key);
                    }
                }
                barWatch.Stop();

                decimal pc = (barWatch.ElapsedMilliseconds * 100.0M) / fooWatch.ElapsedMilliseconds;

                e.Result = string.Format("x{2} via {3} - Foo: {0:###,##0}ms, Bar: {1:###,##0}ms ({4:##.#}%)",
                    fooWatch.ElapsedMilliseconds, barWatch.ElapsedMilliseconds, count, config, pc);
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Text = string.Format("{0}: {1}%", e.UserState, e.ProgressPercentage);
            progressBar1.Value = e.ProgressPercentage;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Text = "Stress complete: " + Convert.ToString(e.Error ?? e.Result);
            progressBar1.Visible = false;
            numericUpDown1.Enabled = checkBox2.Enabled = 
                checkBox1.Enabled = button2.Enabled = true;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            using (var client = new NWindServiceClient(ConfigName))
            {
                nwindSource.DataSource = client.LoadFoo();
                Text = "Loaded Foo via " + ConfigName;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            using (var client = new NWindServiceClient(ConfigName))
            {
                nwindSource.DataSource = client.LoadBar();
                Text = "Loaded Bar via " + ConfigName;
            }
        }
    }
}
