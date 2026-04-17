using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Characters.Skills.曹丕;

public class 御极守成 : BaseSkill
{
    public 御极守成() : base("御极守成", ActionType.Defend, DamageType.Physical, AttackType.Blunt)
    {
        BaseEffectiveness = 4.0f;
        CoinEffectiveness = 8.0f;
        DefenseLevelModifier = 2;
        Level = 1;
        ExtraEffects = "[回合开始时]本回合自身持有护盾时临时提升25%伤害减免，且受到护盾伤害时获得1级[嗣业承祚]强度（每回合至多触发3次）";
    }
    
    public override void CalculateValues(int attackLevel, int defenseLevel, int morale = 0)
    {
        int effectiveLevel = defenseLevel + DefenseLevelModifier;
        BaseValue = 12 + (int)(effectiveLevel / BaseEffectiveness);
        CoinValue = 4 + (int)(effectiveLevel / CoinEffectiveness);
        CoinCount = 2;
    }
}