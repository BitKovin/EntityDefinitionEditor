using EntityEditor.Models;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace EntityEditor.Converters;

[ValueConversion(typeof(bool), typeof(Visibility))]
public class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }
    public object Convert(object v, Type t, object p, CultureInfo c)
    {
        bool b = v is bool bv && bv;
        if (Invert) b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object v, Type t, object p, CultureInfo c) =>
        v is Visibility vis && vis == Visibility.Visible;
}

[ValueConversion(typeof(bool), typeof(bool))]
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) => v is bool b && !b;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => v is bool b && !b;
}

[ValueConversion(typeof(bool), typeof(Visibility))]
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) =>
        v is bool b && b ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) =>
        v is Visibility vis && vis != Visibility.Visible;
}

[ValueConversion(typeof(double), typeof(double))]
public class DoubleToDoubleConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) => v;
    public object ConvertBack(object v, Type t, object p, CultureInfo c)
    {
        if (v is string s && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)) return d;
        if (v is double d2) return d2;
        return 0.0;
    }
}

// Convert three separate R/G/B values to a SolidColorBrush via MultiBinding
public class RgbToColorConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type t, object p, CultureInfo c)
    {
        try
        {
            double r = System.Convert.ToDouble(values[0]);
            double g = System.Convert.ToDouble(values[1]);
            double b = System.Convert.ToDouble(values[2]);
            return new SolidColorBrush(Color.FromRgb(
                (byte)(r * 255), (byte)(g * 255), (byte)(b * 255)));
        }
        catch { return Brushes.Gray; }
    }
    public object[] ConvertBack(object v, Type[] t, object p, CultureInfo c) => throw new NotImplementedException();
}

[ValueConversion(typeof(bool), typeof(FontStyle))]
public class BoolToItalicConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) =>
        v is bool b && b ? FontStyles.Italic : FontStyles.Normal;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

[ValueConversion(typeof(bool), typeof(double))]
public class BoolToOpacityConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) =>
        v is bool b && b ? 0.55 : 1.0;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

[ValueConversion(typeof(PropertyType), typeof(Visibility))]
public class IsFlagTypeConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) =>
        v is PropertyType pt && pt == PropertyType.Flag ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

[ValueConversion(typeof(string), typeof(bool))]
public class NotNullOrEmptyConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) => !string.IsNullOrEmpty(v as string);
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

[ValueConversion(typeof(double), typeof(string))]
public class DoubleToStringConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) =>
        v is double d ? d.ToString("G", CultureInfo.InvariantCulture) : "0";
    public object ConvertBack(object v, Type t, object p, CultureInfo c)
    {
        if (v is string s && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)) return d;
        return 0.0;
    }
}

[ValueConversion(typeof(bool), typeof(Brush))]
public class InheritedRowBrushConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) =>
        v is bool b && b
            ? new SolidColorBrush(Color.FromRgb(50, 55, 65))
            : Brushes.Transparent;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}
