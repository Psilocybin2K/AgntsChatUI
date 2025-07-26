namespace AgntsChatUI.AI
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class AgentDefinition : INotifyPropertyChanged
    {
        private bool _isSelected;

        public int? Id { get; set; }
        public required string Description { get; set; }
        public required string InstructionsPath { get; set; }
        public required string Name { get; set; }
        public required string PromptyPath { get; set; }

        public bool IsSelected
        {
            get => this._isSelected;
            set
            {
                if (this._isSelected != value)
                {
                    this._isSelected = value;
                    this.OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
