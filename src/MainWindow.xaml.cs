using MahApps.Metro.Controls;
using PipMyWindow.Resources.Functions.Services;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace PipMyWindow
{
    public partial class MainWindow : MetroWindow
    {
        // Store process handles
        private IntPtr selectedHandle = IntPtr.Zero;

        public MainWindow()
        {
            InitializeComponent();
            ListRunningProcesses();
        }

        // Open PipMyWindow's GitHub Repo in the user's default browser
        private void LaunchBrowserGitHubPipMyWindow(object sender, RoutedEventArgs e)
        {
            UrlService.OpenUrlAsync("https://github.com/AriesLR/PiPMyWindow");
        }

        // Check for updates via json
        private async void CheckForUpdates(object sender, RoutedEventArgs e)
        {
            await UpdateService.CheckForUpdatesAsync("https://raw.githubusercontent.com/AriesLR/PiPMyWindow/refs/heads/main/docs/version/update.json");
        }

        // Import Win32 API for window handling
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;

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

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProcessListComboBox.SelectedItem is Tuple<IntPtr, string> selected)
            {
                selectedHandle = selected.Item1;

                // Restore and bring the selected window forward
                ShowWindow(selectedHandle, SW_RESTORE);
                SetForegroundWindow(selectedHandle);

                // Enter "PIP mode"
                ControlPanel.Visibility = Visibility.Collapsed;
                PipContent.Visibility = Visibility.Visible;

                this.Topmost = true;
                this.ResizeMode = ResizeMode.NoResize;
            }
            else
            {
                MessageBox.Show("Please select a window first.");
            }
        }
    }
}