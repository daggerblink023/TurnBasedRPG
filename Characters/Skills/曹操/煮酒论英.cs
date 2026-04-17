using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Characters.Skills.曹操;

public class 煮酒论英 : BaseSkill
{
    public 煮酒论英() : base("煮酒论英", ActionType.Attack, DamageType.Magic, AttackType.Slash)
    {
        BaseEffectiveness = 4.0f;
        CoinEffectiveness = 6.0f;
        Level = 1;
        ExtraEffects = "[攻击前]若目标持有的减益状态不低于3个，则使此技能的拼点威力提升4\n[攻击后]为目标施加1级1层\"罪己诏\"；使\"仁心\"状态强度提升，提升值相当于此技能总伤害的100%";
    }

    public override void CalculateValues(int attackLevel, int defenseLevel, int morale = 0)
    {
        int effectiveLevel = attackLevel + AttackLevelModifier;
        BaseValue = 8 + (int)(effectiveLevel / 4);
        CoinValue = 4 + (int)(effectiveLevel / 6);
        CoinCount = 1;
    }
}
