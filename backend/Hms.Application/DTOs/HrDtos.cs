using Hms.Domain.Enums;

namespace Hms.Application.DTOs;

public sealed record CreateUserRequest(
    string FullName,
    string Email,
    string Password,
    StaffRole StaffRole,
    string? EmployeeNumber,
    string? Phone,
    decimal? SalaryAmount,
    string? BankName,
    string? BankAccountName,
    string? BankAccountNumber);

public sealed record UpdatePayrollRequest(
    decimal? SalaryAmount,
    string? BankName,
    string? BankAccountName,
    string? BankAccountNumber);

public sealed record DisableUserRequest(string Reason);

public sealed record UserDto(
    Guid Id,
    string FullName,
    string Email,
    StaffRole StaffRole,
    string? EmployeeNumber,
    string? Phone,
    bool IsActive,
    decimal? SalaryAmount,
    string? BankName,
    string? BankAccountName,
    string? BankAccountNumber,
    DateOnly? HiredOn,
    DateTimeOffset? DeactivatedAt);
