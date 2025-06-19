namespace AgntsChatUI.Views
{
    using AgntsChatUI.ViewModels;

    using Avalonia.Controls;

    public partial class DocumentManagementView : UserControl
    {
        public DocumentManagementView()
        {
            this.InitializeComponent();
        }

        public DocumentManagementView(DocumentManagementViewModel viewModel) : this()
        {
            this.DataContext = viewModel;
        }
    }
}