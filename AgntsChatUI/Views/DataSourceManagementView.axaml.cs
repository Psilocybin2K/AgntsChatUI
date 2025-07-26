namespace AgntsChatUI.Views
{
    using AgntsChatUI.ViewModels;

    using Avalonia.Controls;

    public partial class DataSourceManagementView : UserControl
    {
        public DataSourceManagementView()
        {
            this.InitializeComponent();
        }

        public DataSourceManagementView(DataSourceManagementViewModel viewModel) : this()
        {
            this.DataContext = viewModel;
        }
    }
}