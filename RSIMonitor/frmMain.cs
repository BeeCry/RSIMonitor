using Binance.Net.Enums;
using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Lib;
using Lib.Models;

namespace RSIMonitor
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
            Initialize();
        }

        private delegate void SafeCallDelegate();

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private List<CustomBinanceTicket> _customTickets = new List<CustomBinanceTicket>();

        void Initialize()
        {
            //NLog
            var config = new NLog.Config.LoggingConfiguration();
            var logfile = new NLog.Targets.FileTarget("logfile") { FileName = "log.txt" };
            config.AddRuleForAllLevels(logfile);
            LogManager.Configuration = config;

            //DGV
            dgvData.BackgroundColor = Color.White;
            dgvData.RowHeadersVisible = false;
            dgvData.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.Fill);

            //Init Data
            _customTickets = Worker.GetAllPrices((int)nudTop.Value);
            Worker.Initialize(_customTickets, ReceiveData);
            Logger.Info("Total Ticket: " + _customTickets.Count);

            Text = "RSI Monitor by BeeCry!";
            TopMost = true;
        }


        void ReceiveData(List<CustomBinanceTicket> customTickets)
        {
            _customTickets = customTickets;
            UpdateDataSourceSafe();
        }

        private void UpdateDataSourceSafe()
        {
            if (dgvData.InvokeRequired)
            {
                var d = new SafeCallDelegate(UpdateDataSourceSafe);
                dgvData.Invoke(d);
            }
            else
            {
                dgvData.DataSource = null;
                dgvData.DataSource = _customTickets.Select(x => new
                {
                    x.Pair,
                    Price = x.LastPrice.ToRoundPrice(),
                    RSI5m = x.RSIs[KlineInterval.FiveMinutes].Value.ToString("N2"),
                    RSI30m = x.RSIs[KlineInterval.ThirtyMinutes]?.Value.ToString("N2") ?? "",
                    RSI1h = x.RSIs[KlineInterval.OneHour]?.Value.ToString("N2") ?? "",
                    RSI4h = x.RSIs[KlineInterval.FourHour]?.Value.ToString("N2") ?? "",
                    RSI1d = x.RSIs[KlineInterval.OneDay]?.Value.ToString("N2") ?? "",
                }).ToArray();
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            btnStart.Enabled = false;
            Task.Run(Worker.DoWork);
        }

        private void btnRestart_Click(object sender, EventArgs e)
        {
            Application.Restart();
        }
    }
}
