namespace TicketSystem.Application.Common.Interfaces;

public interface IPmoIntegrationService
{
    Task<bool> IsEnabledAsync(Guid companyId);
    Task<bool> SendTicketToPmoAsync(Guid ticketId, string apiEndpoint, string apiKey);
    Task<bool> TestConnectionAsync(string apiEndpoint, string apiKey);
}