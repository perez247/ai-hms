using FluentValidation;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Audit;

public sealed record SearchAuditLogsQuery(string? EntityType, Guid? BillId, DateOnly? From, DateOnly? To, int Page, int PageSize)
    : IRequest<PagedResult<AuditLogDto>>;

public sealed class SearchAuditLogsQueryHandler(IAuditQueryService audit)
    : IRequestHandler<SearchAuditLogsQuery, PagedResult<AuditLogDto>>
{
    public Task<PagedResult<AuditLogDto>> Handle(SearchAuditLogsQuery request, CancellationToken cancellationToken) =>
        audit.SearchAsync(request.EntityType, request.BillId, request.From, request.To, request.Page, request.PageSize, cancellationToken);
}

public sealed class SearchAuditLogsQueryValidator : AbstractValidator<SearchAuditLogsQuery>
{
    public SearchAuditLogsQueryValidator()
    {
        RuleFor(x => x.EntityType).MaximumLength(100);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PageSize).GreaterThanOrEqualTo(0);
        RuleFor(x => x).Must(x => x.From is null || x.To is null || x.From <= x.To)
            .WithMessage("'From' must be on or before 'To'.");
    }
}
