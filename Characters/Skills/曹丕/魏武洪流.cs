﻿using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Characters.Skills.曹丕;

public class 魏武洪流 : BaseSkill
{
    public 魏武洪流() : base("魏武洪流", ActionType.Counter, DamageType.True, AttackType.Spell)
    {
        BaseEffectiveness = 1.0f;
        CoinEffectiveness = 1.0f;
        AttackLevelModifier = 0;
        Level = 1;
        IsSpecialCounterSkill = true; // 特殊反击技能
        ExtraEffects = "[攻击前]将本技能的基础伤害设置为（防御等级/4，不低于1）\n本技能最终暴击率提升100%，以所有敌方单位为目标\n为同队所有持有增益效果不低于3个的魏国武将施加1级持续2回合的[天恩]\n[攻击后]获得3级[嗣业承祚]强度；使所有目标获得1级[虚弱]，持续2回合";

    }

    public override void CalculateValues(int attackLevel, int defenseLevel, int morale = 0)
    {
        int effectiveLevel = defenseLevel + AttackLevelModifier;
        // 基础点数：防御等级/4，不低于1
        BaseValue = Math.Max(1, effectiveLevel / 4);
        CoinValue = 1;
        CoinCount = 1;
    }

    public override Color GetSkillColor()
    {
        return Color.Purple; // 反击技能使用紫色
    }
}