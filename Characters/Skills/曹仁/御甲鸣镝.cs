﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Systems;
using TurnBasedRPG.Characters;

namespace TurnBasedRPG.Characters.Skills.曹仁;

public class 御甲鸣镝 : BaseSkill
{
    public bool HasNoSecondaryTargets { get; set; } = false;
    public bool IsFirstCoin { get; set; } = false;
    public bool IsSecondCoin { get; set; } = false;
    public bool IsLastCoin { get; set; } = false;
    public List<Character> SecondaryTargets { get; set; } = new List<Character>();
    public bool HasShieldEnhancement { get; set; } = false;
    
    public 御甲鸣镝() : base("御甲鸣镝", ActionType.Attack, DamageType.Physical, AttackType.Blunt)
    {
        BaseEffectiveness = 4.0f;
        CoinEffectiveness = 5.0f;
        AttackLevelModifier = 0;
        Level = 3;
        ExtraEffects = "[回合开始时]为曹仁和同队魏国武将添加20%最大生命的护盾\n[攻击前]随机选取主要目标外至多两个敌方单位作为次级目标，本技能的硬币命中时会对次级目标造成50%伤害；若没有可选取的次级目标，则对主要目标造成的伤害获得20%最终伤害提升\n[第一枚硬币命中时]曹仁与同队魏国武将获得等同于5%最大生命的护盾\n[第二枚硬币命中时]曹仁与同队魏国武将获得等同于5%最大生命的护盾\n[最后一枚硬币命中时]曹仁与同队魏国武将获得等同于5%最大生命的护盾\n[攻击后]使主要目标与所有次级目标获得[围城]，持续2回合";
    }
    
    public void SetCoinState(int coinIndex, int totalCoins)
    {
        IsFirstCoin = (coinIndex == 0);
        IsSecondCoin = (coinIndex == 1);
        IsLastCoin = (coinIndex == totalCoins - 1);
    }
    
    public void ApplyShieldEnhancement()
    {
        HasShieldEnhancement = true;
        CompetingPower += 8;
    }
    
    public override void CalculateValues(int attackLevel, int defenseLevel, int morale = 0)
    {
        int effectiveLevel = attackLevel + AttackLevelModifier;
        BaseValue = 11 + (int)(effectiveLevel / BaseEffectiveness);
        CoinValue = 2 + (int)(effectiveLevel / CoinEffectiveness);
        CoinCount = 3;
    }
}
