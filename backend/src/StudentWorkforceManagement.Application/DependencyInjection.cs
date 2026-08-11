using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StudentWorkforceManagement.Application.Common.Behaviors;
using StudentWorkforceManagement.Application.Common.Events;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Common.Services;
using StudentWorkforceManagement.Application.Files.Services;
using StudentWorkforceManagement.Application.RecurringTasks.Services;
using StudentWorkforceManagement.Application.Tasks.Services;

namespace StudentWorkforceManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var applicationAssembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(applicationAssembly));
        services.AddValidatorsFromAssembly(applicationAssembly);

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

        services.TryAddScoped<ICurrentUserService, AnonymousCurrentUserService>();
        services.AddOptions<UploadFilePolicyOptions>();
        services.AddScoped<IUploadFilePolicy, UploadFilePolicy>();
        services.AddScoped<IApplicationEventQueue, ApplicationEventQueue>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<INotificationIntentService, NotificationIntentService>();
        services.AddScoped<ITaskStateMachine, TaskStateMachine>();
        services.AddScoped<ITaskCreationService, TaskCreationService>();
        services.AddScoped<ITaskWorkloadService, TaskWorkloadService>();
        services.AddScoped<ITaskDependencyService, TaskDependencyService>();
        services.AddScoped<IAssignmentRecommendationService, AssignmentRecommendationService>();
        services.AddScoped<ISkillMatchingService, SkillMatchingService>();
        services.AddScoped<IRecurringTaskGenerationService, RecurringTaskGenerationService>();

        return services;
    }
}
