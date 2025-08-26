using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketSystem.Application.Features.Common.DTOs
{
    public class TicketTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string? FormDefinition { get; set; } // Dictionary'den string'e değiştir
        public bool IsActive { get; set; } = true; // Ekle
        public int SortOrder { get; set; } = 0; // Ekle
    }
}
