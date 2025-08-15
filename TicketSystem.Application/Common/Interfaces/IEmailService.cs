namespace TicketSystem.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string body, bool isHtml = true);
    Task SendTicketCreatedNotificationAsync(string to, string ticketNumber, string title);
    Task SendTicketStatusChangedNotificationAsync(string to, string ticketNumber, string oldStatus, string newStatus);
    Task SendTicketCommentNotificationAsync(string to, string ticketNumber, string commenterName, string comment);
}