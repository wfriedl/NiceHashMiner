﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace NiceHashMiner
{
    public partial class Form2 : Form
    {
        private int index;
        private bool inBenchmark;

        private int Time = Config.ConfigData.BenchmarkTimeLimits[1];
        private Miner CurrentlyBenchmarking;

        public Form2(bool autostart)
        {
            InitializeComponent();

            foreach (Miner m in Form1.Miners)
            {
                for (int i = 0; i < m.SupportedAlgorithms.Length; i++)
                {
                    ListViewItem lvi = new ListViewItem();
                    lvi.Checked = !m.SupportedAlgorithms[i].Skip;
                    if (m.EnabledDeviceCount() == 0)
                    {
                        lvi.Checked = false;
                        lvi.BackColor = Color.LightGray;
                    }
                    lvi.SubItems.Add(m.MinerDeviceName);
                    ListViewItem.ListViewSubItem sub = lvi.SubItems.Add(m.SupportedAlgorithms[i].NiceHashName);
                    sub.Tag = i;
                    if (m.SupportedAlgorithms[i].BenchmarkSpeed > 0)
                        lvi.SubItems.Add(m.PrintSpeed(m.SupportedAlgorithms[i].BenchmarkSpeed));
                    else
                        lvi.SubItems.Add("");
                    lvi.Tag = m;
                    listView1.Items.Add(lvi);
                }
            }

            inBenchmark = false;

            if (autostart)
                button1_Click(null, null);
        }


        private void BenchmarkCompleted(bool success, string text, object tag)
        {
            if (this.InvokeRequired)
            {
                BenchmarkComplete d = new BenchmarkComplete(BenchmarkCompleted);
                this.Invoke(d, new object[] { success, text, tag });
            }
            else
            {
                inBenchmark = false;
                CurrentlyBenchmarking = null;

                ListViewItem lvi = tag as ListViewItem;
                lvi.SubItems[3].Text = text;

                // initiate new benchmark
                InitiateBenchmark();
            }
        }


        private void InitiateBenchmark()
        {
            if (listView1.Items.Count > index)
            {
                ListViewItem lvi = listView1.Items[index];
                index++;

                if (!lvi.Checked)
                {
                    InitiateBenchmark();
                    return;
                }

                Miner m = lvi.Tag as Miner;
                int i = (int)lvi.SubItems[2].Tag;
                lvi.SubItems[3].Text = "Please wait...";
                inBenchmark = true;
                CurrentlyBenchmarking = m;
                m.BenchmarkStart(i, Time, BenchmarkCompleted, lvi);
            }
            else
            {
                // average all cpu benchmarks
                if (Form1.Miners[0] is cpuminer)
                {
                    Helpers.ConsolePrint("Calculating average CPU speeds:");

                    double[] Speeds = new double[Form1.Miners[0].SupportedAlgorithms.Length];
                    int[] MTaken = new int[Form1.Miners[0].SupportedAlgorithms.Length];

                    foreach (ListViewItem lvi in listView1.Items)
                    {
                        if (lvi.Tag is cpuminer)
                        {
                            Miner m = lvi.Tag as Miner;
                            int i = (int)lvi.SubItems[2].Tag;
                            if (m.SupportedAlgorithms[i].BenchmarkSpeed > 0)
                            {
                                Speeds[i] += m.SupportedAlgorithms[i].BenchmarkSpeed;
                                MTaken[i]++;
                            }
                        }
                    }

                    for (int i = 0; i < Speeds.Length; i++)
                    {
                        if (MTaken[i] > 0) Speeds[i] /= MTaken[i];
                        Helpers.ConsolePrint(Form1.Miners[0].SupportedAlgorithms[i].NiceHashName + " average speed: " + Form1.Miners[0].PrintSpeed(Speeds[i]));

                        foreach (Miner m in Form1.Miners)
                        {
                            if (m is cpuminer)
                                m.SupportedAlgorithms[i].BenchmarkSpeed = Speeds[i];
                        }
                    }
                }

                foreach (ListViewItem lvi in listView1.Items)
                {
                    Miner m = lvi.Tag as Miner;
                    int i = (int)lvi.SubItems[2].Tag;
                    lvi.SubItems[3].Text = m.PrintSpeed(m.SupportedAlgorithms[i].BenchmarkSpeed);
                }
                
                Config.RebuildGroups();

                button1.Enabled = true;
                button2.Enabled = false;
                button3.Enabled = true;
            }
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (inBenchmark) e.Cancel = true;
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            Time = Config.ConfigData.BenchmarkTimeLimits[0];
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            Time = Config.ConfigData.BenchmarkTimeLimits[1];
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            Time = Config.ConfigData.BenchmarkTimeLimits[2];
        }

        private void button1_Click(object sender, EventArgs e)
        {
            index = 0;
            button1.Enabled = false;
            button2.Enabled = true;
            button3.Enabled = false;
            InitiateBenchmark();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (CurrentlyBenchmarking != null)
                CurrentlyBenchmarking.BenchmarkSignalQuit = true;
            index = 9999;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            foreach (Miner m in Form1.Miners)
            {
                for (int i = 0; i < m.SupportedAlgorithms.Length; i++)
                {
                    m.SupportedAlgorithms[i].BenchmarkSpeed = 0;
                }
            }

            Config.RebuildGroups();

            foreach (ListViewItem lvi in listView1.Items)
            {
                lvi.SubItems[3].Text = "";
            }
        }

        private void listView1_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            Miner m = e.Item.Tag as Miner;
            if (m.EnabledDeviceCount() == 0)
                e.Item.Checked = false;
            else
            {
                int i = (int)e.Item.SubItems[2].Tag;
                m.SupportedAlgorithms[i].Skip = !e.Item.Checked;
                Config.RebuildGroups();
            }
        }
    }
}
