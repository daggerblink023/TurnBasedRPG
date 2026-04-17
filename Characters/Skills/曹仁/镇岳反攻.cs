using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Characters.Skills.曹仁;

public class 镇岳反攻 : BaseSkill
{
    public bool HasShieldEnhancement { get; set; } = false;
    
    public 镇岳反攻() : base("镇岳反攻", ActionType.Attack, DamageType.Physical, AttackType.Blunt)
    {
        BaseEffectiveness = 4.0f;
        CoinEffectiveness = 12.0f;
        AttackLevelModifier = 0;
        Level = 2;
        ExtraEffects = "[攻击前]若自身剩余护盾值高于25%最大生命，则本技能的拼点威力提升2\n\n[命中时]若目标持有可转移的减益状态，则尝试将目标的最多2个随机减益状态复制后再次施加给目标\n\n[攻击后]额外造成相当于自身剩余护盾值15%的真实伤害；若目标未持有护盾，则真实伤害倍率提升至20%\n\n（可转移的减益状态：非永久持续、非特定武将专属、不具有\"不可驱散\"标签的减益类状态）";
    }
    
    public void ApplyShieldEnhancement()
    {
        HasShieldEnhancement = true;
        CompetingPower += 2;
    }
    
    public override void CalculateValues(int attackLevel, int defenseLevel, int morale = 0)
    {
        int effectiveLevel = attackLevel + AttackLevelModifier;
        BaseValue = 8 + (int)(effectiveLevel / BaseEffectiveness);
        CoinValue = 3 + (int)(effectiveLevel / CoinEffectiveness);
        CoinCount = 2;
    }
}
