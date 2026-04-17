using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Characters.Skills.夏侯惇;

public class 横斩 : BaseSkill
{
    public bool HasSilenceEnhancement { get; set; } = false;
    
    public 横斩() : base("横斩", ActionType.Attack, DamageType.Physical, AttackType.Slash)
    {
        BaseEffectiveness = 4.0f;
        CoinEffectiveness = 12.0f;
        AttackLevelModifier = 0;
        Level = 1;
        ExtraEffects = "[使用前]若自身带有沉默状态，则本技能的拼点威力提升2，硬币点数提升1;所有硬币造成的伤害变为真实伤害,且临时获得20%伤害提升\n[命中时]使目标获得2级[脆弱]，持续2回合";
    }
    
    public void ApplySilenceEnhancement()
    {
        HasSilenceEnhancement = true;
        CompetingPower += 2;
        CoinValue += 1;
    }
    
    public override void CalculateValues(int attackLevel, int defenseLevel, int morale = 0)
    {
        int effectiveLevel = attackLevel + AttackLevelModifier;
        BaseValue = 7 + (int)(effectiveLevel / BaseEffectiveness);
        CoinValue = 2 + (int)(effectiveLevel / CoinEffectiveness);
        CoinCount = 2;
    }
}
