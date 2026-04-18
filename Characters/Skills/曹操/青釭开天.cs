﻿using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Characters.Skills.曹操;

public class 青釭开天: BaseSkill
{
    public 青釭开天() : base("青釭开天", ActionType.Counter, DamageType.True, AttackType.Spell)
    {
        BaseEffectiveness = 5.0f;
        CoinEffectiveness = 10.0f;
        Level = 1;
        CanBeSelected = false;
        IsSpecialCounterSkill = true; // 特殊反击技能
        ExtraEffects = "此技能固定对敌方全体造成伤害\n[命中时]复制每个命中目标持有的1个减益效果施加给随机敌方单位，复制每个命中目标持有的1个增益效果施加给随机我方单位；降低所有命中目标1点士气值，恢复所有我方魏国武将1点士气值\n[攻击后]为此技能命中的所有目标施加一层\"罪己诏\"；\n消耗我方全部单位持有的护盾并记录消耗总值，然后将记录值按我方存活单位数量平均分配，为每个我方单位施加等同于分配值的护盾，对魏国武将施加等同于分配值125%的护盾；\n使\"仁心\"状态强度提升，提升值相当于记录值的25%";
    }

    public override void CalculateValues(int attackLevel, int defenseLevel, int morale = 0)
    {
        int effectiveLevel = attackLevel + AttackLevelModifier;
        BaseValue = 6 + (int)(effectiveLevel / 5);
        CoinValue = 3 + (int)(effectiveLevel / 10);
        CoinCount = 1;
    }

    public override Color GetSkillColor()
    {
        return Color.Purple;
    }
}
