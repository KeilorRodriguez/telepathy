using System.Globalization;

namespace Telepathic.Converters
{
    /// <summary>
    /// Converter that takes multiple boolean inputs and returns TrueObject when ALL are true,
    /// otherwise returns FalseObject
    /// </summary>
    public class BothBooleanConverter : IMultiValueConverter
    {
        /// <summary>
        /// Object to return when all values are true
        /// </summary>
        public object TrueObject { get; set; }
        
        /// <summary>
        /// Object to return when any value is false
        /// </summary>
        public object FalseObject { get; set; }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // If any value is not present or cannot be converted to bool, return false result
            if (values == null || values.Length == 0)
                return FalseObject;

            // Check if ALL values are true
            foreach (var value in values)
            {
                // Try to convert the value to boolean
                if (value is bool boolValue)
                {
                    // If any value is false, return FalseObject
                    if (!boolValue)
                        return FalseObject;
                }
                else
                {
                    // If any value is not a boolean, return FalseObject
                    return FalseObject;
                }
            }

            // All values are true
            return TrueObject;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ConvertBack method is not implemented for BothBooleanConverter");
        }
    }
}
