using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace RetroBar.Controls
{
    /// <summary>
    /// ToolBar fork: a horizontal WrapPanel that, when <see cref="Spread"/> is on, distributes its
    /// items evenly over the whole area it is given (columns across the width, rows across the
    /// height) instead of packing them together. Falls back to normal WrapPanel layout when the
    /// rows needed would not fit in the available height.
    /// </summary>
    public class SpreadWrapPanel : WrapPanel
    {
        public static readonly DependencyProperty SpreadProperty = DependencyProperty.Register(
            nameof(Spread), typeof(bool), typeof(SpreadWrapPanel),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public bool Spread
        {
            get => (bool)GetValue(SpreadProperty);
            set => SetValue(SpreadProperty, value);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (!Spread || Orientation != Orientation.Horizontal)
            {
                return base.ArrangeOverride(finalSize);
            }

            var children = InternalChildren.Cast<UIElement>()
                .Where(c => c != null && c.Visibility != Visibility.Collapsed)
                .ToList();

            if (children.Count == 0)
            {
                return base.ArrangeOverride(finalSize);
            }

            double childW = children.Max(c => c.DesiredSize.Width);
            double childH = children.Max(c => c.DesiredSize.Height);

            if (childW <= 0 || childH <= 0)
            {
                return base.ArrangeOverride(finalSize);
            }

            int maxCols = Math.Max(1, (int)Math.Floor(finalSize.Width / childW));
            int maxRows = Math.Max(1, (int)Math.Floor(finalSize.Height / childH));
            int rows = (int)Math.Ceiling((double)children.Count / maxCols);

            if (rows > maxRows)
            {
                // Not enough room to spread out; keep the regular wrap layout.
                return base.ArrangeOverride(finalSize);
            }

            // Balance items across rows, e.g. 13 icons in 2 rows -> 7 + 6.
            int cols = (int)Math.Ceiling((double)children.Count / rows);
            double cellW = finalSize.Width / cols;
            double cellH = finalSize.Height / rows;

            for (int i = 0; i < children.Count; i++)
            {
                int row = i / cols;
                int col = i % cols;
                UIElement child = children[i];
                double x = col * cellW + (cellW - child.DesiredSize.Width) / 2;
                double y = row * cellH + (cellH - child.DesiredSize.Height) / 2;
                child.Arrange(new Rect(new Point(Math.Floor(x), Math.Floor(y)), child.DesiredSize));
            }

            return finalSize;
        }
    }
}
