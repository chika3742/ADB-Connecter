using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ADB_Connecter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Properties.Settings settings = Properties.Settings.Default;
        public MainWindow()
        {
            InitializeComponent();
            AutoConnectCheckBox.IsChecked = settings.AutoConnect;
            TargetIdTextBox.Text = settings.TargetID;
            TargetIPAddressTextBox.Text = settings.TargetIPAddress;
            PortTextBox.Text = settings.Port;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (settings.AutoConnect)
            {
                Connect();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            settings.AutoConnect = (bool)AutoConnectCheckBox.IsChecked;
            settings.TargetID = TargetIdTextBox.Text;
            settings.TargetIPAddress = TargetIPAddressTextBox.Text;
            settings.Port = PortTextBox.Text;
            settings.Save();
        }

        private void ProceedBtn_Click(object sender, RoutedEventArgs e)
        {
            Connect();
        }

        async void Connect()
        {
            if (TargetIdTextBox.Text == "" || TargetIPAddressTextBox.Text == "" || PortTextBox.Text == "")
            {
                MessageBox.Show("すべての欄を入力してください", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var targetId = TargetIdTextBox.Text;
            var targetIPAddr = TargetIPAddressTextBox.Text;
            var port = PortTextBox.Text;
            await Task.Run(() =>
            {
                UpdateUI(false);

                var info = new ProcessStartInfo("adb");
                info.UseShellExecute = false;
                info.CreateNoWindow = true;
                info.RedirectStandardOutput = true;

                UpdateStatus("ADB起動中");
                info.Arguments = "start-server";
                Process.Start(info).WaitForExit();

                info.Arguments = "devices";
                var getDevices = Process.Start(info);
                getDevices.WaitForExit();
                var devices = getDevices.StandardOutput.ReadToEnd();

                if (devices.Contains(targetId))
                {
                    if (devices.Contains("unauthorized"))
                    {
                        MessageBox.Show("デバイスでADBの実行を許可してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                        UpdateUI(true);
                        return;
                    }
                    UpdateStatus("デバイスのポート開放中");
                    info.Arguments = $"-s {targetId} tcpip {port}";
                    Process.Start(info).WaitForExit();
                    Task.Delay(500);
                }

                UpdateStatus("デバイスに接続中");
                info.Arguments = $"connect {targetIPAddr}:{port}";
                var connectProcess = Process.Start(info);
                connectProcess.WaitForExit();
                var result = connectProcess.StandardOutput.ReadToEnd();

                if (result.Contains("connected to"))
                {
                    UpdateStatus("接続に成功しました。");
                }
                else
                {
                    if (result.Contains("10061"))
                    {
                        UpdateStatus("接続が拒否されました。");
                    }
                    else if (result.Contains("10060"))
                    {
                        UpdateStatus("接続に失敗しました。");
                        MessageBox.Show("IPアドレスが間違っていないか、同じLANに接続されているかを確認してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        UpdateStatus("接続に失敗しました。");
                        MessageBox.Show(result, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                UpdateUI(true);
                //UpdateStatus(devices);
            });
        }

        void UpdateStatus(string status)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = status;
            });
        }

        void UpdateUI(bool status)
        {
            Dispatcher.Invoke(() =>
            {
                TargetIdTextBox.IsEnabled = status;
                TargetIPAddressTextBox.IsEnabled = status;
                PortTextBox.IsEnabled = status;
                ProceedBtn.IsEnabled = status;
                CancelAutoConnectBtn.IsEnabled = status;
            });
        }

        private void PortTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = new Regex("\\D+").IsMatch(e.Text);
        }

        private void PortTextBox_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = e.Command == ApplicationCommands.Paste;
        }

        private void TargetIPAddressTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = new Regex("[^0-9.]").IsMatch(e.Text);
        }
    }
}
