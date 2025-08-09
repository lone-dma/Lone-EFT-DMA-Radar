using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace eft_dma_radar.UI.Misc
{

    /// <summary>
    /// Converts a hex/color‐name string ↔ SolidColorBrush.
    /// </summary>
    public class StringToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var col = new StringToColorConverter().Convert(value, typeof(Color), parameter, culture);
            if (col is Color c)
                return new SolidColorBrush(c);
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush b)
                return b.Color.ToString();
            return string.Empty;
        }
    }
}
