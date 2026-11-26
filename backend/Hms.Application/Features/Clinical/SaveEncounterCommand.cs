using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Clinical;

public sealed record SaveEncounterCommand(Guid EncounterId, SaveEncounterRequest Request) : IRequest<EncounterDto>;

public sealed class SaveEncounterCommandHandler(IClinicalService clinical) : IRequestHandler<SaveEncounterCommand, EncounterDto>
{
    public Task<EncounterDto> Handle(SaveEncounterCommand request, CancellationToken cancellationToken) =>
        clinical.SaveEncounterAsync(request.EncounterId, request.Request, cancellationToken);
}

public sealed class SaveEncounterCommandValidator : AbstractValidator<SaveEncounterCommand>
{
    private static readonly string[] ValidDispositions =
    [
        nameof(Hms.Domain.Enums.EncounterDisposition.Discharge),
        nameof(Hms.Domain.Enums.EncounterDisposition.FollowUp),
        nameof(Hms.Domain.Enums.EncounterDisposition.Admit),
        nameof(Hms.Domain.Enums.EncounterDisposition.ReferToSpecialist),
    ];

    private static readonly string[] ValidRoutes = ["Oral", "IV", "IM", "Topical", "Inhaled"];
    private static readonly string[] ValidFrequencies = ["OD", "BD", "TDS", "QID", "PRN"];
    private static readonly string[] ValidPriorities = ["Routine", "Urgent"];

    public SaveEncounterCommandValidator()
    {
        RuleFor(x => x.Request.PresentingComplaint).MaximumLength(1000);
        RuleFor(x => x.Request.PresentingComplaintDurationDays)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Request.PresentingComplaintDurationDays is not null);
        RuleFor(x => x.Request.HistoryOfPresentingComplaint).MaximumLength(1000);
        RuleFor(x => x.Request.PastMedicalSurgicalHistory).MaximumLength(1000);
        RuleFor(x => x.Request.FamilyHistory).MaximumLength(1000);
        RuleFor(x => x.Request.Diagnosis).MaximumLength(1000);
        RuleFor(x => x.Request.Disposition)
            .Must(value => value is null || ValidDispositions.Contains(value))
            .WithMessage("Disposition must be one of Discharge, Follow-up, Admit, Refer to Specialist.");
        RuleFor(x => x.Request.Examination)
            .Must(examination => examination is null || examination.Count <= 20)
            .WithMessage("At most 20 examination findings are allowed.");
        RuleFor(x => x.Request.Clerking!.RosNotes).MaximumLength(2000).When(x => x.Request.Clerking is not null);
        RuleFor(x => x.Request.Clerking!.GeneralObservation).MaximumLength(1000).When(x => x.Request.Clerking is not null);
        RuleFor(x => x.Request.Clerking!.ClinicalImpression).MaximumLength(2000).When(x => x.Request.Clerking is not null);
        RuleFor(x => x.Request.Clerking!.PlanAdvice).MaximumLength(2000).When(x => x.Request.Clerking is not null);
        RuleFor(x => x.Request.Clerking!.ReferralDetails).MaximumLength(2000).When(x => x.Request.Clerking is not null);
        RuleFor(x => x.Request.Clerking!.Allergies)
            .Must(allergies => allergies is null || allergies.Count <= 20)
            .WithMessage("At most 20 allergies are allowed.")
            .When(x => x.Request.Clerking is not null);
        RuleFor(x => x.Request.Clerking!.PointOfCare)
            .Must(items => items is null || items.Count <= 20)
            .WithMessage("At most 20 point-of-care results are allowed.")
            .When(x => x.Request.Clerking is not null);

        // Plan rows are optional, but any row that is supplied must be complete. These rules apply to
        // draft saves as well as signing so a partially-filled row can never be persisted.
        RuleForEach(x => x.Request.Medications).ChildRules(row =>
        {
            row.RuleFor(x => x.StockItemId).NotEmpty()
                .WithMessage("Select a medication for every prescription row.");
            row.RuleFor(x => x.Dose).NotEmpty().MaximumLength(100)
                .WithMessage("Enter a dose for every medication.");
            row.RuleFor(x => x.Route).NotEmpty().Must(ValidRoutes.Contains)
                .WithMessage("Select a valid medication route.");
            row.RuleFor(x => x.Frequency).NotEmpty().Must(ValidFrequencies.Contains)
                .WithMessage("Select a valid medication frequency.");
            row.RuleFor(x => x.DurationDays).GreaterThanOrEqualTo(1)
                .WithMessage("Medication duration must be at least one day.");
            row.RuleFor(x => x.Quantity).GreaterThan(0)
                .WithMessage("Medication quantity must be greater than zero.");
            row.RuleFor(x => x.Instructions).MaximumLength(500);
        });
        RuleFor(x => x.Request.Medications)
            .Must(items => items is null || items.Select(i => i.StockItemId).Distinct().Count() == items.Count)
            .WithMessage("The same medication cannot be prescribed twice on one encounter.");

        RuleForEach(x => x.Request.Tests).ChildRules(row =>
        {
            row.RuleFor(x => x.StockItemId).NotEmpty()
                .WithMessage("Select a test for every lab order row.");
            row.RuleFor(x => x.Priority).Must(value => value is null || ValidPriorities.Contains(value))
                .WithMessage("Test priority must be Routine or Urgent.");
            row.RuleFor(x => x.ClinicalNote).MaximumLength(500);
        });
        RuleFor(x => x.Request.Tests)
            .Must(items => items is null || items.Select(i => i.StockItemId).Distinct().Count() == items.Count)
            .WithMessage("The same test cannot be requested twice on one encounter.");

        RuleForEach(x => x.Request.Procedures).ChildRules(row =>
        {
            row.RuleFor(x => x.StockItemId).NotEmpty()
                .WithMessage("Select a procedure for every procedure row.");
            row.RuleFor(x => x.Priority).Must(value => value is null || ValidPriorities.Contains(value))
                .WithMessage("Procedure priority must be Routine or Urgent.");
            row.RuleFor(x => x.ClinicalNote).MaximumLength(500);
        });
        RuleFor(x => x.Request.Procedures)
            .Must(items => items is null || items.Select(i => i.StockItemId).Distinct().Count() == items.Count)
            .WithMessage("The same procedure cannot be requested twice on one encounter.");

        RuleFor(x => x.Request)
            .Must(request => !request.Complete
                || (!string.IsNullOrWhiteSpace(request.PresentingComplaint)
                    && !string.IsNullOrWhiteSpace(request.Diagnosis)
                    && request.Disposition is not null))
            .WithMessage("Signing requires a presenting complaint, diagnosis and disposition.");
    }
}
