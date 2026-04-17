using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Buffs;
using TurnBasedRPG.Characters;

namespace TurnBasedRPG.Buffs.Buff;

public class 御极 : BaseBuff
{
    public 御极(int? remainingTurns = null, int strength = 0) : base("御极","每级强度提升10%最终伤害提升", remainingTurns, Math.Clamp(strength, 0, 5))
    {
        IconColor = Color.Orange;
    }
    
    public override void UpdateBuff(Character character)
    {
        // 每级强度提升10%最终伤害提升        character.FinalDamageIncrease += Strength * 0.1f;
    }
}
