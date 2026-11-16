using ManagedShell.AppBar;
using ManagedShell.Common.Helpers;
using ManagedShell.WindowsTray;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RetroBar.Utilities
{
    internal class Settings : INotifyPropertyChanged, IMigratableSettings
    {
        private static Settings instance;

        public static Settings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = _settingsManager.Settings;
                    _isInitializing = false;
                }

                return instance;
            }
        }

        private static string _settingsPath = "settings.json".InLocalAppData();
        private static bool _isInitializing = true;
        private static SettingsManager<Settings> _settingsManager = new(_settingsPath, new Settings());

        private bool _migrationPerformed = false;
        public bool MigrationPerformed { get => _migrationPerformed; }
        public event PropertyChangedEventHandler PropertyChanged;

        // This should not be used directly! Unfortunately it must be public for JsonSerializer.
        public Settings()
        {
            PropertyChanged += Settings_PropertyChanged;
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_isInitializing)
            {
                return;
            }

            _settingsManager.Settings = this;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void Set<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (!field.Equals(value))
            {
                field = value;
                OnPropertyChanged(propertyName);
            }
        }

        protected void SetEnum<T>(ref T field, T value, [CallerMemberName] string propertyName = "") where T : Enum
        {
            if (!field.Equals(value))
            {
                if (!Enum.IsDefined(typeof(T), value))
                {
                    return;
                }

                field = value;
                OnPropertyChanged(propertyName);
            }
        }

        #region Properties
        private string _language = "System";
        public string Language
        {
            get => _language;
            set => Set(ref _language, value);
        }

        private string _theme = "Moderno Oscuro";
        public string Theme
        {
            get => _theme;
            set => Set(ref _theme, value);
        }

        private bool _showInputLanguage = false;
        public bool ShowInputLanguage
        {
            get => _showInputLanguage;
            set => Set(ref _showInputLanguage, value);
        }

        private bool _showClock = true;
        public bool ShowClock
        {
            get => _showClock;
            set => Set(ref _showClock, value);
        }

        private bool _showDesktopButton = false;
        public bool ShowDesktopButton
        {
            get => _showDesktopButton;
            set => Set(ref _showDesktopButton, value);
        }

        private bool _peekAtDesktop = false;
        public bool PeekAtDesktop
        {
            get => _peekAtDesktop;
            set => Set(ref _peekAtDesktop, value);
        }

        private bool _showMultiMon = false;
        public bool ShowMultiMon
        {
            get => _showMultiMon;
            set => Set(ref _showMultiMon, value);
        }

        private bool _showQuickLaunch = true;
        public bool ShowQuickLaunch
        {
            get => _showQuickLaunch;
            set => Set(ref _showQuickLaunch, value);
        }

        private string _quickLaunchPath = "%appdata%\\Microsoft\\Internet Explorer\\Quick Launch"; // fork: classic folder (User Pinned\TaskBar is empty/missing on Win11)
        public string QuickLaunchPath
        {
            get => _quickLaunchPath;
            set => Set(ref _quickLaunchPath, value);
        }

        private bool _collapseNotifyIcons = false;
        public bool CollapseNotifyIcons
        {
            get => _collapseNotifyIcons;
            set => Set(ref _collapseNotifyIcons, value);
        }

        private List<string> _invertNotifyIcons = new List<string> { NotificationArea.HARDWARE_GUID, NotificationArea.UPDATE_GUID, NotificationArea.MICROPHONE_GUID, NotificationArea.LOCATION_GUID, NotificationArea.MEETNOW_GUID, NotificationArea.NETWORK_GUID, NotificationArea.POWER_GUID, NotificationArea.VOLUME_GUID };
        public List<string> InvertNotifyIcons
        {
            get => _invertNotifyIcons;
            set => Set(ref _invertNotifyIcons, value);
        }

        private List<NotifyIconBehaviorSetting> _notifyIconBehaviors = new List<NotifyIconBehaviorSetting>
        {
            new NotifyIconBehaviorSetting
            {
                Identifier = NotificationArea.HEALTH_GUID,
                Behavior = NotifyIconBehavior.AlwaysShow
            },
            new NotifyIconBehaviorSetting
            {
                Identifier = NotificationArea.POWER_GUID,
                Behavior = NotifyIconBehavior.AlwaysShow
            },
            new NotifyIconBehaviorSetting
            {
                Identifier = NotificationArea.NETWORK_GUID,
                Behavior = NotifyIconBehavior.AlwaysShow
            },
            new NotifyIconBehaviorSetting
            {
                Identifier = NotificationArea.VOLUME_GUID,
                Behavior = NotifyIconBehavior.AlwaysShow
            },
        };
        public List<NotifyIconBehaviorSetting> NotifyIconBehaviors
        {
            get => _notifyIconBehaviors;
            set => Set(ref _notifyIconBehaviors, value);
        }

        private List<string> _notifyIconOrder = new List<string>();
        public List<string> NotifyIconOrder
        {
            get => _notifyIconOrder;
            set => Set(ref _notifyIconOrder, value);
        }

        private bool _allowFontSmoothing = true; // WorkWindowsBar: smooth (ClearType) text by default
        public bool AllowFontSmoothing
        {
            get => _allowFontSmoothing;
            set => Set(ref _allowFontSmoothing, value);
        }

        private bool _allowFontSmoothingMenu = true;
        public bool AllowFontSmoothingMenu
        {
            get => _allowFontSmoothingMenu;
            set => Set(ref _allowFontSmoothingMenu, value);
        }

        private bool _useSoftwareRendering = false;
        public bool UseSoftwareRendering
        {
            get => _useSoftwareRendering;
            set => Set(ref _useSoftwareRendering, value);
        }

        private AppBarEdge _edge = AppBarEdge.Bottom;
        public AppBarEdge Edge
        {
            get => _edge;
            set => SetEnum(ref _edge, value);
        }

        private int _rowCount = 2;
        public int RowCount
        {
            get => _rowCount;
            set => Set(ref _rowCount, value);
        }

        private int _rowLimit = 5;
        public int RowLimit
        {
            get => _rowLimit;
            set => Set(ref _rowLimit, value);
        }

        private int _taskbarWidth = 1;
        public int TaskbarWidth
        {
            get => _taskbarWidth;
            set => Set(ref _taskbarWidth, value);
        }

        private int _taskbarWidthLimit = 7;
        public int TaskbarWidthLimit
        {
            get => _taskbarWidthLimit;
            set => Set(ref _taskbarWidthLimit, value);
        }

        private List<string> _quickLaunchOrder = [];
        public List<string> QuickLaunchOrder
        {
            get => _quickLaunchOrder;
            set => Set(ref _quickLaunchOrder, value);
        }

        private bool _showTaskThumbnails = false;
        public bool ShowTaskThumbnails
        {
            get => _showTaskThumbnails;
            set => Set(ref _showTaskThumbnails, value);
        }

        private MultiMonOption _multiMonMode = MultiMonOption.AllTaskbars;
        public MultiMonOption MultiMonMode
        {
            get => _multiMonMode;
            set => SetEnum(ref _multiMonMode, value);
        }

        private double _taskbarScale = 1.0;
        public double TaskbarScale
        {
            get => _taskbarScale;
            set => Set(ref _taskbarScale, value);
        }

        private bool _debugLogging = false;
        public bool DebugLogging
        {
            get => _debugLogging;
            set => Set(ref _debugLogging, value);
        }

        private bool _autoHide = false;
        public bool AutoHide
        {
            get => _autoHide;
            set => Set(ref _autoHide, value);
        }

        private bool _lockTaskbar = false;
        public bool LockTaskbar
        {
            get => _lockTaskbar;
            set => Set(ref _lockTaskbar, value);
        }

        private InvertIconsOption _invertIconsMode = EnvironmentHelper.IsWindows10OrBetter ? InvertIconsOption.WhenNeededByTheme : InvertIconsOption.Never;
        public InvertIconsOption InvertIconsMode
        {
            get => _invertIconsMode;
            set => SetEnum(ref _invertIconsMode, value);
        }

        private bool _showTaskBadges = true;
        public bool ShowTaskBadges
        {
            get => _showTaskBadges;
            set => Set(ref _showTaskBadges, value);
        }

        private TaskMiddleClickOption _taskMiddleClickAction = TaskMiddleClickOption.OpenNewInstance;
        public TaskMiddleClickOption TaskMiddleClickAction
        {
            get => _taskMiddleClickAction;
            set => SetEnum(ref _taskMiddleClickAction, value);
        }

        private ClockClickOption _clockClickAction = EnvironmentHelper.IsWindows10OrBetter ? ClockClickOption.OpenNotificationCenter : ClockClickOption.DoNothing;
        public ClockClickOption ClockClickAction
        {
            get
            {
                // On Windows versions prior to 10, neither the Modern calendar nor the Notification Center is available
                if (!EnvironmentHelper.IsWindows10OrBetter && _clockClickAction > ClockClickOption.OpenAeroCalendar)
                {
                    return ClockClickOption.DoNothing;
                }

                return _clockClickAction;
            }
            set => SetEnum(ref _clockClickAction, value);
        }

        private bool _checkForUpdates = false; // fork: don't offer upstream RetroBar updates
        public bool CheckForUpdates
        {
            get => _checkForUpdates;
            set => Set(ref _checkForUpdates, value);
        }

        private bool _showExitMenuItem = true;
        public bool ShowExitMenuItem
        {
            get => _showExitMenuItem;
            set => Set(ref _showExitMenuItem, value);
        }

        private bool _showEndTaskButton = false;
        public bool ShowEndTaskButton
        {
            get => _showEndTaskButton;
            set => Set(ref _showEndTaskButton, value);
        }

        private bool _showStartButtonMultiMon = false;
        public bool ShowStartButtonMultiMon
        {
            get => _showStartButtonMultiMon;
            set => Set(ref _showStartButtonMultiMon, value);
        }

        private bool _autoHideTransparent = false;
        public bool AutoHideTransparent
        {
            get => _autoHideTransparent;
            set => Set(ref _autoHideTransparent, value);
        }

        private bool _slideTaskbarButtons = false;
        public bool SlideTaskbarButtons
        {
            get => _slideTaskbarButtons;
            set => Set(ref _slideTaskbarButtons, value);
        }

        private bool _showClockSeconds = false;
        public bool ShowClockSeconds
        {
            get => _showClockSeconds;
            set => Set(ref _showClockSeconds, value);
        }

        private WinNumHotkeysOption _winNumHotkeysAction = WinNumHotkeysOption.WindowsDefault;
        public WinNumHotkeysOption WinNumHotkeysAction
        {
            get => _winNumHotkeysAction;
            set => SetEnum(ref _winNumHotkeysAction, value);
        }

        private bool _allowBlurBehind = true;
        public bool AllowBlurBehind
        {
            get => _allowBlurBehind;
            set => Set(ref _allowBlurBehind, value);
        }

        // --- ToolBar fork: tall task buttons with multi-line titles ---

        // When true (and the taskbar is horizontal), task buttons use the full taskbar height
        // as a single row, and the window title wraps onto up to TaskLabelLines lines.
        private bool _multiLineTaskButtons = true;
        public bool MultiLineTaskButtons
        {
            get => _multiLineTaskButtons;
            set => Set(ref _multiLineTaskButtons, value);
        }

        private int _taskLabelLines = 1;
        public int TaskLabelLines
        {
            get => _taskLabelLines;
            set => Set(ref _taskLabelLines, Math.Clamp(value, 1, 6));
        }

        // Target height of one row of task buttons (pixels, before scaling). The taskbar fits
        // floor(height / TaskRowHeight) rows, capped by MaxTaskRows.
        private double _taskRowHeight = 32;
        public double TaskRowHeight
        {
            get => _taskRowHeight;
            set => Set(ref _taskRowHeight, Math.Max(16, value));
        }

        // Maximum rows of task buttons in multi-line mode.
        private int _maxTaskRows = 3;
        public int MaxTaskRows
        {
            get => _maxTaskRows;
            set => Set(ref _maxTaskRows, Math.Clamp(value, 1, 6));
        }

        // Quick Launch width in pixels (before scaling). 0 = automatic (fit icons).
        // Changed by dragging the grip between Quick Launch and the task buttons (taskbar unlocked).
        private double _quickLaunchWidth = 0;
        public double QuickLaunchWidth
        {
            get => _quickLaunchWidth;
            set => Set(ref _quickLaunchWidth, Math.Max(0, value));
        }

        // Notification-area (tray) icon area width in pixels (before scaling). 0 = automatic.
        // Changed by dragging the grip at the left of the tray (taskbar unlocked).
        private double _trayIconsWidth = 0;
        public double TrayIconsWidth
        {
            get => _trayIconsWidth;
            set => Set(ref _trayIconsWidth, Math.Max(0, value));
        }

        // Start button icon. "" = the theme's icon; a name = a PNG from Resources\StartIcons or
        // %LOCALAPPDATA%\WorkWindowsBar\StartIcons (e.g. "Cerdito"); or a full image path.
        private string _customStartIcon = "";
        public string CustomStartIcon
        {
            get => _customStartIcon;
            set => Set(ref _customStartIcon, value ?? "");
        }

        // Display size (px, before scaling) of CustomStartIcon. 0 = keep the theme's icon size.
        private double _customStartIconSize = 52;
        public double CustomStartIconSize
        {
            get => _customStartIconSize;
            set => Set(ref _customStartIconSize, Math.Clamp(value, 0, 200));
        }

        // Preferred task button width in pixels (before scaling). 0 = use the theme's width.
        private double _taskButtonWidthOverride = 220;
        public double TaskButtonWidthOverride
        {
            get => _taskButtonWidthOverride;
            set => Set(ref _taskButtonWidthOverride, Math.Max(0, value));
        }
        #endregion

        #region Old Properties
        public bool? MiddleMouseToClose
        {
            get => null;
            set
            {
                // Migrate to TaskMiddleClickAction
                if (value != null)
                {
                    TaskMiddleClickAction = (bool)value ? TaskMiddleClickOption.CloseTask : TaskMiddleClickOption.OpenNewInstance;
                    _migrationPerformed = true;
                }
            }
        }

        public string[] PinnedNotifyIcons
        {
            get => [];
            set
            {
                // Migrate to NotifyIconBehaviors
                if (value.Length > 0)
                {
                    var newSettings = new List<NotifyIconBehaviorSetting>();

                    foreach (var identifier in value)
                    {
                        newSettings.Add(new NotifyIconBehaviorSetting
                        {
                            Identifier = identifier,
                            Behavior = NotifyIconBehavior.AlwaysShow
                        });
                    }

                    NotifyIconBehaviors = newSettings;
                    _migrationPerformed = true;
                }
            }
        }
        #endregion
    }

    #region Enums
    public enum InvertIconsOption
    {
        WhenNeededByTheme,
        Always,
        Never
    }

    public enum MultiMonOption
    {
        AllTaskbars,
        SameAsWindow,
        SameAsWindowAndPrimary
    }

    public enum TaskMiddleClickOption
    {
        DoNothing,
        OpenNewInstance,
        CloseTask
    }

    public enum ClockClickOption
    {
        DoNothing,
        OpenAeroCalendar,
        OpenModernCalendar,
        OpenNotificationCenter,
    }

    public enum WinNumHotkeysOption
    {
        WindowsDefault,
        SwitchTasks,
        InvokeQuickLaunch,
    }

    public enum NotifyIconBehavior
    {
        HideWhenInactive,
        AlwaysHide,
        AlwaysShow,
        Remove
    }
    #endregion

    #region Structs
    public struct NotifyIconBehaviorSetting
    {
        public string Identifier {  get; set; }
        public NotifyIconBehavior Behavior { get; set; }
    }
    #endregion
}