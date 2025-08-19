namespace AgntsChatUI.Views.Components
{
    using System;
    using System.Threading.Tasks;
    using System.Threading;

    using AgntsChatUI.ViewModels;

    using Avalonia.Controls;
    using Avalonia.Threading;
    using Avalonia.Media;
    using Avalonia;

    public partial class ChatMessagesComponent : UserControl
    {
        private ScrollViewer? _messagesScrollViewer;
        private Border? _animatedBorder;
        private CancellationTokenSource? _animationCancellationTokenSource;

        public ChatMessagesComponent()
        {
            this.InitializeComponent();
            this.Loaded += this.OnLoaded;
        }

        private void OnLoaded(object? sender, EventArgs e)
        {
            this._messagesScrollViewer = this.FindControl<ScrollViewer>("MessagesScrollViewer");
            this._animatedBorder = this.FindControl<Border>("AnimatedBorder");

            if (this.DataContext is ChatViewModel vm)
            {
                vm.ScrollToBottomRequested += this.OnScrollToBottomRequested;
                vm.PropertyChanged += this.OnViewModelPropertyChanged;
            }
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ChatViewModel.IsTyping))
            {
                if (this.DataContext is ChatViewModel vm)
                {
                    if (vm.IsTyping)
                    {
                        this.StartBorderAnimation();
                    }
                    else
                    {
                        this.StopBorderAnimation();
                    }
                }
            }
        }

        private void StartBorderAnimation()
        {
            this.StopBorderAnimation(); // Stop any existing animation
            
            if (this._animatedBorder?.BorderBrush is LinearGradientBrush gradientBrush)
            {
                this._animationCancellationTokenSource = new CancellationTokenSource();
                _ = Task.Run(async () => await this.AnimateBorderAsync(gradientBrush, this._animationCancellationTokenSource.Token));
            }
        }

        private void StopBorderAnimation()
        {
            this._animationCancellationTokenSource?.Cancel();
            this._animationCancellationTokenSource?.Dispose();
            this._animationCancellationTokenSource = null;
        }

        private async Task AnimateBorderAsync(LinearGradientBrush gradientBrush, CancellationToken cancellationToken)
        {
            double angle = 0;
            const double rotationSpeed = 4.0; // degrees per frame - increased for more pronounced effect
            const int frameDelay = 16; // ~60 FPS

            while (!cancellationToken.IsCancellationRequested)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    // Create a new gradient brush with the current rotation
                    var newGradientBrush = new LinearGradientBrush
                    {
                        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                        EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                        Transform = new RotateTransform(angle, 0.5, 0.5)
                    };

                    // Copy the gradient stops
                    foreach (var stop in gradientBrush.GradientStops)
                    {
                        newGradientBrush.GradientStops.Add(stop);
                    }

                    // Apply the new brush to the border
                    if (this._animatedBorder != null)
                    {
                        this._animatedBorder.BorderBrush = newGradientBrush;
                    }
                });

                angle += rotationSpeed;
                if (angle >= 360) angle = 0;

                await Task.Delay(frameDelay, cancellationToken);
            }
        }

        private async void OnScrollToBottomRequested()
        {
            if (this._messagesScrollViewer == null)
            {
                return;
            }

            // Wait for the next render cycle to ensure layout is updated
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                // Force a layout update
                this._messagesScrollViewer.InvalidateArrange();
                this._messagesScrollViewer.InvalidateMeasure();
            });

            // Small delay to ensure layout is updated
            await Task.Delay(100);

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
                vm.PropertyChanged += this.OnViewModelPropertyChanged;
            }
        }

        protected override void OnUnloaded(Avalonia.Interactivity.RoutedEventArgs e)
        {
            this.StopBorderAnimation();
            base.OnUnloaded(e);
        }
    }
}