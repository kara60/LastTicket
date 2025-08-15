using Microsoft.Extensions.Logging;
using System.Text.Json;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Infrastructure.Repositories;

namespace TicketSystem.Infrastructure.Services;

public class PmoIntegrationService : IPmoIntegrationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PmoIntegrationService> _logger;

    public PmoIntegrationService(
        IUnitOfWork unitOfWork,
        IHttpClientFactory httpClientFactory,
        ILogger<PmoIntegrationService> logger)
    {
        _unitOfWork = unitOfWork;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<bool> IsEnabledAsync(Guid companyId)
    {
        var company = await _unitOfWork.Companies.GetByIdAsync(companyId);
        return company?.PmoIntegrationEnabled == true;
    }

    public async Task<bool> SendTicketToPmoAsync(Guid ticketId, string apiEndpoint, string apiKey)
    {
        try
        {
            var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketId,
                t => t.Type, t => t.Category, t => t.Customer!, t => t.CreatedBy);

            if (ticket == null) return false;

            var payload = new
            {
                ticketNumber = ticket.TicketNumber,
                title = ticket.Title,
                description = ticket.Description,
                type = ticket.Type.Name,
                category = ticket.Category.Name,
                customer = ticket.Customer?.Name,
                createdBy = $"{ticket.CreatedBy.FirstName} {ticket.CreatedBy.LastName}",
                createdAt = ticket.CreatedAt,
                formData = ticket.FormData
            };

            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(apiEndpoint, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Ticket {TicketId} successfully sent to PMO", ticketId);
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to send ticket {TicketId} to PMO. Status: {StatusCode}", ticketId, response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending ticket {TicketId} to PMO", ticketId);
            return false;
        }
    }

    public async Task<bool> TestConnectionAsync(string apiEndpoint, string apiKey)
    {
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var response = await httpClient.GetAsync($"{apiEndpoint}/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}