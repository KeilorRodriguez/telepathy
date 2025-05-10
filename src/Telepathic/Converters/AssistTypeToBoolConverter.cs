using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Telepathic.Models;

namespace Telepathic.Converters
{
    public class AssistTypeToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AssistType at)
            {
                return at != AssistType.None;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
