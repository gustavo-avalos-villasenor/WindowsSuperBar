using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Animation;
using ManagedShell.Common.Helpers;
using ManagedShell.Interop;
using ManagedShell.WindowsTasks;
using RetroBar.Converters;
using RetroBar.Utilities;

namespace RetroBar.Controls
{
    /// <summary>
    /// Interaction logic for TaskButton.xaml
    /// </summary>
    public partial class TaskButton : UserControl
    {
        public static DependencyProperty HostProperty = DependencyProperty.Register(nameof(Host), typeof(TaskList), typeof(TaskButton));

        public TaskList Host
        {
            get { return (TaskList)GetValue(HostProperty); }
            set { SetValue(HostProperty, value); }
        }

        private ApplicationWindow Window;
        private TaskButtonStyleConverter StyleConverter = new TaskButtonStyleConverter();
        private ApplicationWindow.WindowState PressedWindowState = ApplicationWindow.WindowState.Inactive;

        private DelayedActivationHandler dragHandler;
        private bool _isLoaded;

        public TaskButton()
        {
            InitializeComponent();
            SetStyle();
            ApplyLabelLayout();
        }

        // ToolBar fork: wrap the window title onto several lines when multi-line buttons are on.
        // Local values override the theme's TaskLabel style (which trims to a single line).
        private void ApplyLabelLayout()
        {
            bool vertical = Settings.Instance.Edge == ManagedShell.AppBar.AppBarEdge.Left ||
                            Settings.Instance.Edge == ManagedShell.AppBar.AppBarEdge.Right;

            if (Settings.Instance.MultiLineTaskButtons && !vertical)
            {
                double lineHeight = Math.Ceiling(TaskLabelText.FontSize * 1.25);
                TaskLabelText.TextWrapping = TextWrapping.Wrap;
                TaskLabelText.TextTrimming = TextTrimming.CharacterEllipsis;
                TaskLabelText.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
                TaskLabelText.LineHeight = lineHeight;

                // Show as many whole lines as fit in the button (capped by the setting).
                int lines = Settings.Instance.TaskLabelLines;
                if (TaskLabelText.Parent is FrameworkElement container && container.ActualHeight > 0)
                {
                    int fit = (int)Math.Floor(container.ActualHeight / lineHeight);
                    lines = Math.Max(1, Math.Min(lines, fit));
                }
                TaskLabelText.MaxHeight = lineHeight * lines;
            }
            else
            {
                TaskLabelText.ClearValue(TextBlock.TextWrappingProperty);
                TaskLabelText.ClearValue(TextBlock.TextTrimmingProperty);
                TaskLabelText.ClearValue(TextBlock.LineStackingStrategyProperty);
                TaskLabelText.ClearValue(TextBlock.LineHeightProperty);
                TaskLabelText.ClearValue(MaxHeightProperty);
            }
        }

        private void SetStyle()
        {
            MultiBinding multiBinding = new MultiBinding();
            multiBinding.Converter = StyleConverter;

            multiBinding.Bindings.Add(new Binding { RelativeSource = RelativeSource.Self });
            multiBinding.Bindings.Add(new Binding("State"));

            AppButton.SetBinding(StyleProperty, multiBinding);
        }

        private void ScrollIntoView()
        {
            if (Window == null)
            {
                return;
            }

            if (Window.State == ApplicationWindow.WindowState.Active)
            {
                BringIntoView();
            }
        }

        private void Animate()
        {
            var ease = new SineEase();
            ease.EasingMode = EasingMode.EaseInOut;

            DoubleAnimation animation = new DoubleAnimation();
            animation.From = 0;
            animation.To = Host?.ButtonWidth ?? ActualWidth;
            animation.Duration = new Duration(TimeSpan.FromMilliseconds(250));
            animation.FillBehavior = FillBehavior.Stop;
            animation.EasingFunction = ease;
            Storyboard.SetTarget(animation, this);
            Storyboard.SetTargetProperty(animation, new PropertyPath(WidthProperty));

            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(animation);
            storyboard.Begin();
        }

        private void TaskButton_OnLoaded(object sender, RoutedEventArgs e)
        {
            Window = DataContext as ApplicationWindow;

            Settings.Instance.PropertyChanged += Settings_PropertyChanged;
            ApplyLabelLayout(); // font size from the theme is resolved by now

            dragHandler = new DelayedActivationHandler(() =>
            {
                Window?.BringToFront();
            });

            if (Window != null)
            {
                Window.GetButtonRect += Window_GetButtonRect;
                Window.PropertyChanged += Window_PropertyChanged;
                UpdateAttentionState();
            }

            if (Settings.Instance.SlideTaskbarButtons && Host?.Host?.Orientation == Orientation.Horizontal)
            {
                Animate();
            }

            _isLoaded = true;
        }

        private void Window_GetButtonRect(ref NativeMethods.ShortRect rect)
        {
            if (Host?.Host?.Screen.Primary != true && Settings.Instance.MultiMonMode != MultiMonOption.SameAsWindow)
            {
                // If there are multiple instances of a button, use the button on the primary display only
                return;
            }

            Point buttonTopLeft = PointToScreen(new Point(0, 0));
            Point buttonBottomRight = PointToScreen(new Point(ActualWidth, ActualHeight));
            rect.Top = (short)buttonTopLeft.Y;
            rect.Left = (short)buttonTopLeft.X;
            rect.Bottom = (short)buttonBottomRight.Y;
            rect.Right = (short)buttonBottomRight.X;
        }

        private void Window_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "State")
            {
                ScrollIntoView();

                if (Dispatcher.CheckAccess())
                {
                    UpdateAttentionState();
                }
                else
                {
                    Dispatcher.BeginInvoke(new Action(UpdateAttentionState));
                }
            }
        }

        // WorkWindowsBar: like the native Windows taskbar, a button that asked for attention
        // (FlashWindowEx) stays highlighted until the window is activated. ManagedShell alone
        // toggles Flashing/Inactive with every blink and ends un-highlighted once the app stops
        // flashing, so the notification was easy to miss.
        private bool _needsAttention;

        private void UpdateAttentionState()
        {
            if (Window == null)
            {
                return;
            }

            switch (Window.State)
            {
                case ApplicationWindow.WindowState.Flashing:
                    _needsAttention = true;
                    break;
                case ApplicationWindow.WindowState.Active:
                    _needsAttention = false;
                    break;
            }

            if (_needsAttention && Window.State != ApplicationWindow.WindowState.Active)
            {
                // Pin the flashing style (replaces the state-driven binding until cleared).
                AppButton.Style = FindResource("TaskButtonFlashing") as Style;
            }
            else if (BindingOperations.GetMultiBindingExpression(AppButton, StyleProperty) == null)
            {
                SetStyle();
            }
        }

        private void TaskButton_OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded)
            {
                return;
            }

            Settings.Instance.PropertyChanged -= Settings_PropertyChanged;
            dragHandler?.Dispose();

            if (Window != null)
            {
                Window.GetButtonRect -= Window_GetButtonRect;
                Window.PropertyChanged -= Window_PropertyChanged;
            }

            _isLoaded = false;
        }

        private void AppButton_OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (Window == null)
            {
                return;
            }

            NativeMethods.WindowShowStyle wss = Window.ShowStyle;
            int ws = Window.WindowStyles;

            // disable window operations depending on current window state. originally tried implementing via bindings but found there is no notification we get regarding maximized state
            MaximizeMenuItem.IsEnabled = wss != NativeMethods.WindowShowStyle.ShowMaximized && (ws & (int)NativeMethods.WindowStyles.WS_MAXIMIZEBOX) != 0;
            MinimizeMenuItem.IsEnabled = wss != NativeMethods.WindowShowStyle.ShowMinimized && Window.CanMinimize;
            if (RestoreMenuItem.IsEnabled = wss != NativeMethods.WindowShowStyle.ShowNormal)
            {
                CloseMenuItem.FontWeight = FontWeights.Normal;
                RestoreMenuItem.FontWeight = FontWeights.Bold;
            }
            if (!RestoreMenuItem.IsEnabled || RestoreMenuItem.IsEnabled && !MaximizeMenuItem.IsEnabled)
            {
                CloseMenuItem.FontWeight = FontWeights.Bold;
                RestoreMenuItem.FontWeight = FontWeights.Normal;
            }
            MoveMenuItem.IsEnabled = wss == NativeMethods.WindowShowStyle.ShowNormal;
            SizeMenuItem.IsEnabled = wss == NativeMethods.WindowShowStyle.ShowNormal && (ws & (int)NativeMethods.WindowStyles.WS_MAXIMIZEBOX) != 0;
        }

        private void CloseMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            Window?.Close();
        }

        private void EndTaskMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (Window != null)
            {
                ForceEndTask();
            }
        }

        private void ForceEndTask()
        {
            try
            {
                if (Window.ProcId.HasValue && Window.ProcId.Value != 0)
                {
                    // Don't kill RetroBar itself - just close the window gracefully
                    int currentProcId = Process.GetCurrentProcess().Id;
                    if (Window.ProcId.Value == currentProcId)
                    {
                        Window?.Close();
                        return;
                    }

                    Process process = Process.GetProcessById((int)Window.ProcId.Value);
                    process.Kill();
                }
            }
            catch (Exception)
            {
                Window?.Close();
            }
        }

        private void RestoreMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            Window?.Restore();
        }

        private void MoveMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            Window?.Move();
        }

        private void SizeMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            Window?.Size();
        }

        private void MinimizeMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            Window?.Minimize();
        }

        private void MaximizeMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            Window?.Maximize();
        }

        private void AppButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (PressedWindowState == ApplicationWindow.WindowState.Active && Window?.CanMinimize == true)
            {
                Window?.Minimize();
            }
            else
            {
                Window?.BringToFront();
            }
        }

        private void AppButton_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                PressedWindowState = Window.State;
            }
        }

        private void AppButton_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                if (Window == null || Settings.Instance.TaskMiddleClickAction == TaskMiddleClickOption.DoNothing)
                {
                    return;
                }
                if (Settings.Instance.TaskMiddleClickAction == TaskMiddleClickOption.CloseTask !=
                    (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
                {
                    Window?.Close();
                }
                else
                {
                    ShellHelper.StartProcess(Window.IsUWP ? "appx:" + Window.AppUserModelID : Window.WinFileName);
                }
            }
        }

        private void LabelContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.HeightChanged)
            {
                ApplyLabelLayout();
            }
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Settings.Theme))
            {
                SetStyle();
                ApplyLabelLayout();
                UpdateAttentionState();
            }
            else if (e.PropertyName is nameof(Settings.MultiLineTaskButtons)
                     or nameof(Settings.TaskLabelLines)
                     or nameof(Settings.Edge))
            {
                ApplyLabelLayout();
            }
        }

        private void AppButton_OnDragEnter(object sender, DragEventArgs e)
        {
            dragHandler?.OnDragEnter(e);
        }

        private void AppButton_OnDragLeave(object sender, DragEventArgs e)
        {
            dragHandler?.OnDragLeave();
        }

        private void ContextMenu_OpenedOrClosed(object sender, RoutedEventArgs e)
        {
            if (sender is ContextMenu menu && menu.IsOpen)
            {
                // Show as active while the menu is open (restores the state-driven binding).
                if (BindingOperations.GetMultiBindingExpression(AppButton, StyleProperty) == null)
                {
                    SetStyle();
                }
                BindingOperations.GetMultiBindingExpression(AppButton, StyleProperty)?.UpdateTarget();
            }
            else
            {
                BindingOperations.GetMultiBindingExpression(AppButton, StyleProperty)?.UpdateTarget();
                UpdateAttentionState();
            }
        }
    }
}