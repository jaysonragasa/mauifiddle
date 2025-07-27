using System.Globalization;

namespace MAUIFiddle.Converters
{
	public class ToReverseBooleanConverter : IValueConverter
	{
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> value is bool booleanValue ? !booleanValue : false;

		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> value;
	}
}
