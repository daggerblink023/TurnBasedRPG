using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Characters.Skills.曹丕;

public class 受禅代汉 : BaseSkill
{
    public 受禅代汉() : base("受禅代汉", ActionType.Attack, DamageType.True, AttackType.Slash)
    {
        BaseEffectiveness = 4.0f;
        CoinEffectiveness = 6.0f;
        AttackLevelModifier = 0;
        Level = 1;
        ExtraEffects = "[回合开始时]获得1级[嗣业承祚]强度\n[使用前]使基础伤害提升（[嗣业承祚]的状态强度*10）%\n使本技能的拼点威力提升4\n若自身的[嗣业承祚]强度高于3级，则高出的每级强度使本技能的硬币点数提升1\n[攻击前]为同队所有持有增益效果不低于3个的魏国武将施加1级持续3回合的[天恩]\n[攻击后]为全队魏国武将施加护盾，护盾值相当于本技能总伤害量的50%\n清零[嗣业承祚]的状态强度，然后将强度设置为3级";
    }
    
    public override void CalculateValues(int attackLevel, int defenseLevel, int morale = 0)
    {
        int effectiveLevel = attackLevel + AttackLevelModifier;
        BaseValue = 6 + (int)(effectiveLevel / BaseEffectiveness);
        CoinValue = 3 + (int)(effectiveLevel / CoinEffectiveness);
        CoinCount = 2;
    }
}