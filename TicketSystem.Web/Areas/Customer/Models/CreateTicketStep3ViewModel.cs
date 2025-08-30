using System.ComponentModel.DataAnnotations;
using TicketSystem.Application.Features.Common.DTOs;

namespace TicketSystem.Web.Areas.Customer.Models;

public class CreateTicketStep3ViewModel
{
    public int SelectedTypeId { get; set; }
    public int SelectedCategoryId { get; set; }
    public TicketTypeDto? SelectedType { get; set; }
    public TicketCategoryDto? SelectedCategory { get; set; }

    [Display(Name = "Başlık")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Display(Name = "Modül")]
    public string? SelectedModule { get; set; }

    // Dictionary binding için property - PUBLIC olmalı
    public Dictionary<string, object> FormData { get; set; } = new();

    // Form data manipulation için helper methods
    public void SetFormData(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        FormData[key] = value ?? string.Empty;
    }

    public T? GetFormData<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || !FormData.ContainsKey(key))
            return default(T);

        try
        {
            return (T)Convert.ChangeType(FormData[key], typeof(T));
        }
        catch
        {
            return default(T);
        }
    }

    public string GetFormDataAsString(string key)
    {
        return FormData.ContainsKey(key) ? FormData[key]?.ToString() ?? string.Empty : string.Empty;
    }

    // Request.Form'dan FormData'yı parse etmek için method
    public void ParseFormDataFromRequest(IFormCollection form)
    {
        FormData = new Dictionary<string, object>();

        foreach (var key in form.Keys)
        {

            if (key.StartsWith("FormData[") && key.EndsWith("]"))
            {
                var fieldName = key.Substring(9, key.Length - 10);
                var value = form[key].ToString()?.Trim();

                if (!string.IsNullOrEmpty(value))
                {
                    FormData[fieldName] = value;
                }
            }
        }
    }

    // Dinamik form alanlarından title çekme
    public string GetDynamicTitle()
    {
        var titleKeys = new[] { "title", "baslik", "name", "ad", "konu", "subject", "topic" };

        foreach (var key in titleKeys)
        {
            var titleValue = GetFormDataAsString(key);
            if (!string.IsNullOrWhiteSpace(titleValue))
            {
                return titleValue;
            }
        }

        // Fallback to generated title
        return $"{SelectedType?.Name ?? "Ticket"} - {SelectedCategory?.Name ?? "Genel"} - {DateTime.Now:dd.MM.yyyy HH:mm}";
    }
}