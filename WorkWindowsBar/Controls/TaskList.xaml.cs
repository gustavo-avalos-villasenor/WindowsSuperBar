using ManagedShell.AppBar;
using ManagedShell.WindowsTasks;
using ManagedShell.Common.Helpers;
using RetroBar.Utilities;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace RetroBar.Controls
{
    /// <summary>
    /// Interaction logic for TaskList.xaml
    /// </summary>
    public partial class TaskList : UserControl
    {
        private bool isLoaded;
        private bool isScrollable;
        private double DefaultButtonWidth;
        private double MinButtonWidth;
        private double TaskButtonLeftMargin;
        private double TaskButtonRightMargin;
        private ICollectionView taskbarItems;

        public static DependencyProperty ButtonWidthProperty = DependencyProperty.Register(nameof(ButtonWidth), typeof(double), typeof(TaskList), new PropertyMetadata(new double()));

        public double ButtonWidth
        {
            get { return (double)GetValue(ButtonWidthProperty); }
            set { SetValue(ButtonWidthProperty, value); }
        }

        public static DependencyProperty ButtonsPerRowProperty = DependencyProperty.Register(nameof(ButtonsPerRow), typeof(int), typeof(TaskList), new PropertyMetadata(0));

        public int ButtonsPerRow
        {
            get { return (int)GetValue(ButtonsPerRowProperty); }
            set { SetValue(ButtonsPerRowProperty, value); }
        }

        // Number of columns in a full row that get one extra pixel, so the row fills the
        // taskbar exactly instead of leaving the floored remainder empty.
        public static DependencyProperty ExtraWidthCountProperty = DependencyProperty.Register(nameof(ExtraWidthCount), typeof(int), typeof(TaskList), new PropertyMetadata(0));

        public int ExtraWidthCount
        {
            get { return (int)GetValue(ExtraWidthCountProperty); }
            set { SetValue(ExtraWidthCountProperty, value); }
        }

        // Explicit button height; NaN = automatic (theme default). Used by multi-line mode so
        // buttons fill the full taskbar height instead of one row.
        public static DependencyProperty ButtonHeightProperty = DependencyProperty.Register(nameof(ButtonHeight), typeof(double), typeof(TaskList), new PropertyMetadata(double.NaN));

        public double ButtonHeight
        {
            get { return (double)GetValue(ButtonHeightProperty); }
            set { SetValue(ButtonHeightProperty, value); }
        }

        private bool IsMultiLineMode =>
            Settings.Instance.MultiLineTaskButtons &&
            Settings.Instance.Edge != AppBarEdge.Left &&
            Settings.Instance.Edge != AppBarEdge.Right;

        public static DependencyProperty TasksProperty =DependencyProperty.Register(nameof(Tasks), typeof(Tasks), typeof(TaskList), new PropertyMetadata(TasksChangedCallback));

        public Tasks Tasks
        {
            get { return (Tasks)GetValue(TasksProperty); }
            set { SetValue(TasksProperty, value); }
        }

        public static DependencyProperty HostProperty = DependencyProperty.Register(nameof(Host), typeof(Taskbar), typeof(TaskList), new PropertyMetadata(TasksChangedCallback));

        public Taskbar Host
        {
            get { return (Taskbar)GetValue(HostProperty); }
            set { SetValue(HostProperty, value); }
        }

        public TaskList()
        {
            InitializeComponent();
        }

        // WorkWindowsBar: the windows currently shown as buttons on this taskbar (same filter as the UI).
        public System.Collections.Generic.List<ApplicationWindow> GetVisibleWindows()
        {
            var result = new System.Collections.Generic.List<ApplicationWindow>();
            if (taskbarItems != null)
            {
                foreach (object item in taskbarItems)
                {
                    if (item is ApplicationWindow window)
                    {
                        result.Add(window);
                    }
                }
            }
            return result;
        }

        private void SetStyles()
        {
            DefaultButtonWidth = Application.Current.FindResource("TaskButtonWidth") as double? ?? 0;
            if (Settings.Instance.TaskButtonWidthOverride > 0)
            {
                DefaultButtonWidth = Settings.Instance.TaskButtonWidthOverride;
            }
            MinButtonWidth = Application.Current.FindResource("TaskButtonMinWidth") as double? ?? 0;
            Thickness buttonMargin;

            if (Settings.Instance.Edge == AppBarEdge.Left || Settings.Instance.Edge == AppBarEdge.Right)
            {
                buttonMargin = Application.Current.FindResource("TaskButtonVerticalMargin") as Thickness? ?? new Thickness();
            }
            else
            {
                buttonMargin = Application.Current.FindResource("TaskButtonMargin") as Thickness? ?? new Thickness();
            }

            TaskButtonLeftMargin = buttonMargin.Left;
            TaskButtonRightMargin = buttonMargin.Right;
        }

        private void TaskList_OnLoaded(object sender, RoutedEventArgs e)
        {
            SetStyles();
            SetTasksCollection();
            UpdateResizeGrip();
        }

        // WorkWindowsBar: only advertise the Quick Launch resize grip while the taskbar is unlocked.
        private void UpdateResizeGrip()
        {
            bool resizable = !Settings.Instance.LockTaskbar &&
                             Settings.Instance.Edge != AppBarEdge.Left &&
                             Settings.Instance.Edge != AppBarEdge.Right;

            if (resizable)
            {
                QuickLaunchResizeThumb.Cursor = System.Windows.Input.Cursors.SizeWE;
                QuickLaunchResizeThumb.ToolTip = "Arrastra para cambiar el ancho de Inicio rápido";
            }
            else
            {
                QuickLaunchResizeThumb.ClearValue(CursorProperty);
                QuickLaunchResizeThumb.ClearValue(ToolTipProperty);
            }
        }

        private void SetTasksCollection()
        {
            if (!isLoaded && Tasks != null && Host != null)
            {
                taskbarItems = Tasks.CreateGroupedWindowsCollection();
                if (taskbarItems != null)
                {
                    taskbarItems.CollectionChanged += GroupedWindows_CollectionChanged;
                    taskbarItems.Filter = Tasks_Filter;
                }

                TasksList.ItemsSource = taskbarItems;

                Settings.Instance.PropertyChanged += Settings_PropertyChanged;
                Host.hotkeyManager.TaskbarHotkeyPressed += TaskList_TaskbarHotkeyPressed;

                isLoaded = true;
            }
        }

        private static void TasksChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is TaskList taskList && e.OldValue == null && e.NewValue != null)
            {
                taskList.SetTasksCollection();
            }
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(Settings.LockTaskbar) or nameof(Settings.Edge))
            {
                UpdateResizeGrip();
            }

            if (e.PropertyName == nameof(Settings.MultiMonMode))
            {
                taskbarItems?.Refresh();
            }
            else if (e.PropertyName == nameof(Settings.ShowMultiMon))
            {
                if (Settings.Instance.MultiMonMode != MultiMonOption.AllTaskbars)
                {
                    taskbarItems?.Refresh();
                }
            }
            else if (e.PropertyName is nameof(Settings.MultiLineTaskButtons)
                     or nameof(Settings.TaskButtonWidthOverride)
                     or nameof(Settings.Theme)
                     or nameof(Settings.Edge)
                     or nameof(Settings.RowCount)
                     or nameof(Settings.MaxTaskRows)
                     or nameof(Settings.TaskRowHeight))
            {
                SetStyles();
                SetTaskButtonWidth();
            }
        }
        private void TaskList_TaskbarHotkeyPressed(object sender, HotkeyManager.TaskbarHotkeyEventArgs e)
        {
            if (Settings.Instance.WinNumHotkeysAction == WinNumHotkeysOption.SwitchTasks && Host.Screen.Primary)
            {
                try
                {
                    bool exists = taskbarItems.MoveCurrentToPosition(e.index);

                    if (exists)
                    {
                        ApplicationWindow window = taskbarItems.CurrentItem as ApplicationWindow;

                        if (e.isShiftPressed)
                        {
                            // Open new instance when Shift is pressed
                            ShellHelper.StartProcess(window.IsUWP ? "appx:" + window.AppUserModelID : window.WinFileName);
                        }
                        else
                        {
                            // Normal behavior - switch to existing window
                            if (window.State == ApplicationWindow.WindowState.Active && window.CanMinimize)
                            {
                                window.Minimize();
                            }
                            else
                            {
                                window.BringToFront();
                            }
                        }
                    }

                }
                catch (ArgumentOutOfRangeException) { }
            }
        }

        private bool Tasks_Filter(object obj)
        {
            if (obj is ApplicationWindow window)
            {
                if (!window.ShowInTaskbar)
                {
                    return false;
                }

                if (!Settings.Instance.ShowMultiMon || Settings.Instance.MultiMonMode == MultiMonOption.AllTaskbars)
                {
                    return true;
                }

                if (Settings.Instance.MultiMonMode == MultiMonOption.SameAsWindowAndPrimary && Host.Screen.Primary)
                {
                    return true;
                }

                IntPtr hMonitor = window.HMonitor;
                if (Host.Screen.Primary && !Host.windowManager.IsValidHMonitor(hMonitor))
                {
                    return true;
                }

                if (hMonitor != Host.Screen.HMonitor)
                {
                    return false;
                }
            }

            return true;
        }

        private void TaskList_OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (taskbarItems != null)
            {
                taskbarItems.CollectionChanged -= GroupedWindows_CollectionChanged;
                taskbarItems.Filter = null;
            }

            if (Host != null)
            {
                Host.hotkeyManager.TaskbarHotkeyPressed -= TaskList_TaskbarHotkeyPressed;
            }

            Settings.Instance.PropertyChanged -= Settings_PropertyChanged;

            isLoaded = false;
        }

        private void GroupedWindows_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            SetTaskButtonWidth();
        }

        private void TaskList_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetTaskButtonWidth();
        }

        private void SetTaskButtonWidth()
        {
            if (Host is null)
                return; // The state is trashed, but presumably it's just a transition

            if (Settings.Instance.Edge == AppBarEdge.Left || Settings.Instance.Edge == AppBarEdge.Right)
            {
                ExtraWidthCount = 0;
                ButtonHeight = double.NaN;
                ButtonWidth = ActualWidth;
                SetScrollable(true); // while technically not always scrollable, we don't run into DPI-specific issues with it enabled while vertical
                return;
            }

            int rows = Host.Rows;
            int taskCount = TasksList.Items.Count;
            TasksList.AlternationCount = taskCount; // keep in sync for correct AlternationIndex

            double margin = TaskButtonLeftMargin + TaskButtonRightMargin;

            if (IsMultiLineMode)
            {
                // Fit as many rows as the taskbar height allows at the target row height
                // (capped by MaxTaskRows). Buttons fill the first row left-to-right, then wrap.
                double available = TasksScrollViewer.ActualHeight;
                double targetRowHeight = Math.Max(16, Settings.Instance.TaskRowHeight);
                rows = available > 0
                    ? Math.Clamp((int)Math.Floor(available / targetRowHeight), 1, Math.Max(1, Settings.Instance.MaxTaskRows))
                    : 1;

                ButtonHeight = available > 0 ? Math.Floor(available / rows) : double.NaN;
            }
            else
            {
                ButtonHeight = double.NaN;
            }

            ButtonsPerRow = Math.Max(1, (int)Math.Ceiling((double)taskCount / rows));
            double maxWidth = TasksList.ActualWidth / ButtonsPerRow;
            double defaultWidth = DefaultButtonWidth + margin;
            double minWidth = MinButtonWidth + margin;

            if (maxWidth > defaultWidth)
            {
                // Room to spare: keep the default width and leave the trailing gap.
                ExtraWidthCount = 0;
                ButtonWidth = defaultWidth;
                SetScrollable(false);
            }
            else if (maxWidth < minWidth)
            {
                ExtraWidthCount = 0;
                ButtonWidth = Math.Ceiling(defaultWidth / 2);
                SetScrollable(true);
            }
            else
            {
                // Buttons are shrunk to fit a full row: spread the pixels lost to flooring
                // across the columns so the row fills the taskbar.
                double baseWidth = Math.Floor(maxWidth);
                ButtonWidth = baseWidth;
                ExtraWidthCount = (int)Math.Floor(TasksList.ActualWidth) - (int)baseWidth * ButtonsPerRow;
                SetScrollable(false);
            }
        }

        // ToolBar fork: dragging the grip at the left of the task list resizes Quick Launch.
        private void Thumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            if (Settings.Instance.LockTaskbar || Host?.QuickLaunchToolbar == null ||
                Host.QuickLaunchToolbar.Visibility != Visibility.Visible ||
                Settings.Instance.Edge == AppBarEdge.Left || Settings.Instance.Edge == AppBarEdge.Right)
            {
                return;
            }

            double current = Settings.Instance.QuickLaunchWidth > 0
                ? Settings.Instance.QuickLaunchWidth
                : Host.QuickLaunchToolbar.ActualWidth;
            double width = Math.Round(Math.Clamp(current + e.HorizontalChange, 24, 1200));
            Settings.Instance.QuickLaunchWidth = width;
        }

        private void SetScrollable(bool canScroll)
        {
            if (canScroll == isScrollable) return;

            if (canScroll)
            {
                TasksScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            }
            else
            {
                TasksScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            }

            isScrollable = canScroll;
        }

        private void TasksScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (!isScrollable)
            {
                e.Handled = true;
            }
        }
    }
}