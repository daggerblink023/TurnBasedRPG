using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Characters.Skills.曹仁;

public class 铁壁 : DefendSkill
{
    public 铁壁() : base()
    {
        Name = "铁壁";
        ActionType = ActionType.Defend;
        DamageType = DamageType.Physical;
        AttackType = AttackType.Blunt;
        BaseEffectiveness = 4.0f;
        CoinEffectiveness = 4.0f;
        DefenseLevelModifier = 4;
        Level = 1;
        ExtraEffects = "[回合开始时]获得2级忍耐，持续2回合；本回合造成伤害时若持有护盾，则获得30%伤害提升";
    }
    
    public override void CalculateValues(int attackLevel, int defenseLevel, int morale = 0)
    {
        int effectiveLevel = defenseLevel + DefenseLevelModifier;
        BaseValue = 12 + (int)(effectiveLevel / BaseEffectiveness);
        CoinValue = 6 + (int)(effectiveLevel / CoinEffectiveness);
        CoinCount = 1;
    }
}
