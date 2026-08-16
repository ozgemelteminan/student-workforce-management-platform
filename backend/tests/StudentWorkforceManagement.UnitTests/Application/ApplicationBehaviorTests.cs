using FluentValidation;
using MediatR;
using StudentWorkforceManagement.Application.Common.Behaviors;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Tasks.Commands.CreateTask;
using StudentWorkforceManagement.Application.Tasks.Services;
using StudentWorkforceManagement.Domain.Enums;
using TaskStatus = StudentWorkforceManagement.Domain.Enums.TaskStatus;

namespace StudentWorkforceManagement.UnitTests.Application;

public sealed class ApplicationBehaviorTests
{
    [Fact]
    public void Task_state_machine_blocks_student_completion()
    {
        var stateMachine = new TaskStateMachine();

        Assert.Throws<ForbiddenException>(() => stateMachine.ValidateTransition(TaskStatus.SUBMITTED_FOR_REVIEW, TaskStatus.COMPLETED, [UserRole.STUDENT], isAssignedStudent: true));
    }

    [Fact]
    public void Task_state_machine_allows_reviewer_approval_but_not_task_manager_by_default()
    {
        var stateMachine = new TaskStateMachine();

        stateMachine.ValidateTransition(TaskStatus.SUBMITTED_FOR_REVIEW, TaskStatus.COMPLETED, [UserRole.REVIEWER], isAssignedStudent: false);
        Assert.Throws<ForbiddenException>(() => stateMachine.ValidateTransition(TaskStatus.SUBMITTED_FOR_REVIEW, TaskStatus.COMPLETED, [UserRole.TASK_MANAGER], isAssignedStudent: false));
    }

    [Fact]
    public void Task_state_machine_allows_reviewer_revision_request_but_not_task_manager_by_default()
    {
        var stateMachine = new TaskStateMachine();

        stateMachine.ValidateTransition(TaskStatus.SUBMITTED_FOR_REVIEW, TaskStatus.IN_PROGRESS, [UserRole.REVIEWER], isAssignedStudent: false);
        Assert.Throws<ForbiddenException>(() => stateMachine.ValidateTransition(TaskStatus.SUBMITTED_FOR_REVIEW, TaskStatus.IN_PROGRESS, [UserRole.TASK_MANAGER], isAssignedStudent: false));
    }

    [Fact]
    public void Skill_matching_respects_minimum_level_order()
    {
        var service = new SkillMatchingService();

        Assert.True(service.MeetsMinimumLevel(SkillLevel.ADVANCED, SkillLevel.INTERMEDIATE));
        Assert.False(service.MeetsMinimumLevel(SkillLevel.BEGINNER, SkillLevel.EXPERT));
    }

    [Fact]
    public async System.Threading.Tasks.Task Authorization_behavior_blocks_forbidden_roles()
    {
        var behavior = new AuthorizationBehavior<ReviewerOnlyRequest, Unit>(new FakeCurrentUser(UserRole.TASK_MANAGER));

        await Assert.ThrowsAsync<ForbiddenException>(() => behavior.Handle(new ReviewerOnlyRequest(), () => System.Threading.Tasks.Task.FromResult(Unit.Value), CancellationToken.None));
    }

    [Fact]
    public async System.Threading.Tasks.Task Validation_behavior_throws_application_validation_exception()
    {
        var behavior = new ValidationBehavior<CreateTaskCommand, StudentWorkforceManagement.Application.Tasks.DTOs.TaskDto>([new CreateTaskCommandValidator()]);
        var command = new CreateTaskCommand("", null, Guid.Empty, null, TaskPriority.MEDIUM, TaskDifficulty.EASY, null, DateTimeOffset.UtcNow, 0);

        await Assert.ThrowsAsync<ApplicationValidationException>(() => behavior.Handle(command, () => throw new InvalidOperationException("Handler should not execute."), CancellationToken.None));
    }

    private sealed record ReviewerOnlyRequest : IRequest<Unit>, IAuthorizableRequest
    {
        public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.Reviewers;
    }

    private sealed class FakeCurrentUser(params UserRole[] roles) : ICurrentUserService
    {
        public Guid? UserId { get; } = Guid.NewGuid();
        public Guid? StudentId { get; } = Guid.NewGuid();
        public bool IsAuthenticated => true;
        public IReadOnlyCollection<UserRole> Roles { get; } = roles;
    }
}
