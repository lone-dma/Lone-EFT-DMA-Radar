using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace eft_dma_radar.UI.Misc
{
    /// <summary>
    /// Converts a hex/color‐name string ↔ System.Windows.Media.Color.
    /// </summary>
    public class StringToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s && !string.IsNullOrWhiteSpace(s))
            {
                try
                {
                    return (Color)ColorConverter.ConvertFromString(s);
                }
                catch { }
            }
            return Colors.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color c)
                return c.ToString();  // e.g. "#FFAABBCC"
            return string.Empty;
        }
    }
}
