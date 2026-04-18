using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Characters.Skills.曹仁;

public class 盾击 : BaseSkill
{
    public 盾击() : base("盾击", ActionType.Attack, DamageType.Physical, AttackType.Blunt)
    {
        BaseEffectiveness = 4.0f;
        CoinEffectiveness = 6.0f;
        AttackLevelModifier = 0;
        Level = 1;
        ExtraEffects = "[命中时]使全队所有魏国武将获得伤害量50%的护盾";
    }
    
    public override void CalculateValues(int attackLevel, int defenseLevel, int morale = 0)
    {
        int effectiveLevel = attackLevel + AttackLevelModifier;
        BaseValue = 7 + (int)(effectiveLevel / BaseEffectiveness);
        CoinValue = 6 + (int)(effectiveLevel / CoinEffectiveness);
        CoinCount = 1;
    }
}
