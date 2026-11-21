using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Domain.Entities;
using Hms.Domain.Enums;

namespace Hms.Application.Services;

public interface IHrService
{
    Task<PagedResult<UserDto>> SearchAsync(bool? active, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserDto> UpdatePayrollAsync(Guid userId, UpdatePayrollRequest request, CancellationToken cancellationToken = default);
    Task<UserDto> DisableAsync(Guid userId, string reason, CancellationToken cancellationToken = default);
    Task<byte[]> ExportCsvAsync(bool? active, CancellationToken cancellationToken = default);
}
