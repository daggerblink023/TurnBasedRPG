using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Buffs;
using TurnBasedRPG.Characters;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Buffs.Buff;

public class 决断_夏侯惇: BaseBuff
{
    public 决断_夏侯惇(int? remainingTurns = null, int strength = 1) : base("决断-夏侯惇", "[回合开始时]每拥有1状态强度，会为夏侯惇添加（10+防御等级/2）点护盾，并使得本回合夏侯惇造成的真实伤害提升10%", remainingTurns, Math.Clamp(strength, 1, 3))
    {
        IconColor = Color.LightBlue;
    }
    
    public override void UpdateBuff(Character character)
    {
        // 决断-夏侯惇效果：每拥有1状态强度，真实伤害提升10%
        // 注意：这个效果只对真实伤害生效，在BattleSystem中处理
    }
    
    public void OnTurnStart(Character character, BuffHandler buffHandler, BattleSystem battleSystem)
    {
        // 回合开始时，根据状态强度为夏侯惇添加护盾
        int shieldAmount = Strength * (10 + character.FinalDefenseLevel / 2);
        if (battleSystem != null && shieldAmount > 0)
        {
            battleSystem.AddShield(character, shieldAmount);
        }
    }
}
