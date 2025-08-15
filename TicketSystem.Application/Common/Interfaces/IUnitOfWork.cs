using TicketSystem.Domain.Entities;

namespace TicketSystem.Application.Common.Interfaces;

public interface IUnitOfWork : IDisposable
{
    // Repository properties
    IGenericRepository<Company> Companies { get; }
    IGenericRepository<Customer> Customers { get; }
    IGenericRepository<User> Users { get; }
    IGenericRepository<TicketType> TicketTypes { get; }
    IGenericRepository<TicketCategory> TicketCategories { get; }
    IGenericRepository<TicketCategoryModule> TicketCategoryModules { get; }
    IGenericRepository<Ticket> Tickets { get; }
    IGenericRepository<TicketComment> TicketComments { get; }
    IGenericRepository<TicketAttachment> TicketAttachments { get; }
    IGenericRepository<TicketHistory> TicketHistory { get; }

    // Unit of Work methods
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}