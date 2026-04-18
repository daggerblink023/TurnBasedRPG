using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Buffs;
using TurnBasedRPG.Characters;

namespace TurnBasedRPG.Buffs.Buff;

public class 韬晦 : BaseBuff
{
    public 韬晦(int? remainingTurns = null, int strength = 0) : base("韬晦", "获得（10-[韬晦]强度）*3%的最终受到伤害减免\n\n获得（1+[韬晦]强度）*3%的最终造成伤害增加", remainingTurns, Math.Clamp(strength, 0, 9), true, true)
    {
        IconColor = Color.Purple;
    }
    
    public override void UpdateBuff(Character character)
    {
        // 获得（10-[韬晦]强度）*3%的最终受到伤害减免
        character.FinalDamageReduction += (10 - Strength) * 0.03f;
        // 获得（1+[韬晦]强度）*3%的最终造成伤害增加
        character.FinalDamageIncrease += (1 + Strength) * 0.03f;
    }
}
