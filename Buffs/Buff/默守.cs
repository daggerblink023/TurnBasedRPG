using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Characters;

namespace TurnBasedRPG.Buffs.Buff;

public class 默守 : BaseBuff
{
    public Character LastShieldHitAttacker { get; set; }
    
    public 默守(int? remainingTurns = null, int strength = 0) 
        : base("默守", "曹仁正在积蓄锋芒，等待反戈之�?, remainingTurns, strength)
    {
        IconColor = Color.DarkOrange;
        LastShieldHitAttacker = null;
    }
    
    public override void UpdateBuff(Character character)
    {
        // 默守状态不需要特殊的Update逻辑
    }
    
    public void AddLayer()
    {
        Strength = Strength + 1;
    }
    
    public bool ShouldTriggerCounter()
    {
        return Strength >= 6;
    }
    
    public void ResetLayers()
    {
        Strength = 0;
    }
}
