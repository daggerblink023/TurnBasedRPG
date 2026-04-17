using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Buffs;
using TurnBasedRPG.Characters;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Buffs.Buff;

public class 决断_张辽 : BaseBuff
{
    public 决断_张辽(int? remainingTurns = null, int strength = 1) : base("决断-张辽", "[回合开始时]每拥�?状态强度，会使得张辽在本回合内临时获得15%暴击率，随后为张辽添加（10+攻击等级/2）点护盾", remainingTurns, Math.Max(strength, 1), false, true)
    {
        IconColor = Color.LightBlue;
    }
    
    public void OnTurnStart(Character character, BuffHandler buffHandler, BattleSystem battleSystem)
    {

        
        // 临时获得15%暴击率每强度
        character.FinalCritRate += Strength * 0.15f;

        
        // 添加护盾 - 使用攻击等级计算
        int shieldAmount = 10 + character.FinalAttackLevel / 2;

        if (battleSystem != null && shieldAmount > 0)
        {
            battleSystem.AddShield(character, shieldAmount);
        }
    }
}
