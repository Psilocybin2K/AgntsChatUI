namespace AgntsChatUI.Views.Components
{
	using System;
	using AgntsChatUI.ViewModels;
	using Avalonia.Controls;
	using Avalonia.Input;
	using Avalonia.Interactivity;
	using Avalonia.Media;

	public partial class AgentManagementPanel : UserControl
	{
		private Border? _slidePanel;
		private Border? _backdrop;

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

			// Set up animation triggers
			this.DataContextChanged += this.OnDataContextChanged;
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
				transform.X = 300;
			}
		}

		private void OnBackdropPressed(object? sender, PointerPressedEventArgs e)
		{
			if (this.DataContext is MainWindowViewModel vm)
			{
				vm.ChatViewModel.ToggleAgentPanelCommand.Execute(null);
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

			base.OnUnloaded(e);
		}
	}
}
