using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Characters.Skills.曹丕;

public class 制衡 : BaseSkill
{
    public 制衡() : base("制衡", ActionType.Counter, DamageType.True, AttackType.Spell)
    {
        BaseEffectiveness = 1.0f;
        CoinEffectiveness = 1.0f;
        AttackLevelModifier = 0;
        Level = 1;
        ExtraEffects = "[攻击前]将本技能的基础伤害设置为（防御等级/3，不低于1）\n为同队所有持有增益效果不低于3个的魏国武将施加1级持续3回合的[天恩]\n[攻击后]消耗1级[嗣业承祚]强度";
    }
    
    public override void CalculateValues(int attackLevel, int defenseLevel, int morale = 0)
    {
        int effectiveLevel = attackLevel + AttackLevelModifier;
        BaseValue = 1;
        CoinValue = 1;
        CoinCount = 1;
    }
    
    public override Color GetSkillColor()
    {
        return Color.Purple; // 反击技能使用紫色
    }
}