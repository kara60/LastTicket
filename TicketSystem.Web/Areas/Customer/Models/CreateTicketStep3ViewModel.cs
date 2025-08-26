using System.ComponentModel.DataAnnotations;
using TicketSystem.Application.Features.Common.DTOs;

namespace TicketSystem.Web.Areas.Customer.Models;

public class CreateTicketStep3ViewModel
{
    public int SelectedTypeId { get; set; }
    public int SelectedCategoryId { get; set; }
    public TicketTypeDto? SelectedType { get; set; }
    public TicketCategoryDto? SelectedCategory { get; set; }

    // Title artık required değil - dinamik formdan alınacak
    [Display(Name = "Başlık")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Display(Name = "Modül")]
    public string? SelectedModule { get; set; }

    // Dictionary binding için property
    public Dictionary<string, object> FormData { get; set; } = new();

    // Model binding'i kolaylaştırmak için yardımcı method
    public void SetFormData(string key, object value)
    {
        FormData[key] = value;
    }

    public T? GetFormData<T>(string key)
    {
        if (FormData.ContainsKey(key))
        {
            try
            {
                return (T)Convert.ChangeType(FormData[key], typeof(T));
            }
            catch
            {
                return default(T);
            }
        }
        return default(T);
    }

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