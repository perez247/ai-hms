using Hms.Application.DTOs;
using Hms.Application.Features.Billing;
using Hms.Application.Security;
using Hms.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hms.Api.Controllers;

[ApiController]
[Route("api/billing")]
[Authorize]
/// <summary>Finance reads, HMO routing, negotiation, payments and final approval.</summary>
public sealed class BillingController(IMediator mediator) : ControllerBase
{
    [HttpPost("bills")]
    [Authorize(Policy = Permissions.BillingApprove)]
    public async Task<ActionResult<ManualBillResultDto>> CreateBill(CreateManualBillRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new CreateManualBillCommand(request), cancellationToken));

    [HttpGet("bills")]
    [Authorize(Policy = Permissions.BillingRead)]
    public async Task<IActionResult> Search([FromQuery] BillStatus? status, [FromQuery] Guid? patientId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        Ok(await mediator.Send(new SearchBillsQuery(status, patientId, page, pageSize), cancellationToken));

    [HttpGet("bills/{id:guid}")]
    [Authorize(Policy = Permissions.BillingRead)]
    public async Task<ActionResult<BillDto>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetBillQuery(id), cancellationToken));

    [HttpPost("bills/{id:guid}/route-hmo")]
    [Authorize(Policy = Permissions.BillingRead)]
    public async Task<ActionResult<BillDto>> RouteToHmo(Guid id, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new RouteBillToHmoCommand(id), cancellationToken));

    [HttpPost("bills/{id:guid}/negotiate")]
    [Authorize(Policy = Permissions.BillingNegotiate)]
    public async Task<ActionResult<BillDto>> Negotiate(Guid id, NegotiateBillRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new NegotiateBillCommand(id, request), cancellationToken));

    [HttpPost("bills/{id:guid}/payments")]
    [Authorize(Policy = Permissions.BillingPayment)]
    public async Task<ActionResult<BillDto>> RecordPayment(Guid id, PaymentRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new RecordPaymentCommand(id, request), cancellationToken));

    [HttpPost("bills/{id:guid}/approve")]
    [Authorize(Policy = Permissions.BillingApprove)]
    public async Task<ActionResult<BillDto>> Approve(Guid id, ApproveBillRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new ApproveBillCommand(id, request), cancellationToken));

    [HttpGet("account-book")]
    [Authorize(Policy = Permissions.AccountBookRead)]
    public async Task<IActionResult> AccountBook([FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] AccountEntryType? type, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await mediator.Send(new GetAccountBookQuery(from, to, type, page, pageSize), cancellationToken));

    [HttpPost("account-book/expenses")]
    [Authorize(Policy = Permissions.AccountBookWrite)]
    public async Task<ActionResult<AccountBookEntryDto>> AddExpense(AccountBookEntryRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new AddExpenseCommand(request), cancellationToken));

    [HttpPost("account-book/purchases")]
    [Authorize(Policy = Permissions.AccountBookWrite)]
    public async Task<ActionResult<AccountBookEntryDto>> RecordPurchase(AccountBookEntryRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new RecordPurchaseCommand(request), cancellationToken));
}
