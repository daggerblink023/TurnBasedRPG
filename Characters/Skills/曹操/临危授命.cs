﻿using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Characters.Skills.曹操;

public class 临危授命 : DefendSkill
{
    public 临危授命() : base()
    {
        Name = "临危授命";
        ActionType = ActionType.Defend;
        DamageType = DamageType.Physical;
        AttackType = AttackType.Blunt;
        BaseEffectiveness = 4.0f;
        CoinEffectiveness = 6.0f;
        DefenseLevelModifier = 2;
        Level = 1;
        ExtraEffects = "[回合开始时]使当前护盾最少的1名我方魏国武将的护盾值增加，增加值相当于此技能为曹操自己施加的护盾值的50%；\n若不存在满足要求的我方目标，则使此技能为自己施加的护盾值提升50%；\n使\"仁心\"状态强度提升，提升值相当于此技能施加的总护盾值的50%";
    }

    public override void CalculateValues(int attackLevel, int defenseLevel, int morale = 0)
    {
        int effectiveLevel = defenseLevel + DefenseLevelModifier;
        BaseValue = 16 + (int)(effectiveLevel / 4);
        CoinValue = 4 + (int)(effectiveLevel / 6);
        CoinCount = 1;
    }
}
