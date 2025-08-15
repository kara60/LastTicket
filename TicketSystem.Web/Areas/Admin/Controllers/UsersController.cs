using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Infrastructure.Repositories;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;
using TicketSystem.Web.Areas.Admin.Models;

namespace TicketSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class UsersController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public UsersController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _unitOfWork.Users.GetAllAsync(x => x.Customer!);
        return View(users);
    }

    public async Task<IActionResult> Create()
    {
        var customers = await _unitOfWork.Customers.GetAllAsync();
        ViewBag.Customers = customers;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var customers = await _unitOfWork.Customers.GetAllAsync();
            ViewBag.Customers = customers;
            return View(model);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            CompanyId = Guid.Parse("your-company-id"), // Bu değer current user'dan alınmalı
            CustomerId = model.Role == UserRole.Customer ? model.CustomerId : null,
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.Email,
            Username = model.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            Role = model.Role,
            IsActive = true
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "Kullanıcı başarıyla oluşturuldu.";
        return RedirectToAction("Index");
    }
}