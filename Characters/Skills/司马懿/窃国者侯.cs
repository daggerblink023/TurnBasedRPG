using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Characters.Skills.司马懿;

public class 窃国者侯 : BaseSkill
{
    public 窃国者侯() : base("窃国者侯", ActionType.Attack, DamageType.True, AttackType.Spell)
    {
        BaseEffectiveness = 4.0f;
        CoinEffectiveness = 6.0f;
        AttackLevelModifier = 0;
        Level = 3;
        ExtraEffects = "[回合开始时]获得3级[韬晦]\n[使用前]若自身带有的护盾值不低于最大生命的33%，则本技能的拼点威力提升4；\n随机选取主要目标外至多两个敌方单位作为次级目标，本技能的硬币命中时会对次级目标造成75%伤害\n[命中时]若主要目标与次级目标持有可转移的增益状态，则尝试将每个目标的最多一个随机增益状态转移给自身\n[攻击后]使所有目标获得2级[虚弱]，持续2回合\n（可转移的增益状态：非永久持续、非特定武将专属、不具有\"不可驱散\"标签的增益类状态）";
    }
    
    public override void CalculateValues(int attackLevel, int defenseLevel, int morale = 0)
    {
        int effectiveLevel = attackLevel + AttackLevelModifier;
        BaseValue = 6 + (int)(effectiveLevel / BaseEffectiveness);
        CoinValue = 3 + (int)(effectiveLevel / CoinEffectiveness);
        CoinCount = 2;
    }
}