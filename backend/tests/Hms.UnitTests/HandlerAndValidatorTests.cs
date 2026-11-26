using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Features.Auth;
using Hms.Application.Features.Billing;
using Hms.Application.Features.Clinical;
using Hms.Application.Features.Patients;
using Hms.Application.Features.Pharmacy;
using Hms.Application.Services;
using Hms.Domain.Enums;
using Moq;

namespace Hms.UnitTests;

/// <summary>CQRS handler delegation and FluentValidation rule coverage.</summary>
public sealed class HandlerAndValidatorTests
{
    [Fact]
    public async Task Create_patient_handler_dispatches_to_application_port()
    {
        var request = new CreatePatientRequest("Ada", null, "Okafor", Sex.Female, new DateOnly(1992, 5, 18),
            "+234", "ada@example.test", null, null, null, null, null, null, null, []);
        var expected = Patient();
        var service = new Mock<IPatientService>();
        service.Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var handler = new CreatePatientCommandHandler(service.Object);

        var result = await handler.Handle(new CreatePatientCommand(request), CancellationToken.None);

        Assert.Same(expected, result);
        service.Verify(x => x.CreateAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Approve_bill_handler_approves_then_returns_refreshed_bill()
    {
        var billId = Guid.NewGuid();
        var expected = Bill(billId);
        var billing = new Mock<IBillingService>();
        billing.Setup(x => x.ApproveAsync(billId, "ok", It.IsAny<CancellationToken>())).ReturnsAsync(new Hms.Domain.Entities.Bill());
        billing.Setup(x => x.GetAsync(billId, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var handler = new ApproveBillCommandHandler(billing.Object);

        var result = await handler.Handle(new ApproveBillCommand(billId, new ApproveBillRequest("ok")), CancellationToken.None);

        Assert.Same(expected, result);
        billing.Verify(x => x.ApproveAsync(billId, "ok", It.IsAny<CancellationToken>()), Times.Once);
        billing.Verify(x => x.GetAsync(billId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Current_user_query_returns_permissions_for_role()
    {
        var id = Guid.NewGuid();
        var current = new Mock<ICurrentUser>();
        current.SetupGet(x => x.Id).Returns(id);
        current.SetupGet(x => x.Role).Returns(StaffRole.Pharmacy);
        current.SetupGet(x => x.FullName).Returns("Pharmacy Demo");
        current.Setup(x => x.RequireId()).Returns(id);
        var handler = new GetCurrentUserQueryHandler(current.Object);

        var result = await handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        Assert.Equal(id, result.Id);
        Assert.Equal(StaffRole.Pharmacy, result.Role);
        Assert.Contains("pharmacy.commit", result.Permissions);
        Assert.Contains("billing.approve", result.Permissions);
        Assert.Contains("hmo.read", result.Permissions);
        Assert.DoesNotContain("accountbook.read", result.Permissions);
    }

    [Fact]
    public async Task Create_patient_validator_accepts_valid_request()
    {
        var validator = new CreatePatientCommandValidator();
        var command = new CreatePatientCommand(new CreatePatientRequest("Ada", null, "Okafor", Sex.Female,
            new DateOnly(1992, 5, 18), null, null, null, null, null, null, null, null, null,
            [new CreateHmoEnrollmentRequest(Guid.NewGuid(), "HG-1", "Standard", DateOnly.FromDateTime(DateTime.UtcNow), null, true)]));

        var result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Create_patient_validator_rejects_missing_names_future_dob_and_multiple_primary_hmos()
    {
        var validator = new CreatePatientCommandValidator();
        var command = new CreatePatientCommand(new CreatePatientRequest("", null, "", Sex.Female,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), null, "bad-email", null, null, null, null, null, null, null,
            [
                new CreateHmoEnrollmentRequest(Guid.NewGuid(), "", null, DateOnly.FromDateTime(DateTime.UtcNow), null, true),
                new CreateHmoEnrollmentRequest(Guid.NewGuid(), "HG-2", null, DateOnly.FromDateTime(DateTime.UtcNow), null, true)
            ]));

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.EndsWith("FirstName"));
        Assert.Contains(result.Errors, e => e.PropertyName.EndsWith("LastName"));
        Assert.Contains(result.Errors, e => e.PropertyName.EndsWith("DateOfBirth"));
        Assert.Contains(result.Errors, e => e.PropertyName.EndsWith("Email"));
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Only one HMO enrollment"));
    }

    [Fact]
    public async Task Dispense_validator_rejects_missing_lines_and_non_positive_quantity()
    {
        var validator = new DispenseCommandValidator();
        var invalid = await validator.ValidateAsync(new DispenseCommand(new DispenseRequest(Guid.NewGuid(),
            [new DispenseItemRequest(Guid.NewGuid(), 0)])));
        var noLines = await validator.ValidateAsync(new DispenseCommand(new DispenseRequest(Guid.NewGuid(), [])));
        var valid = await validator.ValidateAsync(new DispenseCommand(new DispenseRequest(Guid.NewGuid(),
            [new DispenseItemRequest(Guid.NewGuid(), 2)])));

        Assert.False(invalid.IsValid);
        Assert.False(noLines.IsValid);
        Assert.True(valid.IsValid);
    }

    [Fact]
    public async Task Create_patient_validator_rejects_overlong_demographic_fields()
    {
        var validator = new CreatePatientCommandValidator();
        var command = new CreatePatientCommand(new CreatePatientRequest("Ada", null, "Okafor", Sex.Female,
            new DateOnly(1992, 5, 18), null, null, null, new string('A', 101), "Lagos", "Penicillin, sulfa", "Nursing",
            null, null,
            [
                new CreateHmoEnrollmentRequest(Guid.NewGuid(), "HG-1", "Standard", DateOnly.FromDateTime(DateTime.UtcNow), null, true)
            ]));

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("State Of Origin"));
    }

    [Fact]
    public async Task Update_patient_validator_rejects_missing_names_and_overlong_fields()
    {
        var validator = new UpdatePatientCommandValidator();
        var command = new UpdatePatientCommand(Guid.NewGuid(), new UpdatePatientRequest("", null, "", Sex.Female,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), null, null, null, new string('A', 101), "Lagos",
            "Penicillin", "Nursing", null, null));

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.EndsWith("FirstName"));
        Assert.Contains(result.Errors, e => e.PropertyName.EndsWith("DateOfBirth"));
        Assert.Contains(result.Errors, e => e.PropertyName.EndsWith("StateOfOrigin"));
    }

    [Fact]
    public async Task Create_outpatient_encounter_validator_rejects_presenting_complaint_over_1000_chars()
    {
        var validator = new CreateOutpatientEncounterCommandValidator();
        var command = new CreateOutpatientEncounterCommand(new CreateOutpatientEncounterRequest(
            Guid.NewGuid(), Guid.NewGuid(), null, false, null, new string('F', 1001)));

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Save_encounter_validator_rejects_over_20_examinations_and_incomplete_sign()
    {
        var validator = new SaveEncounterCommandValidator();
        var tooMany = await validator.ValidateAsync(new SaveEncounterCommand(Guid.NewGuid(), new SaveEncounterRequest(
            "Fever", null, null, null, "R50.9", "Discharge",
            [.. Enumerable.Range(0, 21).Select(i => new ExaminationItemRequest($"System {i}", "Normal"))], true)));
        var thirtyOk = await validator.ValidateAsync(new SaveEncounterCommand(Guid.NewGuid(), new SaveEncounterRequest(
            "Fever", null, null, null, "R50.9", "Discharge",
            [.. Enumerable.Range(0, 20).Select(i => new ExaminationItemRequest($"System {i}", "Normal"))], false)));
        var incompleteSign = await validator.ValidateAsync(new SaveEncounterCommand(Guid.NewGuid(), new SaveEncounterRequest(
            null, null, null, null, null, null, null, true)));

        Assert.False(tooMany.IsValid);
        Assert.True(thirtyOk.IsValid);
        Assert.False(incompleteSign.IsValid);
    }

    [Fact]
    public async Task Save_encounter_validator_requires_complete_plan_rows_even_for_drafts()
    {
        var validator = new SaveEncounterCommandValidator();
        var testId = Guid.NewGuid();
        var missingDose = await validator.ValidateAsync(new SaveEncounterCommand(Guid.NewGuid(), new SaveEncounterRequest(
            "Fever", null, null, null, null, null, null, false,
            Medications: [new PrescribeOrderRequest(Guid.NewGuid(), "", "Oral", "TDS", 5, 15)])));
        var badRoute = await validator.ValidateAsync(new SaveEncounterCommand(Guid.NewGuid(), new SaveEncounterRequest(
            "Fever", null, null, null, null, null, null, false,
            Medications: [new PrescribeOrderRequest(Guid.NewGuid(), "500 mg", "Buccal", "TDS", 5, 15)])));
        var zeroQuantity = await validator.ValidateAsync(new SaveEncounterCommand(Guid.NewGuid(), new SaveEncounterRequest(
            "Fever", null, null, null, null, null, null, false,
            Medications: [new PrescribeOrderRequest(Guid.NewGuid(), "500 mg", "Oral", "TDS", 5, 0)])));
        var duplicateTests = await validator.ValidateAsync(new SaveEncounterCommand(Guid.NewGuid(), new SaveEncounterRequest(
            "Fever", null, null, null, null, null, null, false,
            Tests:
            [
                new ServiceOrderRequest(testId, "Routine", null),
                new ServiceOrderRequest(testId, "Routine", null)
            ])));
        var complete = await validator.ValidateAsync(new SaveEncounterCommand(Guid.NewGuid(), new SaveEncounterRequest(
            "Fever", null, null, null, null, null, null, false,
            Medications: [new PrescribeOrderRequest(Guid.NewGuid(), "500 mg", "Oral", "TDS", 5, 15)],
            Tests: [new ServiceOrderRequest(Guid.NewGuid(), "Routine", "Note")],
            Procedures: [new ServiceOrderRequest(Guid.NewGuid(), "Urgent", null)])));

        Assert.False(missingDose.IsValid);
        Assert.False(badRoute.IsValid);
        Assert.False(zeroQuantity.IsValid);
        Assert.False(duplicateTests.IsValid);
        Assert.True(complete.IsValid);
    }

    [Fact]
    public async Task Billing_validator_rejects_empty_id_and_non_positive_payment()
    {
        var approve = new ApproveBillCommandValidator();
        var payment = new RecordPaymentCommandValidator();
        Assert.False((await approve.ValidateAsync(new ApproveBillCommand(Guid.Empty, new ApproveBillRequest(null)))).IsValid);
        Assert.False((await payment.ValidateAsync(new RecordPaymentCommand(Guid.NewGuid(),
            new PaymentRequest(0, PaymentMethod.Cash, null, null, null)))).IsValid);
    }

    [Fact]
    public async Task Hmo_pa_validator_requires_exactly_one_context()
    {
        var validator = new Hms.Application.Features.Hmo.SavePriorAuthorizationCommandValidator();
        var request = new SavePriorAuthorizationRequest(Guid.NewGuid(), Guid.NewGuid(), null, null, "PA-1", null, null, null);
        var result = await validator.ValidateAsync(new Hms.Application.Features.Hmo.SavePriorAuthorizationCommand(request));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("exactly one encounter"));
    }

    private static PatientDto Patient() => new(Guid.NewGuid(), "PAT-1", "Ada Okafor", "Ada", "Okafor", Sex.Female,
        new DateOnly(1992, 5, 18), null, null, null, null, null, null, null, null, null,
        DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2)), PatientStatus.Active, []);

    private static BillDto Bill(Guid id) => new(id, "BILL-1", BillKind.OutpatientEncounter, BillStatus.Committed,
        Guid.NewGuid(), "PAT-1", "Ada Okafor", PayerType.SelfPay, null, null, null, null, "NGN", 100, 0, 0, 100, 0, 100,
        null, null, null, null, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null, null,
        [], [], [], null, false, null, null, null, null);
}
