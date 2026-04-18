using TurnBasedRPG.Characters;

namespace TurnBasedRPG.Characters.Enemies;

public class 示例敌怪1 : Character
{
    public 示例敌怪1(int level = 35, int attackLevelModifier = 0, int defenseLevelModifier = 0)
        : base("示例敌怪1", 94, 9, level, attackLevelModifier, defenseLevelModifier, isAlly: false)
    {
    }
}