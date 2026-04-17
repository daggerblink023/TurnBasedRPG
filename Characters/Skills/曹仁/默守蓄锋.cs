using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Characters.Skills.曹仁;

public class 默守蓄锋 : BaseSkill
{
    public bool CannotTriggerDefense { get; set; } = true;
    
    public 默守蓄锋() : base("默守蓄锋", ActionType.Attack, DamageType.True, AttackType.Pierce)
    {
        BaseEffectiveness = 4.0f;
        CoinEffectiveness = 6.0f;
        DefenseLevelModifier = 2;
        Level = 1;
        CanBeSelected = false; // 无法被主动选择
        ExtraEffects = "[攻击前]此技能不会触发目标的守备技能\n\n[命中时]自身和同队其余魏国武将获得等同于10%最大生命的护盾\n\n[攻击后]额外造成相当于自身剩余护盾值50%的真实伤害";
    }
    
    public override Color GetSkillColor()
    {
        return Color.Purple; // 反击技能使用紫色
    }
    
    public override void CalculateValues(int attackLevel, int defenseLevel, int morale = 0)
    {
        int effectiveLevel = defenseLevel + DefenseLevelModifier;
        BaseValue = 8 + (int)(effectiveLevel / BaseEffectiveness);
        CoinValue = 6 + (int)(effectiveLevel / CoinEffectiveness);
        CoinCount = 1;
    }
}
