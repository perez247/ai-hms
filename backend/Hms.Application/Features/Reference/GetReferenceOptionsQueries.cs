using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Enums;
using MediatR;

namespace Hms.Application.Features.Reference;

public sealed record GetMedicationOptionsQuery : IRequest<IReadOnlyList<ReferenceOptionDto>>;
public sealed record GetInvestigationOptionsQuery : IRequest<IReadOnlyList<ReferenceOptionDto>>;
public sealed record GetProcedureOptionsQuery : IRequest<IReadOnlyList<ReferenceOptionDto>>;
public sealed record GetStockItemsQuery(StockItemType? Type, Guid? PayerId, string? Search = null) : IRequest<IReadOnlyList<StockItemDto>>;

public sealed class GetMedicationOptionsQueryHandler(IReferenceService reference)
    : IRequestHandler<GetMedicationOptionsQuery, IReadOnlyList<ReferenceOptionDto>>
{
    public Task<IReadOnlyList<ReferenceOptionDto>> Handle(GetMedicationOptionsQuery request, CancellationToken cancellationToken) =>
        reference.GetMedicationsAsync(cancellationToken);
}

public sealed class GetInvestigationOptionsQueryHandler(IReferenceService reference)
    : IRequestHandler<GetInvestigationOptionsQuery, IReadOnlyList<ReferenceOptionDto>>
{
    public Task<IReadOnlyList<ReferenceOptionDto>> Handle(GetInvestigationOptionsQuery request, CancellationToken cancellationToken) =>
        reference.GetInvestigationsAsync(cancellationToken);
}

public sealed class GetProcedureOptionsQueryHandler(IReferenceService reference)
    : IRequestHandler<GetProcedureOptionsQuery, IReadOnlyList<ReferenceOptionDto>>
{
    public Task<IReadOnlyList<ReferenceOptionDto>> Handle(GetProcedureOptionsQuery request, CancellationToken cancellationToken) =>
        reference.GetProceduresAsync(cancellationToken);
}

public sealed class GetStockItemsQueryHandler(IReferenceService reference)
    : IRequestHandler<GetStockItemsQuery, IReadOnlyList<StockItemDto>>
{
    public Task<IReadOnlyList<StockItemDto>> Handle(GetStockItemsQuery request, CancellationToken cancellationToken) =>
        reference.GetStockItemsAsync(request.Type, request.PayerId, request.Search, cancellationToken);
}
