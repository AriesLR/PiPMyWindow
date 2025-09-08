using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace PipMyWindow
{
    public partial class PiPWindow : Window
    {
        private IntPtr sourceHandle;
        private IntPtr pipThumbnail = IntPtr.Zero;
        private PiPOverlayWindow pipOverlay;
        private double aspectRatio;

        public PiPWindow(IntPtr targetWindowHandle)
        {
            InitializeComponent();
            sourceHandle = targetWindowHandle;

            Topmost = true;
            ResizeMode = ResizeMode.CanResizeWithGrip;
            Loaded += PiPWindow_Loaded;

            this.SizeChanged += PiPWindow_SizeChanged;
        }

        private void PiPWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var wpfHandle = new WindowInteropHelper(this).Handle;
            if (pipThumbnail != IntPtr.Zero)
                DwmUnregisterThumbnail(pipThumbnail);

            DwmRegisterThumbnail(wpfHandle, sourceHandle, out pipThumbnail);
            UpdateThumbnail();

            // Sync window and DWM capture
            GetWindowRect(sourceHandle, out RECT windowRect);
            int windowWidth = windowRect.right - windowRect.left;
            int windowHeight = windowRect.bottom - windowRect.top;
            aspectRatio = (windowHeight > 0) ? (double)windowWidth / windowHeight : 1.0;

            if (windowWidth > 0 && windowHeight > 0)
            {
                double defaultWidth = 480;
                double initialWidth = defaultWidth;
                double initialHeight = defaultWidth / aspectRatio;

                if (initialWidth > windowWidth)
                {
                    initialWidth = windowWidth;
                    initialHeight = windowHeight;
                }

                this.Width = initialWidth;
                this.Height = initialHeight;
            }

            SizeChanged += (s, ev) => UpdateThumbnail();

            // Create PiP overlay
            pipOverlay = new PiPOverlayWindow
            {
                ParentWindow = this,
                MainAppWindow = Application.Current.MainWindow,
                Owner = this
            };

            // Set ownership to PiP window
            pipOverlay.Owner = this;

            UpdatepipOverlayPosition();

            // Show overlay
            pipOverlay.Topmost = true;
            pipOverlay.Show();

            // Move overlay when PiP moves/resizes
            this.LocationChanged += (s, ev) => UpdatepipOverlayPosition();
            this.SizeChanged += (s, ev) => UpdatepipOverlayPosition();

            // Close overlay when PiP closes
            this.Closed += (s, ev) => pipOverlay?.Close();
        }

        // More aspect ratio preservation
        private void PiPWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (aspectRatio <= 0 || double.IsNaN(aspectRatio) || double.IsInfinity(aspectRatio))
                return;

            double newWidth = e.NewSize.Width;
            double newHeight = newWidth / aspectRatio;

            if (Math.Abs(newHeight - e.NewSize.Height) > 1 && newHeight > 0 && double.IsFinite(newHeight))
            {
                this.Height = newHeight;
            }
            else
            {
                newHeight = e.NewSize.Height;
                newWidth = newHeight * aspectRatio;

                if (Math.Abs(newWidth - e.NewSize.Width) > 1 && newWidth > 0 && double.IsFinite(newWidth))
                {
                    this.Width = newWidth;
                }
            }

            UpdateThumbnail();
            UpdatepipOverlayPosition();
        }

        private void UpdatepipOverlayPosition()
        {
            if (pipOverlay == null) return;

            // Position top-right corner with 5px margin
            var point = this.PointToScreen(new Point(0, 0));
            pipOverlay.Left = point.X + this.ActualWidth - pipOverlay.Width - 5;
            pipOverlay.Top = point.Y + 5;
        }

        private void UpdateThumbnail()
        {
            if (pipThumbnail == IntPtr.Zero) return;

            GetWindowRect(sourceHandle, out RECT windowRect);
            int windowWidth = windowRect.right - windowRect.left;
            int windowHeight = windowRect.bottom - windowRect.top;

            if (windowWidth == 0 || windowHeight == 0) return;

            double scaleX = ActualWidth / windowWidth;
            double scaleY = ActualHeight / windowHeight;
            double scale = Math.Min(scaleX, scaleY);

            int destWidth = (int)(windowWidth * scale);
            int destHeight = (int)(windowHeight * scale);
            int destLeft = (int)((ActualWidth - destWidth) / 2);
            int destTop = (int)((ActualHeight - destHeight) / 2);

            var props = new DWM_THUMBNAIL_PROPERTIES
            {
                dwFlags = DWM_TNP_RECTDESTINATION | DWM_TNP_VISIBLE | DWM_TNP_OPACITY,
                fVisible = true,
                opacity = 255,
                rcDestination = new RECT
                {
                    left = destLeft,
                    top = destTop,
                    right = destLeft + destWidth,
                    bottom = destTop + destHeight
                }
            };

            DwmUpdateThumbnailProperties(pipThumbnail, ref props);
        }

        // DWM
        [DllImport("dwmapi.dll")] private static extern int DwmRegisterThumbnail(IntPtr dest, IntPtr src, out IntPtr thumb);

        [DllImport("dwmapi.dll")] private static extern int DwmUnregisterThumbnail(IntPtr hThumb);

        [DllImport("dwmapi.dll")] private static extern int DwmUpdateThumbnailProperties(IntPtr hThumb, ref DWM_THUMBNAIL_PROPERTIES props);

        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct DWM_THUMBNAIL_PROPERTIES
        {
            public uint dwFlags;
            public RECT rcDestination;
            public RECT rcSource;
            public byte opacity;
            [MarshalAs(UnmanagedType.Bool)] public bool fVisible;
            [MarshalAs(UnmanagedType.Bool)] public bool fSourceClientAreaOnly;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left, top, right, bottom;
        }

        private const int DWM_TNP_RECTDESTINATION = 0x00000001;
        private const int DWM_TNP_VISIBLE = 0x00000008;
        private const int DWM_TNP_OPACITY = 0x00000004;

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (pipThumbnail != IntPtr.Zero)
            {
                DwmUnregisterThumbnail(pipThumbnail);
                pipThumbnail = IntPtr.Zero;
            }

            pipOverlay?.Close();

            base.OnClosing(e);
        }
    }
}