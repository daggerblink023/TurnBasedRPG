using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Characters.Skills.司马懿;

public class 狼顾 : BaseSkill
{
    public bool CannotTriggerDefense { get; set; } = true;

    public 狼顾() : base("狼顾", ActionType.Counter, DamageType.True, AttackType.Spell)
	{
		BaseEffectiveness = 4.0f;
		CoinEffectiveness = 6.0f;
		DefenseLevelModifier = 4;
		Level = 1;
		CanBeSelected = false; // 无法被主动选择
		IsSpecialCounterSkill = true; // 特殊反击技能
		ExtraEffects = "[攻击前]消耗自身当前持有的护盾值的50%，并使本技能的基础点数提升（消耗的护盾总值/15）\n此技能的最终暴击率提升50%，若自身的[韬晦]状态强度不低于6则提升100%\n若结算伤害前此技能的最终增伤低于30%，则获得至30%最终增伤\n[命中时]记录造成的实际伤害，并使全队所有魏国武将获得伤害量50%的护盾，此护盾值不超过司马懿最大生命的15%\n[攻击后]根据自身的[韬晦]状态强度，触发以下效果之一：\n[韬晦]强度低于6：获得1级[韬晦]\n[韬晦]强度不低于6：消耗一半强度（剩余强度向下取整），额外扣除目标的当前生命值，扣除值相当于伤害记录值（若目标拥有护盾，则优先扣除护盾值）；若目标已死亡/无法选中，则对可选中的随机敌方单位执行此效果";
	}

	public override Color GetSkillColor()
	{
		return Color.Purple; // 反击技能使用紫色
	}

    public override void CalculateValues(int attackLevel, int defenseLevel, int morale = 0)
    {
        int effectiveLevel = defenseLevel + DefenseLevelModifier;
        // 基础点数：8+防御等级/6（防御等级即effectiveLevel）
        BaseValue = 8 + (effectiveLevel / 6);
        // 硬币点数：2+防御等级/9（防御等级即effectiveLevel）
        CoinValue = 2 + (effectiveLevel / 9);
        CoinCount = 1;
    }
}
