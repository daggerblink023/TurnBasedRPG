using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Characters.Skills.曹操;

public class 天下归心 : BaseSkill
{
    public 天下归心() : base("天下归心", ActionType.Attack, DamageType.Magic, AttackType.Spell)
    {
        BaseEffectiveness = 4.0f;
        CoinEffectiveness = 8.0f;
        Level = 3;
        ExtraEffects = "[攻击前]若目标持有的减益状态不低于3个，则使此技能的拼点威力提升6\n[命中时]（计算伤害前）消耗敌方全体护盾，消耗值等同于曹操生命上限的（[决断-曹操]强度*2.5）%，护盾不足的会消耗血量；使\"仁心\"状态强度提升，提升值相当于总消耗值的200%\n[攻击后]立刻使用一次特殊反击\"青釭开天\"，每回合至多通过此方式使用1次";
    }

    public override void CalculateValues(int attackLevel, int defenseLevel, int morale = 0)
    {
        int effectiveLevel = attackLevel + AttackLevelModifier;
        BaseValue = 8 + (int)(effectiveLevel / 4);
        CoinValue = 2 + (int)(effectiveLevel / 8);
        CoinCount = 2;
    }
}
