using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Characters.Skills.夏侯惇;

public class 铁壁战吼 : BaseSkill
{
    public bool HasShieldEnhancement { get; set; } = false;
    public bool IsLastCoin { get; set; } = false;
    
    public 铁壁战吼() : base("铁壁战吼", ActionType.Attack, DamageType.True, AttackType.Blunt)
    {
        BaseEffectiveness = 4.0f;
        CoinEffectiveness = 8.0f;
        AttackLevelModifier = 0;
        Level = 3;
        ExtraEffects = "[回合开始时]使自身获得4级忍耐与4级防御等级提升，持续1回合，每回合最多触发一次\n[使用前]若自身带有的护盾值不低于最大生命的33%，则本技能的拼点威力提升2\n[最后一枚硬币命中时]额外对敌方的随机目标造成相当于本硬币对主要目标伤害量20%的真实伤害，并使自身获得等同于该次额外伤害200%的护盾值；使主要目标与附加伤害命中的目标获得3级[虚弱]，持续2回合\n[攻击结束后]使自身获得沉默，持续2回合，每回合最多触发一次";
    }
    
    public void ApplyShieldEnhancement()
    {
        HasShieldEnhancement = true;
        CompetingPower += 3;
        BaseValue += 2;
    }
    
    public void SetAsLastCoin(bool isLast)
    {
        IsLastCoin = isLast;
    }
    
    public override void CalculateValues(int attackLevel, int defenseLevel, int morale = 0)
    {
        int effectiveLevel = attackLevel + AttackLevelModifier;
        BaseValue = 11 + (int)(effectiveLevel / BaseEffectiveness);
        CoinValue = 3 + (int)(effectiveLevel / CoinEffectiveness);
        CoinCount = 2;
    }
}
