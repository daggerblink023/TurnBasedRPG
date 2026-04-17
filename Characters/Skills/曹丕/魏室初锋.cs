using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Characters.Skills.曹丕;

public class 魏室初锋 : BaseSkill
{
    public 魏室初锋() : base("魏室初锋", ActionType.Attack, DamageType.Physical, AttackType.Slash)
    {
        BaseEffectiveness = 4.0f;
        CoinEffectiveness = 6.0f;
        AttackLevelModifier = 0;
        Level = 1;
        ExtraEffects = "[回合开始时]获得1级[嗣业承祚]\n[攻击后]额外造成相当于（防御等级/6，不低于1）的真实伤害，此附加伤害暴击率固定为50%\n若附加伤害产生了暴击，额外获得1级[嗣业承祚]";
    }
    
    public override void CalculateValues(int attackLevel, int defenseLevel, int morale = 0)
    {
        int effectiveLevel = attackLevel + AttackLevelModifier;
        BaseValue = 7 + (int)(effectiveLevel / BaseEffectiveness);
        CoinValue = 6 + (int)(effectiveLevel / CoinEffectiveness);
        CoinCount = 1;
    }
}