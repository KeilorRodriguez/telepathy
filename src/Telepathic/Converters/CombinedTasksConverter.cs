using System.Collections.ObjectModel;
using System.Globalization;

namespace Telepathic.Converters
{
    public class CombinedTasksConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
                return new List<Models.ProjectTask>();

            var regularTasks = values[0] as List<Models.ProjectTask> ?? new List<Models.ProjectTask>();
            
            // Handle both ObservableCollection and ReadOnlyObservableCollection
            IEnumerable<Models.ProjectTask> recommendedTasks;
            if (values[1] is ReadOnlyObservableCollection<Models.ProjectTask> readOnlyRecommendedTasks)
            {
                recommendedTasks = readOnlyRecommendedTasks;
            }
            else if (values[1] is ObservableCollection<Models.ProjectTask> observableRecommendedTasks)
            {
                recommendedTasks = observableRecommendedTasks;
            }
            else
            {
                recommendedTasks = values[1] as IEnumerable<Models.ProjectTask> ?? Enumerable.Empty<Models.ProjectTask>();
            }

            // Mark recommended tasks
            foreach (var task in recommendedTasks)
            {
                task.IsRecommendation = true;
            }

            // Create the combined list (regular tasks first, then recommendations)
            var combinedList = new List<Models.ProjectTask>();
            combinedList.AddRange(regularTasks);
            combinedList.AddRange(recommendedTasks);
            
            return combinedList;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("This converter does not support two-way binding");
        }
    }
}
