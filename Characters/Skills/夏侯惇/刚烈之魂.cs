using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Characters.Skills.夏侯惇;

public class 刚烈之魂 : DefendSkill
{
    public 刚烈之魂() : base()
    {
        Name = "刚烈之魂";
        ActionType = ActionType.Defend;
        DamageType = DamageType.Physical;
        AttackType = AttackType.Blunt;
        BaseEffectiveness = 4.0f;
        CoinEffectiveness = 4.0f;
        DefenseLevelModifier = 2;
        Level = 1;
        ExtraEffects = "[回合开始时]本回合自身持有护盾时临时提升50%伤害减免，并将受到的护盾伤害以200%比例的真实伤害反弹给伤害来源";
    }
    
    public override void CalculateValues(int attackLevel, int defenseLevel, int morale = 0)
    {
        int effectiveLevel = defenseLevel + DefenseLevelModifier;
        BaseValue = 12 + (int)(effectiveLevel / BaseEffectiveness);
        CoinValue = 6 + (int)(effectiveLevel / CoinEffectiveness);
        CoinCount = 1;
    }
}
