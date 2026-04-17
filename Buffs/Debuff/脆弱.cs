using System;
using TurnBasedRPG.Characters;
using Microsoft.Xna.Framework;

namespace TurnBasedRPG.Buffs.Debuff;

public class 脆弱 : BaseBuff
{
    public 脆弱(int remainingTurns, int strength = 1) : base("脆弱", "每级强度使伤害减免属性降�?0%", remainingTurns, strength, false, false)
    {
        IconColor = Color.Red;
    }
    
    public override void UpdateBuff(Character character)
    {
        // 每级强度使伤害减免属性降�?0%
        character.DamageReduction -= Strength * 0.1f;
    }
    
    public override void OnAdded(Character character)
    {

    }
    
    public override void OnRemoved(Character character)
    {

    }
}
