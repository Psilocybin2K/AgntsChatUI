namespace AgntsChatUI.Views.Components
{
	using System;
	using System.Collections.Generic;
	using System.Threading;
	using System.Threading.Tasks;
	using AgntsChatUI.ViewModels;
	using Avalonia;
	using Avalonia.Controls;
	using Avalonia.Input;
	using Avalonia.Interactivity;
	using Avalonia.Media;
	using Avalonia.Threading;

	public partial class AgentManagementPanel : UserControl
	{
		private Border? _slidePanel;
		private Border? _backdrop;
		private TopLevel? _topLevel;
		private Dictionary<Border, CancellationTokenSource> _hoverAnimations = new();

		public AgentManagementPanel()
		{
			this.InitializeComponent();
			this.Loaded += this.OnLoaded;
		}

		private void OnLoaded(object? sender, EventArgs e)
		{
			this._backdrop = this.FindControl<Border>("Backdrop");
			this._slidePanel = this.FindControl<Border>("SlidePanel");
			
			if (this._backdrop != null)
			{
				this._backdrop.PointerPressed += this.OnBackdropPressed;
			}

			// Subscribe to window size to keep panel at 33% width
			this._topLevel = TopLevel.GetTopLevel(this);
			if (this._topLevel != null)
			{
				this._topLevel.PropertyChanged += this.OnTopLevelPropertyChanged;
				this.UpdatePanelWidth(this._topLevel.ClientSize.Width);
			}
			else
			{
				// Fallback if TopLevel is not available yet
				this.UpdatePanelWidth(1200); // Default width
			}

			// If DataContext is already set, subscribe now
			if (this.DataContext is MainWindowViewModel currentVm)
			{
				currentVm.ChatViewModel.PropertyChanged += this.OnChatViewModelPropertyChanged;
				// Initialize position to reflect current state
				if (currentVm.ChatViewModel.IsAgentPanelOpen)
				{
					this.AnimateSlideIn();
				}
				else
				{
					this.AnimateSlideOut();
				}
			}

			// Set up animation triggers for future DataContext changes
			this.DataContextChanged += this.OnDataContextChanged;

			// Set up agent card hover animations
			this.SetupAgentCardAnimations();
		}

		private void OnTopLevelPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
		{
			if (e.Property == TopLevel.ClientSizeProperty)
			{
				var size = (Size)e.NewValue!;
				this.UpdatePanelWidth(size.Width);
			}
		}

		private void OnDataContextChanged(object? sender, EventArgs e)
		{
			if (this.DataContext is MainWindowViewModel vm)
			{
				// Subscribe to panel state changes
				vm.ChatViewModel.PropertyChanged += this.OnChatViewModelPropertyChanged;
			}
		}

		private void OnChatViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ChatViewModel.IsAgentPanelOpen) && this._slidePanel != null)
			{
				var vm = (ChatViewModel)sender!;
				if (vm.IsAgentPanelOpen)
				{
					// Slide in
					this.AnimateSlideIn();
				}
				else
				{
					// Slide out
					this.AnimateSlideOut();
				}
			}
		}

		private void UpdatePanelWidth(double hostWidth)
		{
			if (this._slidePanel == null)
			{
				return;
			}

			double targetWidth = Math.Max(240, hostWidth * 0.33);
			this._slidePanel.Width = targetWidth;

			// If currently hidden, ensure it's positioned off-screen by its width
			var vm = (this.DataContext as MainWindowViewModel)?.ChatViewModel;
			bool isOpen = vm?.IsAgentPanelOpen == true;
			if (!isOpen && this._slidePanel.RenderTransform is TranslateTransform transform)
			{
				transform.X = targetWidth;
			}
		}

		private void AnimateSlideIn()
		{
			if (this._slidePanel?.RenderTransform is TranslateTransform transform)
			{
				transform.X = 0;
			}
		}

		private void AnimateSlideOut()
		{
			if (this._slidePanel?.RenderTransform is TranslateTransform transform)
			{
				double width = this._slidePanel.Width;
				transform.X = width > 0 ? width : 300;
			}
		}

		private void OnBackdropPressed(object? sender, PointerPressedEventArgs e)
		{
			if (this.DataContext is MainWindowViewModel vm)
			{
				vm.ChatViewModel.ToggleAgentPanelCommand.Execute(null);
			}
		}

		private void SetupAgentCardAnimations()
		{
			// For now, we'll use a timer-based approach to check for hover
			// This is a workaround since pointer events are not easily accessible in DataTemplates
			var timer = new System.Timers.Timer(100); // Check every 100ms
			timer.Elapsed += this.CheckForHoveredCards;
			timer.Start();
		}

		private void CheckForHoveredCards(object? sender, System.Timers.ElapsedEventArgs e)
		{
			// This is a simplified approach - in a real implementation, you'd want to use proper event handling
			// For now, we'll just ensure the animation code is available but not actively used
		}

		private void OnAgentCardPointerEnter(object? sender, PointerEventArgs e)
		{
			if (sender is Border agentCard)
			{
				this.StartAgentCardAnimation(agentCard);
			}
		}

		private void OnAgentCardPointerLeave(object? sender, PointerEventArgs e)
		{
			if (sender is Border agentCard)
			{
				this.StopAgentCardAnimation(agentCard);
			}
		}

		private void StartAgentCardAnimation(Border agentCard)
		{
			this.StopAgentCardAnimation(agentCard); // Stop any existing animation

			var animatedBorder = agentCard.FindControl<Border>("AnimatedBorder");
			var regularContent = agentCard.FindControl<Border>("RegularContent");

			if (animatedBorder != null && regularContent != null)
			{
				// Show animated border, hide regular content
				animatedBorder.IsVisible = true;
				regularContent.IsVisible = false;

				// Start the rotation animation
				if (animatedBorder.BorderBrush is LinearGradientBrush gradientBrush)
				{
					var cts = new CancellationTokenSource();
					this._hoverAnimations[agentCard] = cts;
					_ = Task.Run(async () => await this.AnimateAgentBorderAsync(gradientBrush, cts.Token));
				}
			}
		}

		private void StopAgentCardAnimation(Border agentCard)
		{
			var animatedBorder = agentCard.FindControl<Border>("AnimatedBorder");
			var regularContent = agentCard.FindControl<Border>("RegularContent");

			if (animatedBorder != null && regularContent != null)
			{
				// Hide animated border, show regular content
				animatedBorder.IsVisible = false;
				regularContent.IsVisible = true;
			}

			// Stop the animation
			if (this._hoverAnimations.TryGetValue(agentCard, out var cts))
			{
				cts.Cancel();
				cts.Dispose();
				this._hoverAnimations.Remove(agentCard);
			}
		}

		private async Task AnimateAgentBorderAsync(LinearGradientBrush gradientBrush, CancellationToken cancellationToken)
		{
			double angle = 0;
			const double rotationSpeed = 4.0; // degrees per frame
			const int frameDelay = 16; // ~60 FPS

			while (!cancellationToken.IsCancellationRequested)
			{
				await Dispatcher.UIThread.InvokeAsync(() =>
				{
					// Apply rotation transform to the gradient brush
					gradientBrush.Transform = new RotateTransform(angle, 0.5, 0.5);
				});

				angle += rotationSpeed;
				if (angle >= 360) angle = 0;

				await Task.Delay(frameDelay, cancellationToken);
			}
		}

		protected override void OnUnloaded(RoutedEventArgs e)
		{
			if (this._backdrop != null)
			{
				this._backdrop.PointerPressed -= this.OnBackdropPressed;
			}

			if (this.DataContext is MainWindowViewModel vm)
			{
				vm.ChatViewModel.PropertyChanged -= this.OnChatViewModelPropertyChanged;
			}

			if (this._topLevel != null)
			{
				this._topLevel.PropertyChanged -= this.OnTopLevelPropertyChanged;
				this._topLevel = null;
			}

			base.OnUnloaded(e);
		}
	}
}
