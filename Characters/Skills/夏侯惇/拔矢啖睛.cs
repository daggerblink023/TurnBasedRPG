using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Characters.Skills.夏侯惇;

public class 拔矢啖睛 : BaseSkill
{
    public bool IsLastCoin { get; set; } = false;
    public int EnduranceStrength { get; set; } = 0;
    
    public 拔矢啖睛() : base("拔矢啖睛", ActionType.Attack, DamageType.Physical, AttackType.Slash)
    {
        BaseEffectiveness = 4.0f;
        CoinEffectiveness = 18.0f;
        AttackLevelModifier = 0;
        Level = 2;
        ExtraEffects = "[回合开始时]使同队所有魏国武将获得2级忍耐，持续2回合，每回合最多触发一次\n[最后一枚硬币使用时]若目标的防御等级低于自身，则本硬币造成真实伤害且临时获得（自身忍耐效果强度x10）%伤害提升";
    }
    
    public void SetAsLastCoin(bool isLast)
    {
        IsLastCoin = isLast;
    }
    
    public void SetEnduranceStrength(int strength)
    {
        EnduranceStrength = strength;
    }
    
    public override void CalculateValues(int attackLevel, int defenseLevel, int morale = 0)
    {
        int effectiveLevel = attackLevel + AttackLevelModifier;
        BaseValue = 6 + (int)(effectiveLevel / BaseEffectiveness);
        CoinValue = 3 + (int)(effectiveLevel / CoinEffectiveness);
        CoinCount = 3;
    }
}
