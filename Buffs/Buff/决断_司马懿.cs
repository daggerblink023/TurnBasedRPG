using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Buffs;
using TurnBasedRPG.Characters;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Buffs.Buff;

public class 决断_司马�?: BaseBuff
{
    public 决断_司马�?int? remainingTurns = null, int strength = 1) : base("决断-司马�?, "[回合开始时]每拥�?状态强度，会使得司马懿在本回合内获�?0%暴击伤害增加，随后为司马懿添加（10+防御等级/2）点护盾", remainingTurns, Math.Clamp(strength, 1, 3))
    {
        IconColor = Color.LightBlue;
    }
    
    public override void UpdateBuff(Character character)
    {
        // 每拥�?状态强度，提升20%暴击伤害
        character.CritDamage += Strength * 0.2f;
    }
    
    public void OnTurnStart(Character character, BuffHandler buffHandler, BattleSystem battleSystem)
    {

        // 回合开始时，根据状态强度为司马懿添加护�?        int shieldAmount = Strength * (10 + character.FinalDefenseLevel / 2);

        if (battleSystem != null && shieldAmount > 0)
        {

            battleSystem.AddShield(character, shieldAmount);
        }
        else
        {

        }
    }
}
