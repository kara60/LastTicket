using Microsoft.EntityFrameworkCore.Storage;
using TicketSystem.Domain.Entities;
using TicketSystem.Infrastructure.Data;
using TicketSystem.Application.Common.Interfaces;

namespace TicketSystem.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;

    // Lazy repositories
    private IGenericRepository<Company>? _companies;
    private IGenericRepository<Customer>? _customers;
    private IGenericRepository<User>? _users;
    private IGenericRepository<TicketType>? _ticketTypes;
    private IGenericRepository<TicketCategory>? _ticketCategories;
    private IGenericRepository<TicketCategoryModule>? _ticketCategoryModules;
    private IGenericRepository<Ticket>? _tickets;
    private IGenericRepository<TicketComment>? _ticketComments;
    private IGenericRepository<TicketAttachment>? _ticketAttachments;
    private IGenericRepository<TicketHistory>? _ticketHistory;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    // Repository properties with lazy initialization
    public IGenericRepository<Company> Companies =>
        _companies ??= new GenericRepository<Company>(_context);

    public IGenericRepository<Customer> Customers =>
        _customers ??= new GenericRepository<Customer>(_context);

    public IGenericRepository<User> Users =>
        _users ??= new GenericRepository<User>(_context);

    public IGenericRepository<TicketType> TicketTypes =>
        _ticketTypes ??= new GenericRepository<TicketType>(_context);

    public IGenericRepository<TicketCategory> TicketCategories =>
        _ticketCategories ??= new GenericRepository<TicketCategory>(_context);

    public IGenericRepository<TicketCategoryModule> TicketCategoryModules =>
        _ticketCategoryModules ??= new GenericRepository<TicketCategoryModule>(_context);

    public IGenericRepository<Ticket> Tickets =>
        _tickets ??= new GenericRepository<Ticket>(_context);

    public IGenericRepository<TicketComment> TicketComments =>
        _ticketComments ??= new GenericRepository<TicketComment>(_context);

    public IGenericRepository<TicketAttachment> TicketAttachments =>
        _ticketAttachments ??= new GenericRepository<TicketAttachment>(_context);

    public IGenericRepository<TicketHistory> TicketHistory =>
        _ticketHistory ??= new GenericRepository<TicketHistory>(_context);

    // Unit of Work methods
    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}