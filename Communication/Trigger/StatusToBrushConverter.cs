using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace NINA.StarMessenger.Communication.Trigger
{
    public class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                ConsecutiveCountStatusLevelType.Ok => Brushes.Green,
                ConsecutiveCountStatusLevelType.Warning => Brushes.Orange,
                ConsecutiveCountStatusLevelType.Error => Brushes.Red,
                _ => Brushes.Gray
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
