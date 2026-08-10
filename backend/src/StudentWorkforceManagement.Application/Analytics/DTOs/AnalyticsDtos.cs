using StudentWorkforceManagement.Domain.Enums;
using TaskStatus = StudentWorkforceManagement.Domain.Enums.TaskStatus;

namespace StudentWorkforceManagement.Application.Analytics.DTOs;

public sealed record DashboardAnalyticsDto(int TotalTasks, int ActiveTasks, int CompletedTasks, int OverdueTasks, int PendingReviews, int PendingRequests);
public sealed record TasksByStatusDto(TaskStatus Status, int Count);
public sealed record TasksByCategoryDto(Guid CategoryId, string CategoryName, int Count);
public sealed record WorkloadDistributionDto(Guid StudentId, string StudentName, int ActiveWorkloadMinutes, int ActiveTaskCount);
public sealed record RequestAnalyticsDto(RequestType Type, RequestStatus Status, int Count);
