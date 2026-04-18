﻿using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Characters.Skills.张辽;

public class 破溃 : BaseSkill
{
    public 破溃() : base("破溃", ActionType.Attack, DamageType.Physical, AttackType.Pierce)
    {
        BaseEffectiveness = 4.0f;
        CoinEffectiveness = 12.0f;
        AttackLevelModifier = 0;
        Level = 1;
        ExtraEffects = "[使用前]若自身的攻击等级不低于目标的防御等级，则使本技能的拼点威力提升5\n[攻击后]若自身的[奔狼]强度不低于3，额外为同队所有魏国武将施加1级持续2回合的[天啸]，每回合至多触发一次\n然后使自身获得2级[奔狼]，每回合至多触发一次";
        CritRate = 1.0f; // 张辽初始暴击率100%
    }
    
    public override void CalculateValues(int attackLevel, int defenseLevel, int morale = 0)
    {
        int effectiveLevel = attackLevel + AttackLevelModifier;
        BaseValue = 8 + (int)(effectiveLevel / 4.0f);
        CoinValue = 2 + (int)(effectiveLevel / 12.0f);
        CoinCount = 2;
    }
}
