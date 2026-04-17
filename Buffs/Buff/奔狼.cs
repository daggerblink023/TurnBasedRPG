using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Buffs;
using TurnBasedRPG.Characters;

namespace TurnBasedRPG.Buffs.Buff;

public class 奔狼 : BaseBuff
{
    public 奔狼(int strength = 1) : base("奔狼", "攻击等级+（状态强度）级，防御等级-（状态强度）级；受到的反弹伤害降低（40+状态强�?5�?\n每拥�?级强度，速度最小值与速度最大值分�?1", null, strength, false, true)
    {
        IconColor = Color.DarkOrange;
    }
    
    public override void OnAdded(Character character)
    {

    }
    
    public override void OnRemoved(Character character)
    {

    }
    
    public override void UpdateBuff(Character character)
    {
        // 每次UpdateBuff都重新应用效果，因为Character.UpdateBuffs()会先ResetAttributes()
        character.AttackLevelAdjustment += Strength;
        character.DefenseLevelAdjustment -= Strength;
        
        // 每拥�?级强度，速度最小值与速度最大值分�?1
        int speedBonus = Strength / 5;
        if (speedBonus > 0)
        {
            character.MinSpeedAdjustment += speedBonus;
            character.MaxSpeedAdjustment += speedBonus;
        }
    }
    
    public float GetReflectionDamageReduction()
    {
        return 0.4f + Strength * 0.05f;
    }
}
