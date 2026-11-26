namespace Hms.Application.DTOs;

public sealed record HmoOptionDto(Guid Id, string Code, string Name);
public sealed record DoctorOptionDto(Guid Id, string FullName);
public sealed record HmoEnrollmentOptionDto(Guid Id, Guid HmoCompanyId, string HmoName, string HmoMemberId, string? PlanName, bool IsPrimary);
public sealed record ReferenceOptionDto(Guid Id, string Code, string Name, string? Detail);
