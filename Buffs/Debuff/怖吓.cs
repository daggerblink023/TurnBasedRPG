using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Buffs;
using TurnBasedRPG.Characters;

namespace TurnBasedRPG.Buffs.Debuff;

public class 怖吓 : BaseBuff
{
    public 怖吓(int remainingTurns = 3, int strength = 1) : base("怖吓", "攻击等级降低（状态强度）�?, remainingTurns, strength, false, false)
    {
        IconColor = Color.DarkGray;
    }
    
    public override void OnAdded(Character character)
    {

    }
    
    public override void UpdateBuff(Character character)
    {
        // 每次UpdateBuff都重新应用效果，因为Character.UpdateBuffs()会先ResetAttributes()
        character.AttackLevelAdjustment -= Strength;
    }
    
    public override void OnRemoved(Character character)
    {

    }
}
