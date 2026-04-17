using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Buffs;
using TurnBasedRPG.Characters;

namespace TurnBasedRPG.Buffs.Buff;

public class 天啸 : BaseBuff
{
    public 天啸(int remainingTurns = 2, int strength = 1) : base("天啸", "（若持有者非魏国武将）暴击伤害抗性提升（20*状态强度）%，速度最小值与速度最大值分�?1\n（若持有者为魏国武将）暴击伤害抗性提升（30*状态强度）%，速度最小值与速度最大值分�?2", remainingTurns, Math.Clamp(strength, 0, 2), false, true)
    {
        IconColor = Color.Teal;
    }
    
    public override void UpdateBuff(Character character)
    {
        // 魏国武将和非魏国武将的效果不�?        if (character.Faction == Faction.�?
        {
            // 魏国武将：暴击伤害抗性提升（30*状态强度）%，速度最小值与最大值分�?2
            character.CritDamageResistance += 0.3f * Strength;
            character.MinSpeedAdjustment += 2;
            character.MaxSpeedAdjustment += 2;
        }
        else
        {
            // 非魏国武将：暴击伤害抗性提升（20*状态强度）%，速度最小值与最大值分�?1
            character.CritDamageResistance += 0.2f * Strength;
            character.MinSpeedAdjustment += 1;
            character.MaxSpeedAdjustment += 1;
        }
    }
    
    public float GetCritDamageResistance(Character character)
    {
        if (character.Faction == Faction.�?
        {
            return 0.3f * Strength;
        }
        else
        {
            return 0.2f * Strength;
        }
    }
}
