﻿using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Characters.Skills.张辽;

public class 疾行 : BaseSkill
{
    public 疾行() : base("疾行", ActionType.Dodge, DamageType.Physical, AttackType.Pierce)
    {
        BaseEffectiveness = 4.0f;
        CoinEffectiveness = 6.0f;
        AttackLevelModifier = 0;
        Level = 1;
        ExtraEffects = "[使用时]自身的攻击等级每高于目标10级获得1基础点数\n[闪避成功时]本回合内使自身临时获得10%暴击率，每回合至多触发2次";
        CritRate = 1.0f; // 张辽初始暴击率100%
    }
    
    public override void CalculateValues(int attackLevel, int defenseLevel, int morale = 0)
    {
        int effectiveLevel = attackLevel + AttackLevelModifier;
        BaseValue = 8 + (int)(effectiveLevel / BaseEffectiveness);
        CoinValue = 7 + (int)(effectiveLevel / CoinEffectiveness);
        CoinCount = 1;
    }
}
