using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Buffs;
using TurnBasedRPG.Characters;

namespace TurnBasedRPG.Buffs.Buff;

public class 天恩 : BaseBuff
{
    public 天恩(int remainingTurns, int strength = 1) : base("天恩", "每拥�?状态强度则临时提升1级攻击等级�?级防御等级�?%最终造成伤害增加�?%最终受到伤害减�?, remainingTurns, Math.Clamp(strength, 1, 6))
    {
        IconColor = Color.Yellow;
    }
    
    public override void UpdateBuff(Character character)
    {
        // 每拥�?状态强度则临时提升1级攻击等�?        character.AttackLevelAdjustment += Strength;
        // 每拥�?状态强度则临时提升1级防御等�?        character.DefenseLevelAdjustment += Strength;
        // 每拥�?状态强度则临时提升5%最终造成伤害增加
        character.FinalDamageIncrease += Strength * 0.05f;
        // 每拥�?状态强度则临时提升5%最终受到伤害减�?        character.FinalDamageReduction += Strength * 0.05f;
    }
}
