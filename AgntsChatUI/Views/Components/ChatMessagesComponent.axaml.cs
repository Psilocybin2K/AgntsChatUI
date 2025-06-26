namespace AgntsChatUI.Views.Components
{
    using System;
    using System.Threading.Tasks;

    using AgntsChatUI.ViewModels;

    using Avalonia.Controls;
    using Avalonia.Threading;

    public partial class ChatMessagesComponent : UserControl
    {
        private ScrollViewer? _messagesScrollViewer;

        public ChatMessagesComponent()
        {
            this.InitializeComponent();
            this.Loaded += this.OnLoaded;
        }

        private void OnLoaded(object? sender, EventArgs e)
        {
            this._messagesScrollViewer = this.FindControl<ScrollViewer>("MessagesScrollViewer");

            if (this.DataContext is ChatViewModel vm)
            {
                vm.ScrollToBottomRequested += this.OnScrollToBottomRequested;
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

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            // Handle DataContext changes to wire up scroll events properly
            if (this.DataContext is ChatViewModel vm && this._messagesScrollViewer != null)
            {
                vm.ScrollToBottomRequested += this.OnScrollToBottomRequested;
            }
        }
    }
}