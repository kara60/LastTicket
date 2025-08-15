using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using TicketSystem.Domain.Entities;

namespace TicketSystem.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Company> Companies { get; }
    DbSet<Customer> Customers { get; }
    DbSet<User> Users { get; }
    DbSet<TicketType> TicketTypes { get; }
    DbSet<TicketCategory> TicketCategories { get; }
    DbSet<TicketCategoryModule> TicketCategoryModules { get; }
    DbSet<Ticket> Tickets { get; }
    DbSet<TicketComment> TicketComments { get; }
    DbSet<TicketAttachment> TicketAttachments { get; }
    DbSet<TicketHistory> TicketHistory { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}