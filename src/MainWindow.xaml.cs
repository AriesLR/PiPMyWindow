using MahApps.Metro.Controls;
using PipMyWindow.Resources.Functions.Services;
using System.Diagnostics;
using System.Windows;

namespace PipMyWindow
{
    public partial class MainWindow : MetroWindow
    {
        private IntPtr selectedHandle = IntPtr.Zero;
        private PiPWindow pipWindow;
        private PiPOverlayWindow pipOverlay;

        public MainWindow()
        {
            InitializeComponent();
            ListRunningProcesses();
        }

        // Open Github Repo
        private void LaunchBrowserGitHubPipMyWindow(object sender, RoutedEventArgs e)
        {
            UrlService.OpenUrlAsync("https://github.com/AriesLR/PiPMyWindow");
        }

        // Check For Updates
        private async void CheckForUpdates(object sender, RoutedEventArgs e)
        {
            await UpdateService.CheckForUpdatesAsync("https://raw.githubusercontent.com/AriesLR/PiPMyWindow/refs/heads/main/docs/version/update.json");
        }

        private void ListRunningProcesses()
        {
            var processes = Process.GetProcesses()
                .Where(p => p.MainWindowHandle != IntPtr.Zero && !string.IsNullOrWhiteSpace(p.MainWindowTitle))
                .Select(p => new Tuple<IntPtr, string>(p.MainWindowHandle, p.MainWindowTitle))
                .ToList();

            ProcessListComboBox.ItemsSource = processes;
            if (processes.Count > 0)
                ProcessListComboBox.SelectedIndex = 0;
        }

        private void ListRunningProcesses(object sender, RoutedEventArgs e) => ListRunningProcesses();

        // Start PiP
        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProcessListComboBox.SelectedItem is Tuple<IntPtr, string> selected)
            {
                selectedHandle = selected.Item1;

                // Hide main UI
                ControlPanel.Visibility = Visibility.Collapsed;
                StartButton.Visibility = Visibility.Collapsed;

                // Open PiP window
                pipWindow = new PiPWindow(selectedHandle);
                pipWindow.Show();

                // Open PiP overlay
                pipOverlay = new PiPOverlayWindow
                {
                    ParentWindow = pipWindow,
                    MainAppWindow = this
                };
                UpdateOverlayPosition();
                pipOverlay.Show();

                // Keep overlay synced with PiP window
                pipWindow.LocationChanged += (s, ev) => UpdateOverlayPosition();
                pipWindow.SizeChanged += (s, ev) => UpdateOverlayPosition();

                // Restore main UI, close overlay, and refocus main window when PiP closes
                pipWindow.Closed += (s, ev) =>
                {
                    ControlPanel.Visibility = Visibility.Visible;
                    StartButton.Visibility = Visibility.Visible;

                    // Mostly a redundancy
                    if (pipOverlay != null)
                    {
                        pipOverlay.Closed += (_, __) =>
                        {
                            Dispatcher.BeginInvoke(new Action(() => this.Activate()));
                        };
                        pipOverlay.Close();
                        pipOverlay = null;
                    }
                    else
                    {
                        Dispatcher.BeginInvoke(new Action(() => this.Activate()));
                    }
                };
            }
            else
            {
                await MessageService.ShowError("Please select a window first.");
            }
        }

        private void UpdateOverlayPosition()
        {
            if (pipWindow == null || pipOverlay == null) return;

            pipOverlay.Left = pipWindow.Left + pipWindow.Width - pipOverlay.Width - 5;
            pipOverlay.Top = pipWindow.Top + 5;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            pipWindow?.Close();
            pipOverlay?.Close();
            base.OnClosing(e);
        }
    }
}