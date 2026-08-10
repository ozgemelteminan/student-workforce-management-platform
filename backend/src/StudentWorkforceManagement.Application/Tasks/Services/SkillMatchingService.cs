using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Tasks.Services;

public sealed class SkillMatchingService : ISkillMatchingService
{
    public bool MeetsMinimumLevel(SkillLevel studentLevel, SkillLevel minimumLevel) => ToRank(studentLevel) >= ToRank(minimumLevel);

    public int ToRank(SkillLevel level)
    {
        return level switch
        {
            SkillLevel.BEGINNER => 1,
            SkillLevel.INTERMEDIATE => 2,
            SkillLevel.ADVANCED => 3,
            SkillLevel.EXPERT => 4,
            _ => 0
        };
    }
}
