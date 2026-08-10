using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Feedback.DTOs;
using StudentWorkforceManagement.Domain.Entities;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Feedback.Commands;

public sealed record CreateFeedbackCommand(Guid TaskId, Guid StudentId, int? Rating, string? Comment) : IRequest<FeedbackDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}

public sealed class CreateFeedbackCommandValidator : AbstractValidator<CreateFeedbackCommand>
{
    public CreateFeedbackCommandValidator()
    {
        RuleFor(command => command.TaskId).NotEmpty();
        RuleFor(command => command.StudentId).NotEmpty();
        RuleFor(command => command.Rating).InclusiveBetween(1, 5).When(command => command.Rating.HasValue);
        RuleFor(command => command.Comment).MaximumLength(4000);
        RuleFor(command => command).Must(command => command.Rating.HasValue || !string.IsNullOrWhiteSpace(command.Comment)).WithMessage("Feedback requires a rating or comment.");
    }
}

public sealed class FeedbackCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser) : IRequestHandler<CreateFeedbackCommand, FeedbackDto>
{
    public async System.Threading.Tasks.Task<FeedbackDto> Handle(CreateFeedbackCommand request, CancellationToken cancellationToken)
    {
        var taskExists = await dbContext.Tasks.AnyAsync(task => task.Id == request.TaskId, cancellationToken);
        if (!taskExists)
        {
            throw new NotFoundException("Task", request.TaskId);
        }
        var studentExists = await dbContext.Students.AnyAsync(student => student.Id == request.StudentId, cancellationToken);
        if (!studentExists)
        {
            throw new NotFoundException("Student", request.StudentId);
        }
        var feedback = new StudentWorkforceManagement.Domain.Entities.Feedback
        {
            Id = Guid.NewGuid(),
            TaskId = request.TaskId,
            StudentId = request.StudentId,
            CreatedById = currentUser.RequireUserId(),
            Rating = request.Rating,
            Comment = request.Comment?.Trim()
        };
        dbContext.Feedback.Add(feedback);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(feedback);
    }

    private static FeedbackDto ToDto(StudentWorkforceManagement.Domain.Entities.Feedback feedback) => new(feedback.Id, feedback.TaskId, feedback.StudentId, feedback.CreatedById, feedback.Rating, feedback.Comment, feedback.CreatedAt);
}
