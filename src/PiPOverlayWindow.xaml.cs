using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace PipMyWindow
{
    public partial class PiPOverlayWindow : Window
    {
        public Window ParentWindow { get; set; }

        public Window MainAppWindow { get; set; }

        // Drag tracking
        private bool isDragging = false;

        private Point clickOffset;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        public PiPOverlayWindow()
        {
            InitializeComponent();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            // Close the PiP window
            ParentWindow?.Close();

            // Close this overlay window
            this.Close();

            // The whole section below tries to force the main window back into focus when the PiP window is closed
            // Activate the main window slightly later to ensure Windows allows focus
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (MainAppWindow != null)
                {
                    // Restore if minimized
                    if (MainAppWindow.WindowState == WindowState.Minimized)
                        MainAppWindow.WindowState = WindowState.Normal;

                    // WPF-level activation and keyboard focus
                    MainAppWindow.Activate();
                    MainAppWindow.Focus();

                    // Force OS-level focus
                    SetForegroundWindow(new System.Windows.Interop.WindowInteropHelper(MainAppWindow).Handle);
                }
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        // Drag handlers
        private void DragButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ParentWindow == null) return;

            isDragging = true;
            DragButton.CaptureMouse();

            // Calculate offset between mouse and parent window top-left
            var mousePos = e.GetPosition(ParentWindow);
            clickOffset = mousePos;

            e.Handled = true;
        }

        private void DragButton_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && ParentWindow != null)
            {
                var screenPos = PointToScreen(e.GetPosition(this));
                ParentWindow.Left = screenPos.X - clickOffset.X;
                ParentWindow.Top = screenPos.Y - clickOffset.Y;
            }
        }

        private void DragButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                DragButton.ReleaseMouseCapture();
            }
        }
    }
}