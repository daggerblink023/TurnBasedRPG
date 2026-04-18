using TurnBasedRPG.Characters;

namespace TurnBasedRPG.Characters.Allies;

public class 示例角色1 : Character
{
    public 示例角色1(int level = 40, int attackLevelModifier = 0, int defenseLevelModifier = 0)
        : base("示例角色1", 69, 4, level, attackLevelModifier, defenseLevelModifier, isAlly: true)
    {
    }
}