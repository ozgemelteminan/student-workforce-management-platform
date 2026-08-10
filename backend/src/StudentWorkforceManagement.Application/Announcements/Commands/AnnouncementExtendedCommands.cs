using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Announcements.DTOs;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Announcements.Commands;

public sealed record UpdateAnnouncementCommand(Guid Id, string Title, string Content, DateTimeOffset? ExpiresAt, bool IsPinned) : IRequest<AnnouncementDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}
public sealed record PinAnnouncementCommand(Guid Id) : IRequest<AnnouncementDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}
public sealed record UnpinAnnouncementCommand(Guid Id) : IRequest<AnnouncementDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}

public sealed class UpdateAnnouncementCommandValidator : AbstractValidator<UpdateAnnouncementCommand>
{
    public UpdateAnnouncementCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Title).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Content).NotEmpty().MaximumLength(12000);
    }
}

public sealed class AnnouncementExtendedCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateAnnouncementCommand, AnnouncementDto>, IRequestHandler<PinAnnouncementCommand, AnnouncementDto>, IRequestHandler<UnpinAnnouncementCommand, AnnouncementDto>
{
    public async System.Threading.Tasks.Task<AnnouncementDto> Handle(UpdateAnnouncementCommand request, CancellationToken cancellationToken)
    {
        var announcement = await dbContext.Announcements.SingleOrDefaultAsync(entity => entity.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Announcement", request.Id);
        announcement.Title = request.Title.Trim();
        announcement.Content = request.Content.Trim();
        announcement.ExpiresAt = request.ExpiresAt;
        announcement.IsPinned = request.IsPinned;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(announcement);
    }

    public System.Threading.Tasks.Task<AnnouncementDto> Handle(PinAnnouncementCommand request, CancellationToken cancellationToken) => SetPinnedAsync(request.Id, true, cancellationToken);
    public System.Threading.Tasks.Task<AnnouncementDto> Handle(UnpinAnnouncementCommand request, CancellationToken cancellationToken) => SetPinnedAsync(request.Id, false, cancellationToken);

    private async System.Threading.Tasks.Task<AnnouncementDto> SetPinnedAsync(Guid id, bool isPinned, CancellationToken cancellationToken)
    {
        var announcement = await dbContext.Announcements.SingleOrDefaultAsync(entity => entity.Id == id, cancellationToken)
            ?? throw new NotFoundException("Announcement", id);
        announcement.IsPinned = isPinned;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(announcement);
    }

    private static AnnouncementDto ToDto(StudentWorkforceManagement.Domain.Entities.Announcement announcement) => new(announcement.Id, announcement.Title, announcement.Content, announcement.CreatedById, announcement.ExpiresAt, announcement.IsPinned, announcement.IsPublished, announcement.PublishedAt, announcement.CreatedAt, announcement.UpdatedAt);
}
