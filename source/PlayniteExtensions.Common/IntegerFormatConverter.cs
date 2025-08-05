using System;

namespace PlayniteExtensions.Common;

public class IntegerFormatConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        int.TryParse(value.ToString(), out int result);
        return result;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        int.TryParse(value.ToString(), out int result);
        return result;
    }
}
