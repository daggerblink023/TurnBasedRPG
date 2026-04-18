using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Buffs;
using TurnBasedRPG.Characters;

namespace TurnBasedRPG.Buffs.Buff;

public class 镇国 : BaseBuff
{
    public 镇国(int? remainingTurns = null, int strength = 1) : base("镇国", "防御等级提升（状态强度*3）级", remainingTurns, Math.Clamp(strength, 1, 3), isFactionBuff: true)
    {
        IconColor = Color.DarkBlue;
    }
    
    public override void UpdateBuff(Character character)
    {
        // 防御等级提升（状态强度/3）级
        character.DefenseLevelAdjustment += Strength * 3;
    }
}
