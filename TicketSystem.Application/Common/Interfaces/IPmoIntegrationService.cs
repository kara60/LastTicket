namespace TicketSystem.Application.Common.Interfaces;

public interface IPmoIntegrationService
{
    Task<bool> IsEnabledAsync(int companyId);
    Task<bool> SendTicketToPmoAsync(int ticketId, string apiEndpoint, string apiKey);
    Task<bool> TestConnectionAsync(string apiEndpoint, string apiKey);
}