using Microsoft.Extensions.Logging;
using TicketSystem.Application.Common.Interfaces;

namespace TicketSystem.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string body, bool isHtml = true)
    {
        // TODO: Implement actual email sending
        _logger.LogInformation("Email sent to {To} with subject {Subject}", to, subject);
        await Task.CompletedTask;
    }

    public async Task SendTicketCreatedNotificationAsync(string to, string ticketNumber, string title)
    {
        var subject = $"Yeni Ticket Oluşturuldu - {ticketNumber}";
        var body = $"Ticket #{ticketNumber} başarıyla oluşturuldu.\nBaşlık: {title}";
        await SendAsync(to, subject, body);
    }

    public async Task SendTicketStatusChangedNotificationAsync(string to, string ticketNumber, string oldStatus, string newStatus)
    {
        var subject = $"Ticket Durumu Değişti - {ticketNumber}";
        var body = $"Ticket #{ticketNumber} durumu {oldStatus} -> {newStatus} olarak değişti.";
        await SendAsync(to, subject, body);
    }

    public async Task SendTicketCommentNotificationAsync(string to, string ticketNumber, string commenterName, string comment)
    {
        var subject = $"Yeni Yorum - {ticketNumber}";
        var body = $"Ticket #{ticketNumber}'e {commenterName} tarafından yeni yorum eklendi:\n{comment}";
        await SendAsync(to, subject, body);
    }
}