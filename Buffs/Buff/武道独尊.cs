﻿using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Buffs;
using TurnBasedRPG.Characters;

namespace TurnBasedRPG.Buffs.Buff;

public class 武道独尊 : BaseBuff
{
    public 武道独尊() : base("武道独尊", "张辽免疫所有来源的攻击等级降低效果，自身所有技能强制以攻击等级进行各项计算\n张辽的攻击等级+5，防御等级-3\n若张辽的攻击等级高于目标，则在攻击时临时获得（攻击等级差*0.75）%伤害增加，上限为75%；并在技能硬币命中时额外扣除目标1点士气值", null, 0, false, true)
    {
        IconColor = Color.Gold;
    }
    
    public override void OnAdded(Character character)
    {

        character.HasWuDaoDuZun = true;
    }
    
    public override void OnRemoved(Character character)
    {

        character.HasWuDaoDuZun = false;
    }
    
    public override void UpdateBuff(Character character)
    {
        // 每次UpdateBuff都重新应用效果，因为Character.UpdateBuffs()会先ResetAttributes()
        character.AttackLevelAdjustment += 5;
        character.DefenseLevelAdjustment -= 3;
        character.HasWuDaoDuZun = true;
    }
}
