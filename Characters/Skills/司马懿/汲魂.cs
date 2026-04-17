using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Characters.Skills.司马懿;

public class 汲魂 : BaseSkill
{
    public 汲魂() : base("汲魂", ActionType.Attack, DamageType.Magic, AttackType.Pierce)
    {
        BaseEffectiveness = 4.0f;
        CoinEffectiveness = 12.0f;
        AttackLevelModifier = 0;
        Level = 2;
        ExtraEffects = "[回合开始时]获得1级[韬晦]\n\n[使用前]消耗全队魏国武将当前持有的护盾值的10%，对于当前护盾值不低于50%最大生命的武将提升消耗量至20%，并记录消耗的护盾总值\n\n消耗的护盾总值每达到自身最大生命的5%，使此技能的拼点威力+1\n\n[命中时]额外造成相当于消耗的护盾总值40%的真实伤害，然后为自身添加相当于此额外伤害值150%的护盾";
    }
    
    public override void CalculateValues(int attackLevel, int defenseLevel, int morale = 0)
    {
        int effectiveLevel = attackLevel + AttackLevelModifier;
        BaseValue = 10 + (int)(effectiveLevel / BaseEffectiveness);
        CoinValue = 2 + (int)(effectiveLevel / CoinEffectiveness);
        CoinCount = 2;
    }
}
