#nullable enable
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ManagedShell.Common.Helpers;
using ManagedShell.Interop;
using RetroBar.Utilities;

namespace RetroBar.Controls
{
    /// <summary>
    /// Interaction logic for StartButton.xaml
    /// </summary>
    public partial class StartButton : UserControl
    {
        private FloatingStartButton? floatingStartButton;
        private bool allowOpenStart;
        private bool visibilityChanged;
        private DelayedActivationHandler? dragHandler;
        private readonly DispatcherTimer pendingOpenTimer;

        public static DependencyProperty HostProperty = DependencyProperty.Register(nameof(Host), typeof(Taskbar), typeof(StartButton));
        public static DependencyProperty StartMenuMonitorProperty = DependencyProperty.Register(nameof(StartMenuMonitor), typeof(StartMenuMonitor), typeof(StartButton));

        public Taskbar Host
        {
            get { return (Taskbar)GetValue(HostProperty); }
            set { SetValue(HostProperty, value); }
        }

        public StartMenuMonitor StartMenuMonitor
        {
            get { return (StartMenuMonitor)GetValue(StartMenuMonitorProperty); }
            set { SetValue(StartMenuMonitorProperty, value); }
        }

        public StartButton()
        {
            InitializeComponent();

            SpeechBubble.CustomPopupPlacementCallback = PlaceSpeechBubble;

            pendingOpenTimer = new DispatcherTimer(DispatcherPriority.Background);
            pendingOpenTimer.Interval = new TimeSpan(0, 0, 0, 1);
            pendingOpenTimer.Tick += (sender, args) =>
            {
                // if the start menu didn't open, flip the button back to unchecked
                SetStartMenuState(false);
            };
        }

        private void Settings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Settings.Theme))
            {
                bool useFloatingStartButton = Application.Current.FindResource("UseFloatingStartButton") as bool? ?? false;

                if (!useFloatingStartButton && floatingStartButton != null)
                {
                    closeFloatingStart();
                }
            }
        }

        public void SetStartMenuState(bool opened)
        {
            Dispatcher.Invoke(() =>
            {
                Start.IsChecked = opened;
                Host?.SetStartMenuOpen(opened);
            });
            pendingOpenTimer.Stop();
        }

        #region Speech bubble (WorkWindowsBar)

        private bool BubbleAbove => Settings.Instance.Edge != ManagedShell.AppBar.AppBarEdge.Top;

        // Centered over the button; below it (without tail) when the taskbar is at the top.
        private CustomPopupPlacement[] PlaceSpeechBubble(Size popupSize, Size targetSize, Point offset)
        {
            double x = (targetSize.Width - popupSize.Width) / 2;
            double y = BubbleAbove ? -popupSize.Height + 4 : targetSize.Height - 4;
            return new[] { new CustomPopupPlacement(new Point(x, y), PopupPrimaryAxis.Horizontal) };
        }

        private void Start_OnMouseEnter(object sender, MouseEventArgs e)
        {
            string text = Settings.Instance.StartIconHoverText;
            bool enabled = !string.IsNullOrWhiteSpace(text);

            // The bubble replaces the regular tooltip while it is enabled.
            ToolTipService.SetIsEnabled(Start, !enabled);

            if (!enabled || Start.IsChecked == true)
            {
                return;
            }

            SpeechBubbleText.Text = text;
            SpeechBubbleTail.Visibility = BubbleAbove ? Visibility.Visible : Visibility.Collapsed;
            SpeechBubbleRoot.RenderTransformOrigin = BubbleAbove ? new Point(0.5, 1) : new Point(0.5, 0);
            SpeechBubble.IsOpen = true;

            // Little "pop" in
            var ease = new System.Windows.Media.Animation.BackEase { Amplitude = 0.4, EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut };
            var grow = new System.Windows.Media.Animation.DoubleAnimation(0.5, 1, TimeSpan.FromMilliseconds(180)) { EasingFunction = ease };
            var fade = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(120));
            SpeechBubbleScale.BeginAnimation(ScaleTransform.ScaleXProperty, grow);
            SpeechBubbleScale.BeginAnimation(ScaleTransform.ScaleYProperty, grow);
            SpeechBubbleRoot.BeginAnimation(OpacityProperty, fade);
        }

        private void Start_OnMouseLeave(object sender, MouseEventArgs e)
        {
            SpeechBubble.IsOpen = false;
        }

        #endregion

        private void Start_OnClick(object sender, RoutedEventArgs e)
        {
            SpeechBubble.IsOpen = false;

            if (allowOpenStart)
            {
                OpenStartMenu();
                return;
            }

            SetStartMenuState(false);
        }

        private void OpenStartMenu()
        {
            Host?.SetTrayHost();
            Host?.SetStartMenuOpen(true);
            pendingOpenTimer.Start();
            if (Host != null && StartMenuMonitor != null && Settings.Instance.ShowMultiMon && Settings.Instance.ShowStartButtonMultiMon)
            {
                StartMenuMonitor.ShowStartMenu(Host.Handle);
            }
            else
            {
                ShellHelper.ShowStartMenu();
            }
        }

        private void Start_DragEnter(object sender, DragEventArgs e)
        {
            dragHandler?.OnDragEnter(e);
        }

        private void Start_DragLeave(object sender, DragEventArgs e)
        {
            dragHandler?.OnDragLeave();
        }

        private void Start_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            allowOpenStart = Start.IsChecked == false;
        }

        private void Start_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (EnvironmentHelper.IsWindows10OrBetter)
            {
                ShellHelper.ShowStartContextMenu();
                e.Handled = true;
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            StartMenuMonitor.StartMenuVisibilityChanged += AppVisibilityHelper_StartMenuVisibilityChanged;

            Settings.Instance.PropertyChanged += Settings_PropertyChanged;

            dragHandler = new DelayedActivationHandler(() =>
            {
                if (Start.IsChecked == false)
                {
                    OpenStartMenu();
                }
            });

            openFloatingStart();

            IsVisibleChanged += StartButton_IsVisibleChanged;
            LayoutUpdated += StartButton_LayoutUpdated;

            if (Host != null)
            {
                Host.PropertyChanged += Taskbar_PropertyChanged;
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            StartMenuMonitor.StartMenuVisibilityChanged -= AppVisibilityHelper_StartMenuVisibilityChanged;

            if (Host != null)
            {
                Host.PropertyChanged -= Taskbar_PropertyChanged;
            }

            Settings.Instance.PropertyChanged -= Settings_PropertyChanged;
            dragHandler?.Dispose();

            hideFloatingStart();
        }

        private void AppVisibilityHelper_StartMenuVisibilityChanged(object? sender, StartMenuMonitor.StartMenuMonitorEventArgs e)
        {
            if (e.Visible && e.TaskbarHwndActivated != IntPtr.Zero && Host != null && e.TaskbarHwndActivated != Host.Handle)
            {
                // Only set as visible when activated from our taskbar
                return;
            }
            SetStartMenuState(e.Visible);
        }

        private void StartButton_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            visibilityChanged = true;
        }

        private void StartButton_LayoutUpdated(object? sender, EventArgs e)
        {
            if (!visibilityChanged)
            {
                UpdateFloatingStartCoordinates();
                return;
            }

            visibilityChanged = false;

            if (IsVisible)
            {
                openFloatingStart();
            }
            else
            {
                hideFloatingStart();
            }
        }

        private void Taskbar_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (Host != null && e.PropertyName == nameof(Opacity))
            {
                if (Host.Opacity == 1)
                {
                    openFloatingStart();
                }
                else
                {
                    hideFloatingStart();
                }
            }
        }

        #region Floating start button

        private void openFloatingStart()
        {
            bool useFloatingStartButton = Application.Current.FindResource("UseFloatingStartButton") as bool? ?? false;

            if (!useFloatingStartButton || Visibility != Visibility.Visible) return;

            if (floatingStartButton == null)
            {
                floatingStartButton = new FloatingStartButton(this, getButtonRect());
                floatingStartButton.Show();
            }
            else
            {
                showFloatingStart();
            }
        }

        private void showFloatingStart()
        {
            if (floatingStartButton == null) return;

            UpdateFloatingStartCoordinates();
            floatingStartButton.Visibility = Visibility.Visible;
        }

        private void hideFloatingStart()
        {
            if (floatingStartButton == null) return;

            floatingStartButton.Visibility = Visibility.Hidden;
        }

        private void closeFloatingStart()
        {
            floatingStartButton?.Close();
            floatingStartButton = null;
        }

        private NativeMethods.Rect getButtonRect()
        {
            // Get the pixel values of the start button's bounds
            Point buttonPosPixels = Start.PointToScreen(new Point(FlowDirection == FlowDirection.LeftToRight ? 0 : Start.ActualWidth, 0));
            Point buttonSizePixels = Start.PointToScreen(new Point(FlowDirection == FlowDirection.LeftToRight ? Start.ActualWidth : 0, Start.ActualHeight));

            // If the start button is currently translated, we get the translated position
            // and need to offset by that much to be positioned correctly.
            if (Host?.AutoHideElement?.RenderTransform is TranslateTransform tt)
            {
                buttonPosPixels.X -= (tt.X * Host.DpiScale);
                buttonPosPixels.Y -= (tt.Y * Host.DpiScale);
                buttonSizePixels.X -= (tt.X * Host.DpiScale);
                buttonSizePixels.Y -= (tt.Y * Host.DpiScale);
            }

            return new NativeMethods.Rect((int)buttonPosPixels.X, (int)buttonPosPixels.Y, (int)buttonSizePixels.X, (int)buttonSizePixels.Y);
        }

        public void UpdateFloatingStartCoordinates()
        {
            // Can't get our coordinates if we aren't visible.
            if (!IsVisible || floatingStartButton == null) return;

            floatingStartButton.SetPosition(getButtonRect());
        }

        public void UpdateFloatingStartTopmost(bool topmost)
        {
            if (floatingStartButton == null) return;

            floatingStartButton.Topmost = topmost;

            if (!topmost)
            {
                // Setting Topmost=false itself does not guarantee that we will go below a full-screen window.
                NativeMethods.SetWindowPos(
                floatingStartButton.Handle,
                Host.Handle,
                0, 0, 0, 0,
                (int)NativeMethods.SetWindowPosFlags.SWP_NOSIZE | (int)NativeMethods.SetWindowPosFlags.SWP_NOMOVE | (int)NativeMethods.SetWindowPosFlags.SWP_NOACTIVATE);
            }
        }

        #endregion
    }
}