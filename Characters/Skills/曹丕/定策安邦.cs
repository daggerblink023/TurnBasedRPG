﻿using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Characters.Skills.曹丕;

public class 定策安邦 : BaseSkill
{
    public 定策安邦() : base("定策安邦", ActionType.Attack, DamageType.Magic, AttackType.Slash)
    {
        BaseEffectiveness = 4.0f;
        CoinEffectiveness = 12.0f;
        AttackLevelModifier = 0;
        Level = 1;
        ExtraEffects = "[回合开始时]获得1级[嗣业承祚]强度\n[攻击前]为持有增益效果不低于3个的所有我方魏国武将施加1级持续2回合的[天恩]与相当于生命上限10%的护盾，并回复1点士气值\n若目标持有减益效果不低于3个则额外造成相当于（防御等级/4，不低于1）的真实伤害，此附加伤害暴击率固定为50%\n若附加伤害产生了暴击，额外获得1级[嗣业承祚]";
    }
    
    public override void CalculateValues(int attackLevel, int defenseLevel, int morale = 0)
    {
        int effectiveLevel = attackLevel + AttackLevelModifier;
        BaseValue = 10 + (int)(effectiveLevel / BaseEffectiveness);
        CoinValue = 3 + (int)(effectiveLevel / CoinEffectiveness);
        CoinCount = 2;
    }
}