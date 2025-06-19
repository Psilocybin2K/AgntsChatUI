namespace AgntsChatUI.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;

    using CommunityToolkit.Mvvm.ComponentModel;

    public partial class MainWindowViewModel : ViewModelBase
    {
        public ChatViewModel ChatViewModel { get; } = new();
    }

    public partial class ChatViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string messageText = string.Empty;

        [ObservableProperty]
        private Contact? selectedContact;

        public ObservableCollection<Contact> Contacts { get; } = [];
        public ObservableCollection<Message> Messages { get; } = [];

        public ChatViewModel()
        {
            this.InitializeContacts();
            this.InitializeMessages();
            this.SelectedContact = this.Contacts.FirstOrDefault();
        }

        private void InitializeContacts()
        {
            this.Contacts.Add(new Contact("JD", "John Doe", "That sounds great! Let's do it.", true, "#4285f4"));
            this.Contacts.Add(new Contact("SM", "Sarah Miller", "Can you send me the files?", true, "#34a853"));
            this.Contacts.Add(new Contact("TW", "Team Weekly", "Meeting starts in 10 minutes", false, "#fbbc05"));
            this.Contacts.Add(new Contact("AL", "Alice Johnson", "Thanks for your help!", false, "#ea4335"));
        }

        private void InitializeMessages()
        {
            this.Messages.Add(new Message("JD", "Hey! How's the project coming along?", DateTime.Now.AddMinutes(-30), false));
            this.Messages.Add(new Message("ME", "Going well! Just finished the main features. Should be ready for review tomorrow.", DateTime.Now.AddMinutes(-28), true));
            this.Messages.Add(new Message("JD", "That sounds great! Let's schedule a demo session.", DateTime.Now.AddMinutes(-25), false));
            this.Messages.Add(new Message("JD", "Here's the design mockup we discussed", DateTime.Now.AddMinutes(-24), false, hasFile: true, fileName: "design-mockup-v2.pdf", fileSize: "2.4 MB"));
            this.Messages.Add(new Message("ME", "Perfect! How about 3 PM tomorrow?", DateTime.Now.AddMinutes(-24), true));
        }
    }

    public record Contact(string Avatar, string Name, string LastMessage, bool IsOnline, string AvatarColor);

    public record Message(string Avatar, string Content, DateTime Time, bool IsSent, bool hasFile = false, string fileName = "", string fileSize = "");
}
