using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Tasks.Services;

public interface ISkillMatchingService
{
    bool MeetsMinimumLevel(SkillLevel studentLevel, SkillLevel minimumLevel);
    int ToRank(SkillLevel level);
}
