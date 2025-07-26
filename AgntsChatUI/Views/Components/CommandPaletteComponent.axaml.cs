namespace AgntsChatUI.Views.Components
{
    using System;

    using Avalonia.Controls;
    using Avalonia.Input;
    using Avalonia.Markup.Xaml;

    public partial class CommandPaletteComponent : UserControl
    {
        public event EventHandler? RequestClose;
        public CommandPaletteComponent()
        {
            this.InitializeComponent();
            this.AttachedToVisualTree += (s, e) =>
            {
                TextBox? input = this.FindControl<TextBox>("InputBox");
                if (input != null)
                {
                    input.Focus();
                    input.LostFocus += (sender, args) => RequestClose?.Invoke(this, EventArgs.Empty);
                    input.KeyDown += (sender, args) =>
                    {
                        if (args.Key == Key.Escape)
                        {
                            RequestClose?.Invoke(this, EventArgs.Empty);
                            args.Handled = true;
                        }
                    };
                }
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}