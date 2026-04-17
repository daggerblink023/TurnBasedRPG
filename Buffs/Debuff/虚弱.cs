using System;
using TurnBasedRPG.Characters;
using Microsoft.Xna.Framework;

namespace TurnBasedRPG.Buffs.Debuff;

public class 虚弱 : BaseBuff
{
    public 虚弱(int remainingTurns, int strength = 1) : base("虚弱", "每级强度使伤害增加属性降�?0%", remainingTurns, strength, false, false)
    {
        IconColor = Color.Yellow;
    }
    
    public override void UpdateBuff(Character character)
    {
        // 使伤害增加属性降低（10*状态强度）%
        character.DamageIncrease -= Strength * 0.1f;
    }
    
    public override void OnAdded(Character character)
    {

    }
    
    public override void OnRemoved(Character character)
    {

    }
}
