﻿using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Characters.Skills.张辽;

public class 威震逍遥津: BaseSkill
{
    public 威震逍遥津() : base("威震逍遥津", ActionType.Attack, DamageType.Physical, AttackType.Pierce)
    {
        BaseEffectiveness = 2.0f;
        CoinEffectiveness = 6.0f;
        AttackLevelModifier = 0;
        Level = 1;
        ExtraEffects = "整场战斗只能使用一次本技能；使用后，尝试选取技能3会改为使用技能2（触发[决断-张辽]的额外弃牌时仍然视为技能3）\n如果没有使用过本技能，在受到致死伤害时时会将血量回退至受到该次伤害前，清除自身所有减益状态，然后立刻使用本技能并触发[回合开始时]相关效果；若以这种方式释放本技能，会在[神威]结束后立刻死亡\n[回合开始时]使自身获得持续2回合的[神威]，然后立刻触发本技能的[使用时]效果\n[使用时]额外对敌方所有单位造成一次基于最终点数的真实伤害，自身每损失1%生命，此伤害临时获得1%最终伤害提升";
        CritRate = 1.0f; // 张辽初始暴击率100%
    }
    
    public override void CalculateValues(int attackLevel, int defenseLevel, int morale = 0)
    {
        int effectiveLevel = attackLevel + AttackLevelModifier;
        BaseValue = 30 + (int)(effectiveLevel / BaseEffectiveness);
        CoinValue = 1;
        CoinCount = 1;
    }
}
