using System;
using TurnBasedRPG.Characters;
using Microsoft.Xna.Framework;

namespace TurnBasedRPG.Buffs.Debuff;

public class 围城 : BaseBuff
{
    public 围城(int remainingTurns, int strength = 1) : base("围城", "鎶ょ浘鏁堢巼闄嶄綆30%", remainingTurns, strength, false, false)
    {
        IconColor = Color.Brown;
    }
    
    public override void UpdateBuff(Character character)
    {
        // 围城鐘舵€侀檷浣庢姢鐩句慨姝ｅ€?.3f
        character.ShieldAdjustment -= 0.3f;
    }
    
    public override void OnAdded(Character character)
    {

    }
    
    public override void OnRemoved(Character character)
    {

    }
}
