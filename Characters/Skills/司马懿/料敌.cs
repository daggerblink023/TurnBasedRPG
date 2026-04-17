using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Characters.Skills.司马懿;

public class 料敌 : DefendSkill
{
    public 料敌() : base()
    {
        Name = "料敌";
        ActionType = ActionType.Defend;
        DamageType = DamageType.Physical;
        AttackType = AttackType.Blunt;
        BaseEffectiveness = 4.0f;
        CoinEffectiveness = 8.0f;
        DefenseLevelModifier = 4;
        Level = 1;
        ExtraEffects = "本回合自身持有护盾时临时提升50%伤害减免";
    }
    
    public override void CalculateValues(int attackLevel, int defenseLevel, int morale = 0)
    {
        int effectiveLevel = defenseLevel + DefenseLevelModifier;
        BaseValue = 12 + (int)(effectiveLevel / BaseEffectiveness);
        CoinValue = 4 + (int)(effectiveLevel / CoinEffectiveness);
        CoinCount = 2;
    }
}
