using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Buffs;
using TurnBasedRPG.Characters;

namespace TurnBasedRPG.Buffs.Debuff;

public class 怖吓 : BaseBuff
{
    public 怖吓(int remainingTurns = 3, int strength = 1) : base("怖吓", "鏀诲嚮绛夌骇闄嶄綆锛堢姸鎬佸己搴︼級绾?", remainingTurns, strength, false, false)
    {
        IconColor = Color.DarkGray;
    }
    
    public override void OnAdded(Character character)
    {

    }
    
    public override void UpdateBuff(Character character)
    {
        // 姣忔UpdateBuff閮介噸鏂板簲鐢ㄦ晥鏋滐紝鍥犱负Character.UpdateBuffs()浼氬厛ResetAttributes()
        character.AttackLevelAdjustment -= Strength;
    }
    
    public override void OnRemoved(Character character)
    {

    }
}
