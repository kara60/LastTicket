using System.ComponentModel.DataAnnotations;
using TicketSystem.Application.Features.Common.DTOs;

namespace TicketSystem.Web.Areas.Customer.Models;

public class CreateTicketStep4ViewModel
{
    public int SelectedTypeId { get; set; }
    public int SelectedCategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SelectedModule { get; set; }
    public Dictionary<string, object> FormData { get; set; } = new();
    public TicketTypeDto? SelectedType { get; set; }
    public TicketCategoryDto? SelectedCategory { get; set; }

    // Dinamik form alanlarından title çekme
    public string GetDynamicTitle()
    {
        // İlk önce FormData'dan "title", "baslik", "name" gibi alanları ara
        var titleKeys = new[] { "title", "baslik", "name", "ad", "konu" };

        foreach (var key in titleKeys)
        {
            if (FormData.ContainsKey(key) && !string.IsNullOrWhiteSpace(FormData[key]?.ToString()))
            {
                return FormData[key].ToString()!;
            }
        }

        // Eğer dinamik formda title yok ise, type ve category'den otomatik oluştur
        return $"{SelectedType?.Name} - {SelectedCategory?.Name} - {DateTime.Now:dd.MM.yyyy HH:mm}";
    }
}