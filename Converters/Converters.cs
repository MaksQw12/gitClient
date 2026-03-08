using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace gitclient.Converters
{
    public class GreaterThanZeroConverter : IValueConverter
    {
        public static readonly GreaterThanZeroConverter Instance = new();
        public object Convert(object? value, Type t, object? p, CultureInfo c)
            => value is int i && i > 0;
        public object ConvertBack(object? value, Type t, object? p, CultureInfo c)
            => throw new NotImplementedException();
    }

    public class IsZeroConverter : IValueConverter
    {
        public static readonly IsZeroConverter Instance = new();
        public object Convert(object? value, Type t, object? p, CultureInfo c)
            => value is int i && i == 0;
        public object ConvertBack(object? value, Type t, object? p, CultureInfo c)
            => throw new NotImplementedException();
    }
}
