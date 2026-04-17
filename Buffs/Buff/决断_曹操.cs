using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Buffs;
using TurnBasedRPG.Characters;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Buffs.Buff;

public class 决断_曹操 : BaseBuff
{
    public 决断_曹操(int? remainingTurns = null, int strength = 1) : base("决断-曹操", "[回合开始时]每拥有1状态强度，会使得曹操在本回合内临时获得10%伤害减免，随后为曹操添加（10+防御等级/2）点护盾；每2级强度使曹操在本回合内施加的所有状态（包括增益和减益）强度级数+1；整场战斗中，同队所有魏国武将的[决断]类状态最小层数+1，最大层数+1，状态强度设置为额外丢弃的技能等阶+1", remainingTurns, Math.Clamp(strength, 1, 3), isFactionBuff: false, isBuff: true)
    {
        IconColor = Color.Gold;
    }

    public override void UpdateBuff(Character character)
    {
        // 每拥有1状态强度，获得10%伤害减免
        character.DamageReduction += Strength * 0.1f;
    }

    public void OnTurnStart(Character character, BuffHandler buffHandler, BattleSystem battleSystem)
    {
        // 回合开始时，为曹操添加护盾
        int shieldAmount = 10 + character.FinalDefenseLevel / 2;
        if (battleSystem != null && shieldAmount > 0)
        {
            battleSystem.AddShield(character, shieldAmount);
        }
    }
}
