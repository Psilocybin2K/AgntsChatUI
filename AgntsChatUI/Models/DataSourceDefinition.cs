namespace AgntsChatUI.Models
{
    using System;

    using CommunityToolkit.Mvvm.ComponentModel;

    // Database entity for storing data source configurations
    public partial class DataSourceDefinition : ObservableObject
    {
        public int? Id { get; set; }

        [ObservableProperty]
        private string name = string.Empty;

        [ObservableProperty]
        private string description = string.Empty;

        public DataSourceType Type { get; set; }
        public string ConfigurationJson { get; set; } = string.Empty;

        [ObservableProperty]
        private bool isEnabled = true;

        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
    }
}