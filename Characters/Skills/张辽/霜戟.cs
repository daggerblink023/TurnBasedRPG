using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Characters.Skills.张辽;

public class 霜戟 : BaseSkill
{
    public 霜戟() : base("霜戟", ActionType.Attack, DamageType.Physical, AttackType.Slash)
    {
        BaseEffectiveness = 4.0f;
        CoinEffectiveness = 6.0f;
        AttackLevelModifier = 0;
        Level = 1;
        ExtraEffects = "[使用前]若自身的攻击等级不低于目标的防御等级，则使本技能的拼点威力提升4\n[最后一枚硬币命中时]为目标施加1级持续3回合的[怖吓]\n[攻击后]使自身获得1级[奔狼]，每回合至多触发一次";
        CritRate = 1.0f; // 张辽初始暴击率100%
    }
    
    public override void CalculateValues(int attackLevel, int defenseLevel, int morale = 0)
    {
        int effectiveLevel = attackLevel + AttackLevelModifier;
        BaseValue = 8 + (int)(effectiveLevel / 4.0f);
        CoinValue = 2 + (int)(effectiveLevel / 6.0f);
        CoinCount = 1;
    }
}
