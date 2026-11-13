using ManagedShell.AppBar;
using RetroBar.Utilities;
using System.Windows.Controls;
using System;
using System.Windows.Data;
using System.Globalization;

namespace RetroBar.Converters
{
    public class IconListOrientation : IMultiValueConverter
    {
        // ConverterParameter: "quicklaunch" or "tray". When that area has a fixed width set
        // (ToolBar fork), icons fill rows left-to-right and wrap at that width, so the width
        // the user chooses decides how many icons go per row.
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool horizontal = Settings.Instance.Edge is AppBarEdge.Top or AppBarEdge.Bottom;
            int rows = Settings.Instance.RowCount;

            if (horizontal && rows > 1)
            {
                double fixedWidth = (parameter as string) switch
                {
                    "quicklaunch" => Settings.Instance.QuickLaunchWidth,
                    "tray" => Settings.Instance.TrayIconsWidth,
                    _ => 0
                };

                return fixedWidth > 0 ? Orientation.Horizontal : Orientation.Vertical;
            }

            return Orientation.Horizontal;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
