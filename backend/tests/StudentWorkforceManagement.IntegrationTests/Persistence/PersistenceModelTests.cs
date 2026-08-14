using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Entities;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Domain.ValueObjects;
using StudentWorkforceManagement.Infrastructure.Persistence;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;

namespace StudentWorkforceManagement.IntegrationTests.Persistence;

public sealed class PersistenceModelTests
{
    [Fact]
    public void Model_contains_canonical_domain_entities()
    {
        using var context = CreateContext();

        Assert.Contains(context.Model.GetEntityTypes(), entity => entity.ClrType == typeof(TaskAssignmentHistory));
        Assert.Contains(context.Model.GetEntityTypes(), entity => entity.ClrType == typeof(TaskRequest));
        Assert.Contains(context.Model.GetEntityTypes(), entity => entity.ClrType == typeof(SubmissionVersion));
        Assert.Contains(context.Model.GetEntityTypes(), entity => entity.ClrType == typeof(User));
        Assert.Contains(context.Model.GetEntityTypes(), entity => entity.ClrType == typeof(Student));
        Assert.Contains(context.Model.GetEntityTypes(), entity => entity.ClrType == typeof(Role));
        Assert.Contains(context.Model.GetEntityTypes(), entity => entity.ClrType == typeof(DomainTask));
        Assert.Contains(context.Model.GetEntityTypes(), entity => entity.ClrType == typeof(TaskRequiredSkill));
    }

    [Fact]
    public void Critical_uniqueness_constraints_are_configured()
    {
        using var context = CreateContext();

        AssertHasUniqueIndex(context, typeof(User), nameof(User.Email));
        AssertHasUniqueIndex(context, typeof(Role), nameof(Role.Name));
        AssertHasUniqueIndex(context, typeof(Student), nameof(Student.Email));
        AssertHasUniqueIndex(context, typeof(StudentSkill), nameof(StudentSkill.StudentId), nameof(StudentSkill.SkillId));
        AssertHasUniqueIndex(context, typeof(TaskAssignmentHistory), nameof(TaskAssignmentHistory.TaskId), nameof(TaskAssignmentHistory.StudentId));
        AssertHasUniqueIndex(context, typeof(TaskRequiredSkill), nameof(TaskRequiredSkill.TaskId), nameof(TaskRequiredSkill.SkillId));
        AssertHasUniqueIndex(context, typeof(TaskDependency), nameof(TaskDependency.TaskId), nameof(TaskDependency.DependsOnTaskId));
        AssertHasUniqueIndex(context, typeof(TaskRequest), nameof(TaskRequest.TaskId), nameof(TaskRequest.Type));
        AssertHasUniqueIndex(context, typeof(SubmissionVersion), nameof(SubmissionVersion.TaskSubmissionId), nameof(SubmissionVersion.VersionNumber));
        AssertHasUniqueIndex(context, typeof(NotificationPreference), nameof(NotificationPreference.UserId), nameof(NotificationPreference.PreferenceType), nameof(NotificationPreference.Channel));
        AssertHasUniqueIndex(context, typeof(EmailDelivery), nameof(EmailDelivery.IdempotencyKey));
        AssertHasUniqueIndex(context, typeof(SystemSetting), nameof(SystemSetting.Key));
        AssertHasUniqueIndex(context, typeof(PasswordResetToken), nameof(PasswordResetToken.TokenHash));
    }

    [Fact]
    public void Business_enums_are_stored_as_strings()
    {
        using var context = CreateContext();

        AssertEnumStoredAsString<DomainTask>(context, nameof(DomainTask.Priority));
        AssertEnumStoredAsString<DomainTask>(context, nameof(DomainTask.Difficulty));
        AssertEnumStoredAsString<DomainTask>(context, nameof(DomainTask.Status));
        AssertEnumStoredAsString<TaskAssignmentHistory>(context, nameof(TaskAssignmentHistory.Status));
        AssertEnumStoredAsString<TaskAssignmentHistory>(context, nameof(TaskAssignmentHistory.Mode));
        AssertEnumStoredAsString<TaskRequest>(context, nameof(TaskRequest.Type));
        AssertEnumStoredAsString<TaskRequest>(context, nameof(TaskRequest.Status));
        AssertEnumStoredAsString<SubmissionVersion>(context, nameof(SubmissionVersion.FileStatus));
        AssertEnumStoredAsString<Notification>(context, nameof(Notification.Type));
        AssertEnumStoredAsString<NotificationPreference>(context, nameof(NotificationPreference.PreferenceType));
        AssertEnumStoredAsString<NotificationPreference>(context, nameof(NotificationPreference.Channel));
        AssertEnumStoredAsString<StudentSkill>(context, nameof(StudentSkill.Level));
    }

    [Fact]
    public void Concurrency_entities_use_guid_concurrency_tokens()
    {
        using var context = CreateContext();

        AssertConcurrencyToken<Student>(context);
        AssertConcurrencyToken<DomainTask>(context);
        AssertConcurrencyToken<TaskAssignmentHistory>(context);
        AssertConcurrencyToken<TaskRequest>(context);
        AssertConcurrencyToken<TaskSubmission>(context);
        AssertConcurrencyToken<MarketplaceListing>(context);
        AssertConcurrencyToken<MarketplaceClaim>(context);
        AssertConcurrencyToken<User>(context);
        AssertConcurrencyToken<Semester>(context);
        AssertConcurrencyToken<Availability>(context);
        AssertConcurrencyToken<RecurringTask>(context);
        AssertConcurrencyToken<SystemSetting>(context);
    }


    [Fact]
    public void Auth_persistence_stores_hashes_without_exposing_security_secrets()
    {
        using var context = CreateContext();
        var userPasswordHash = GetProperty<User>(context, nameof(User.PasswordHash));
        var resetTokenHash = GetProperty<PasswordResetToken>(context, nameof(PasswordResetToken.TokenHash));

        Assert.Equal(typeof(string), userPasswordHash.ClrType);
        Assert.Equal(1024, userPasswordHash.GetMaxLength());
        Assert.Equal(typeof(string), resetTokenHash.ClrType);
        Assert.Equal(512, resetTokenHash.GetMaxLength());
        Assert.DoesNotContain(typeof(StudentWorkforceManagement.Application.Auth.DTOs.InvitationDto).Assembly.GetTypes()
            .Where(type => type.Namespace?.StartsWith("StudentWorkforceManagement.Application.Auth.DTOs", StringComparison.Ordinal) == true)
            .SelectMany(type => type.GetProperties()), property => property.Name.Contains("PasswordHash", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void File_metadata_is_owned_and_schema_does_not_store_binary_payloads()
    {
        using var context = CreateContext();

        Assert.DoesNotContain(context.Model.GetEntityTypes().SelectMany(entity => entity.GetProperties()), property => property.ClrType == typeof(byte[]));
        Assert.All(context.Model.GetEntityTypes().Where(entity => entity.ClrType == typeof(FileMetadata)), entity => Assert.True(entity.IsOwned()));
    }



    [Fact]
    public void Soft_delete_filters_are_configured_for_lifecycle_entities()
    {
        using var context = CreateContext();

        AssertHasQueryFilter<Student>(context);
        AssertHasQueryFilter<DomainTask>(context);
        AssertHasQueryFilter<TaskComment>(context);
        AssertHasQueryFilter<TaskSubmission>(context);
        AssertHasQueryFilter<FileFolder>(context);
        AssertHasQueryFilter<DepartmentFile>(context);
        AssertHasQueryFilter<Announcement>(context);
    }

    [Fact]
    public void Historical_relationships_use_restrict_delete_behavior()
    {
        using var context = CreateContext();

        AssertRestrictDelete<TaskAssignmentHistory>(context, nameof(TaskAssignmentHistory.TaskId));
        AssertRestrictDelete<TaskRequest>(context, nameof(TaskRequest.TaskId));
        AssertRestrictDelete<TaskSubmission>(context, nameof(TaskSubmission.TaskId));
        AssertRestrictDelete<SubmissionVersion>(context, nameof(SubmissionVersion.TaskSubmissionId));
        AssertRestrictDelete<TaskComment>(context, nameof(TaskComment.TaskId));
        AssertRestrictDelete<TaskChecklistItem>(context, nameof(TaskChecklistItem.TaskId));
        AssertRestrictDelete<MarketplaceClaim>(context, nameof(MarketplaceClaim.MarketplaceListingId));
        AssertRestrictDelete<AuditLog>(context, nameof(AuditLog.UserId));
        AssertRestrictDelete<PasswordResetToken>(context, nameof(PasswordResetToken.UserId));
    }

    [Fact]
    public void Check_constraints_cover_key_range_and_consistency_rules()
    {
        using var context = CreateContext();
        var model = context.GetService<IDesignTimeModel>().Model;

        AssertHasCheckConstraint<DomainTask>(model, "CK_Tasks_EstimatedDurationMinutes");
        AssertHasCheckConstraint<TaskDependency>(model, "CK_TaskDependencies_NoSelfDependency");
        AssertHasCheckConstraint<SubmissionVersion>(model, "CK_SubmissionVersions_VersionNumber");
        AssertHasCheckConstraint<Feedback>(model, "CK_Feedback_Rating");
        AssertHasCheckConstraint<CourseSchedule>(model, "CK_CourseSchedules_TimeRange");
        AssertHasCheckConstraint<Availability>(model, "CK_Availability_TimeRange");
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=student_workforce_management;Username=postgres;Password=postgres")
            .Options;

        return new ApplicationDbContext(options);
    }

    private static void AssertHasUniqueIndex(ApplicationDbContext context, Type entityClrType, params string[] propertyNames)
    {
        var entity = Assert.Single(context.Model.GetEntityTypes(), entityType => entityType.ClrType == entityClrType);

        Assert.Contains(entity.GetIndexes(), index =>
            index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual(propertyNames));
    }

    private static void AssertEnumStoredAsString<TEntity>(ApplicationDbContext context, string propertyName)
    {
        var property = GetProperty<TEntity>(context, propertyName);
        var providerClrType = property.GetValueConverter()?.ProviderClrType ?? property.GetTypeMapping().Converter?.ProviderClrType;

        Assert.True(
            providerClrType == typeof(string) || property.GetColumnType()?.StartsWith("character varying", StringComparison.Ordinal) == true,
            $"{typeof(TEntity).Name}.{propertyName} should be stored as a string column.");
    }

    private static void AssertConcurrencyToken<TEntity>(ApplicationDbContext context)
    {
        var property = GetProperty<TEntity>(context, nameof(IHasConcurrencyToken.ConcurrencyToken));

        Assert.True(property.IsConcurrencyToken);
        Assert.Equal(typeof(Guid), property.ClrType);
    }



    private static void AssertHasQueryFilter<TEntity>(ApplicationDbContext context)
    {
        var entity = Assert.Single(context.Model.GetEntityTypes(), entityType => entityType.ClrType == typeof(TEntity));

        Assert.NotNull(entity.GetQueryFilter());
    }

    private static void AssertRestrictDelete<TEntity>(ApplicationDbContext context, string foreignKeyPropertyName)
    {
        var entity = Assert.Single(context.Model.GetEntityTypes(), entityType => entityType.ClrType == typeof(TEntity));

        Assert.Contains(entity.GetForeignKeys(), foreignKey =>
            foreignKey.DeleteBehavior == DeleteBehavior.Restrict &&
            foreignKey.Properties.Any(property => property.Name == foreignKeyPropertyName));
    }

    private static void AssertHasCheckConstraint<TEntity>(IModel model, string constraintName)
    {
        var entity = Assert.Single(model.GetEntityTypes(), entityType => entityType.ClrType == typeof(TEntity));

        Assert.Contains(entity.GetCheckConstraints(), constraint => constraint.Name == constraintName);
    }

    private static IProperty GetProperty<TEntity>(ApplicationDbContext context, string propertyName)
    {
        var entity = Assert.Single(context.Model.GetEntityTypes(), entityType => entityType.ClrType == typeof(TEntity));
        var property = entity.FindProperty(propertyName);

        Assert.NotNull(property);
        return property;
    }
}
