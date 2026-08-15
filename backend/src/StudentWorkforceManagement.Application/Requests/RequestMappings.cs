using StudentWorkforceManagement.Application.Requests.DTOs;
using Announcement = StudentWorkforceManagement.Domain.Entities.Announcement;
using AuditLog = StudentWorkforceManagement.Domain.Entities.AuditLog;
using AvailabilityEntity = StudentWorkforceManagement.Domain.Entities.Availability;
using Category = StudentWorkforceManagement.Domain.Entities.Category;
using CourseSchedule = StudentWorkforceManagement.Domain.Entities.CourseSchedule;
using EmailDelivery = StudentWorkforceManagement.Domain.Entities.EmailDelivery;
using MarketplaceClaim = StudentWorkforceManagement.Domain.Entities.MarketplaceClaim;
using MarketplaceListing = StudentWorkforceManagement.Domain.Entities.MarketplaceListing;
using Notification = StudentWorkforceManagement.Domain.Entities.Notification;
using NotificationPreference = StudentWorkforceManagement.Domain.Entities.NotificationPreference;
using Semester = StudentWorkforceManagement.Domain.Entities.Semester;
using Skill = StudentWorkforceManagement.Domain.Entities.Skill;
using StudentSkill = StudentWorkforceManagement.Domain.Entities.StudentSkill;
using SubmissionVersion = StudentWorkforceManagement.Domain.Entities.SubmissionVersion;
using SystemSetting = StudentWorkforceManagement.Domain.Entities.SystemSetting;
using TaskAssignmentHistory = StudentWorkforceManagement.Domain.Entities.TaskAssignmentHistory;
using TaskChecklistItem = StudentWorkforceManagement.Domain.Entities.TaskChecklistItem;
using TaskComment = StudentWorkforceManagement.Domain.Entities.TaskComment;
using TaskDependency = StudentWorkforceManagement.Domain.Entities.TaskDependency;
using TaskRequest = StudentWorkforceManagement.Domain.Entities.TaskRequest;
using TaskReview = StudentWorkforceManagement.Domain.Entities.TaskReview;
using TaskSubmission = StudentWorkforceManagement.Domain.Entities.TaskSubmission;
using DepartmentFile = StudentWorkforceManagement.Domain.Entities.DepartmentFile;
using Feedback = StudentWorkforceManagement.Domain.Entities.Feedback;
using FileFolder = StudentWorkforceManagement.Domain.Entities.FileFolder;
using Invitation = StudentWorkforceManagement.Domain.Entities.Invitation;
using RecurringTask = StudentWorkforceManagement.Domain.Entities.RecurringTask;
using RefreshToken = StudentWorkforceManagement.Domain.Entities.RefreshToken;
using Role = StudentWorkforceManagement.Domain.Entities.Role;
using Session = StudentWorkforceManagement.Domain.Entities.Session;
using Student = StudentWorkforceManagement.Domain.Entities.Student;
using TaskRequiredSkill = StudentWorkforceManagement.Domain.Entities.TaskRequiredSkill;
using TaskTemplate = StudentWorkforceManagement.Domain.Entities.TaskTemplate;
using User = StudentWorkforceManagement.Domain.Entities.User;

namespace StudentWorkforceManagement.Application.Requests;

internal static class RequestMappings
{
    public static TaskRequestDto ToDto(TaskRequest request)
    {
        return new TaskRequestDto(
            request.Id,
            request.TaskId,
            request.RequestedById,
            request.Type,
            request.Reason,
            request.CurrentDeadline,
            request.RequestedDeadline,
            request.SuggestedStudentId,
            request.Status,
            request.CreatedAt,
            request.ReviewedAt,
            request.ReviewedById,
            request.ReviewerComment,
            request.ConcurrencyToken,
            request.Task?.Title,
            request.RequestedBy is null ? null : $"{request.RequestedBy.FirstName} {request.RequestedBy.LastName}".Trim(),
            request.SuggestedStudent is null ? null : $"{request.SuggestedStudent.FirstName} {request.SuggestedStudent.LastName}".Trim(),
            request.ReviewedBy?.DisplayName);
    }
}
