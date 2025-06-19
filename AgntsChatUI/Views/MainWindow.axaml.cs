namespace AgntsChatUI.Views
{
    using System;
    using System.Threading.Tasks;

    using AgntsChatUI.ViewModels;

    using Avalonia.Controls;
    using Avalonia.Threading;

    public partial class MainWindow : Window
    {
        private ScrollViewer? _messagesScrollViewer;

        public MainWindow()
        {
            this.InitializeComponent();
            this.Loaded += this.OnLoaded;
        }

        private void OnLoaded(object? sender, EventArgs e)
        {
            this._messagesScrollViewer = this.FindControl<ScrollViewer>("MessagesScrollViewer");

            if (this.DataContext is MainWindowViewModel vm)
            {
                vm.ChatViewModel.ScrollToBottomRequested += this.OnScrollToBottomRequested;
            }
        }

        private async void OnScrollToBottomRequested()
        {
            if (this._messagesScrollViewer == null)
            {
                return;
            }

            // Small delay to ensure layout is updated
            await Task.Delay(50);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                this._messagesScrollViewer.ScrollToEnd();
            });
        }
    }
}