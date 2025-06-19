namespace AgntsChatUI.Models
{
    using System;

    public record Message(
        string Id,
        string Content,
        string SenderName,
        string SenderInitials,
        DateTime Timestamp,
        bool IsSent,
        string AvatarColor = "#4285f4"
    );

    public record ChatContact(
        string Id,
        string Name,
        string Initials,
        string LastMessage,
        DateTime LastMessageTime,
        bool IsOnline,
        string AvatarColor = "#4285f4"
    );
}
