using MediatR;

namespace StudentWorkforceManagement.Application.Common.Events;

public sealed record TaskAssignedEvent(Guid TaskId, Guid StudentId, Guid AssignedByUserId) : INotification;
public sealed record TaskReassignedEvent(Guid TaskId, Guid PreviousStudentId, Guid NewStudentId, Guid ReassignedByUserId) : INotification;
public sealed record TaskRequestReviewedEvent(Guid TaskRequestId, Guid TaskId, bool Approved, Guid ReviewedByUserId) : INotification;
public sealed record SubmissionReviewedEvent(Guid TaskId, Guid SubmissionId, bool Approved, Guid ReviewedByUserId) : INotification;
public sealed record MarketplaceClaimAcceptedEvent(Guid ListingId, Guid ClaimId, Guid StudentId, Guid ApprovedByUserId) : INotification;
