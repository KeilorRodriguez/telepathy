using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Telepathic.Models;

namespace Telepathic.ViewModels
{
    public partial class ProjectTaskViewModel : ObservableObject, IEquatable<ProjectTask>
    {
        private readonly ProjectTask _task;
        private string _projectName = string.Empty;

        public ProjectTaskViewModel(ProjectTask task)
        {
            _task = task;
        }

        // Forward the model properties
        public int ID => _task.ID;
        public string Title => _task.Title;
        public bool IsCompleted 
        { 
            get => _task.IsCompleted; 
            set 
            {
                if (_task.IsCompleted != value)
                {
                    _task.IsCompleted = value;
                    OnPropertyChanged();
                }
            }
        }
        public DateTime? DueDate => _task.DueDate;
        public int Priority => _task.Priority;
        public string PriorityReasoning => _task.PriorityReasoning;
        public bool IsPriority => _task.IsPriority;
        public int ProjectID => _task.ProjectID;
        public bool IsRecommendation => _task.IsRecommendation;
        
        [ObservableProperty]
        private string projectName = string.Empty;

        // View-specific properties
        [ObservableProperty]
        private bool _isShowingReasoning;

        // Command to toggle the visibility of reasoning
        [RelayCommand]
        private void ToggleShowReasoning()
        {
            IsShowingReasoning = !IsShowingReasoning;
        }

        // Method to get the underlying model
        public ProjectTask GetModel() => _task;

        // Direct access to the task model for command binding
        public ProjectTask Task => _task;

        public bool Equals(ProjectTask? other)
        {
            if (other is null) return false;
            return ID == other.ID;
        }

        public override bool Equals(object? obj)
        {
            if (obj is ProjectTask task)
                return Equals(task);
            return false;
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }
    }
}