using System;
using System.Windows;
using System.Windows.Controls;
using _3XSyncHub.Models;
using _3XSyncHub.Services;

namespace _3XSyncHub.Views
{
    // ============================================================================
    // 🗂️ Module: 3XSyncHub.UI.DbConfig.xaml.cs
    // 🔖 Version: 20251119_1144
    // 📌 Mục đích:
    // - Load / Save / Test Database MASTER & SLAVE
    // - View không log (Service tự log theo chuẩn 3XVN)
    // - Bind log realtime qua DataContext = DbConfigService
    // ============================================================================

    public partial class DbConfig : Page
    {
        private readonly DbConfigService service = new();

        public DbConfig()
        {
            InitializeComponent();
            DataContext = service;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var cfg = ConfigService.Load();

            // MASTER
            txtMasterHost.Text = cfg.DatabaseMaster.Host;
            txtMasterPort.Text = cfg.DatabaseMaster.Port;
            txtMasterUser.Text = cfg.DatabaseMaster.User;
            txtMasterPassword.Password = cfg.DatabaseMaster.Password;
            txtMasterDatabase.Text = cfg.DatabaseMaster.Database;

            // SLAVE
            txtSlaveHost.Text = cfg.DatabaseSlave.Host;
            txtSlavePort.Text = cfg.DatabaseSlave.Port;
            txtSlaveUser.Text = cfg.DatabaseSlave.User;
            txtSlavePassword.Password = cfg.DatabaseSlave.Password;
            txtSlaveDatabase.Text = cfg.DatabaseSlave.Database;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var master = new DatabaseMasterConfig
            {
                Host = txtMasterHost.Text.Trim(),
                Port = txtMasterPort.Text.Trim(),
                User = txtMasterUser.Text.Trim(),
                Password = txtMasterPassword.Password,
                Database = txtMasterDatabase.Text.Trim()
            };

            var slave = new DatabaseSlaveConfig
            {
                Host = txtSlaveHost.Text.Trim(),
                Port = txtSlavePort.Text.Trim(),
                User = txtSlaveUser.Text.Trim(),
                Password = txtSlavePassword.Password,
                Database = txtSlaveDatabase.Text.Trim()
            };

            service.SaveMasterAndSlave(master, slave);
        }

        private void BtnCheckConnection_Click(object sender, RoutedEventArgs e)
        {
            service.TestMasterSlave(
                txtMasterHost.Text.Trim(), txtMasterPort.Text.Trim(),
                txtMasterUser.Text.Trim(), txtMasterPassword.Password,
                txtMasterDatabase.Text.Trim(),

                txtSlaveHost.Text.Trim(), txtSlavePort.Text.Trim(),
                txtSlaveUser.Text.Trim(), txtSlavePassword.Password,
                txtSlaveDatabase.Text.Trim()
            );
        }
    }
}
