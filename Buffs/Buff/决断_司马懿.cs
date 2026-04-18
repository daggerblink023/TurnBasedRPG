using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Buffs;
using TurnBasedRPG.Characters;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Buffs.Buff;

public class 决断_司马懿: BaseBuff
{
    public 决断_司马懿(int? remainingTurns = null, int strength = 1) : base("决断-司马懿", "[回合开始时]每拥有1状态强度，会使得司马懿在本回合内获得20%暴击伤害增加，随后为司马懿添加（10+防御等级/2）点护盾", remainingTurns, Math.Clamp(strength, 1, 3))
    {
        IconColor = Color.LightBlue;
    }
    
    public override void UpdateBuff(Character character)
    {
        // 每拥有1状态强度，提升20%暴击伤害
        character.CritDamage += Strength * 0.2f;
    }
    
    public void OnTurnStart(Character character, BuffHandler buffHandler, BattleSystem battleSystem)
    {

        // 回合开始时，根据状态强度为司马懿添加护盾
        int shieldAmount = Strength * (10 + character.FinalDefenseLevel / 2);

        if (battleSystem != null && shieldAmount > 0)
        {

            battleSystem.AddShield(character, shieldAmount);
        }
        else
        {

        }
    }
}
