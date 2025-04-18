using System.Globalization;

namespace Telepathic.Converters;

public class PhaseToReviewBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is PageModels.VoicePhase phase)
        {
            return phase == PageModels.VoicePhase.Reviewing;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
