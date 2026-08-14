using Windows.UI;

namespace RotaryMonitor
{
    /// <summary>Windows.UI.Colors is not projected in this SDK config, so the
    /// few colors we need are built from Color.FromArgb.</summary>
    public static class UiColors
    {
        public static Color Black => Color.FromArgb(255, 0, 0, 0);
        public static Color Gray => Color.FromArgb(255, 128, 128, 128);
        public static Color DimGray => Color.FromArgb(255, 105, 105, 105);
        public static Color LightGray => Color.FromArgb(255, 211, 211, 211);
        public static Color Red => Color.FromArgb(255, 255, 0, 0);
        public static Color ForestGreen => Color.FromArgb(255, 34, 139, 34);
        public static Color DarkRed => Color.FromArgb(255, 139, 0, 0);
    }
}
