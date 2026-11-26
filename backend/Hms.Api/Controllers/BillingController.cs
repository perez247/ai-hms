using Hms.Application.DTOs;
using Hms.Application.Features.Billing;
using Hms.Application.Security;
using Hms.Domain.Enums;
using Hms.Application.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hms.Api.Controllers;

[ApiController]
[Route("api/billing")]
[Authorize]
/// <summary>Finance reads, HMO routing, negotiation, payments and final approval.</summary>
public sealed class BillingController(IMediator mediator, IBillingService billing) : ControllerBase
{
    [HttpPost("bills")]
    [Authorize(Policy = Permissions.BillingApprove)]
    public async Task<ActionResult<ManualBillResultDto>> CreateBill(CreateManualBillRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new CreateManualBillCommand(request), cancellationToken));

    [HttpGet("bills")]
    [Authorize(Policy = Permissions.BillingRead)]
    public async Task<IActionResult> Search([FromQuery] BillStatus? status, [FromQuery] Guid? patientId,
        [FromQuery] bool? inDebt, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        Ok(await mediator.Send(new SearchBillsQuery(status, patientId, page, pageSize, inDebt), cancellationToken));

    [HttpGet("bills/{id:guid}")]
    [Authorize(Policy = Permissions.BillingRead)]
    public async Task<ActionResult<BillDto>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetBillQuery(id), cancellationToken));

    [HttpPost("bills/{id:guid}/lines")]
    [Authorize(Policy = Permissions.BillingApprove)]
    public async Task<IActionResult> AddLine(Guid id, AddBillLineRequest request, CancellationToken cancellationToken) =>
        Ok(await billing.AddFinanceLineAsync(id, request, cancellationToken));

    [HttpDelete("bills/{id:guid}/lines/{lineId:guid}")]
    [Authorize(Policy = Permissions.BillingApprove)]
    public async Task<IActionResult> RemoveLine(Guid id, Guid lineId, CancellationToken cancellationToken) =>
        Ok(await billing.RemoveFinanceLineAsync(id, lineId, cancellationToken));

    [HttpPost("bills/{id:guid}/route-hmo")]
    [Authorize(Policy = Permissions.BillingNegotiate)]
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

    [HttpPost("bills/{id:guid}/debt")]
    [Authorize(Policy = Permissions.BillingApprove)]
    public async Task<ActionResult<BillDto>> MoveToDebt(Guid id, MoveBillToDebtRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new MoveBillToDebtCommand(id, request), cancellationToken));

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

    [HttpGet("account-book/summary")]
    [Authorize(Policy = Permissions.AccountBookRead)]
    public async Task<ActionResult<AccountBookSummaryDto>> AccountBookSummary([FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to, CancellationToken cancellationToken = default) =>
        Ok(await billing.GetAccountBookSummaryAsync(from, to, cancellationToken));

    [HttpPost("account-book/credits")]
    [Authorize(Policy = Permissions.AccountBookWrite)]
    public async Task<ActionResult<AccountBookEntryDto>> RecordCredit(RecordCreditRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new RecordCreditCommand(request), cancellationToken));

    [HttpPost("account-book/expenses")]
    [Authorize(Policy = Permissions.AccountBookWrite)]
    public async Task<ActionResult<AccountBookEntryDto>> AddExpense(AccountBookEntryRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new AddExpenseCommand(request), cancellationToken));

    [HttpPost("account-book/purchases")]
    [Authorize(Policy = Permissions.AccountBookWrite)]
    public async Task<ActionResult<AccountBookEntryDto>> RecordPurchase(AccountBookEntryRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new RecordPurchaseCommand(request), cancellationToken));
}
