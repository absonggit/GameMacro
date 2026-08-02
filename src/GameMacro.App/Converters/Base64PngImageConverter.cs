using System.Globalization;
using System.Windows.Data;
using GameMacro.App.Detection;

namespace GameMacro.App.Converters;

public sealed class Base64PngImageConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => PngPreviewCodec.Decode(value as string);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
