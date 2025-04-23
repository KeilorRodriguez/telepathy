using System.Globalization;

namespace Telepathic.Converters;

public class PhaseToRecordingBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is PageModels.VoicePhase phase)
        {
            return phase == PageModels.VoicePhase.Recording;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
