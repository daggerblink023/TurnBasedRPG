using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Buffs;
using TurnBasedRPG.Characters;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Buffs.Buff;

public class 决断_曹仁 : BaseBuff
{
    public 决断_曹仁(int? remainingTurns = null, int strength = 1) : base("决断-曹仁", "[回合开始时]每拥�?状态强度，会使得曹仁在本回合内提升10%护盾修正，随后为曹仁添加�?0+防御等级/2）点护盾，并为同队的其他魏国武将添加�?+防御等级/4）点护盾", remainingTurns, Math.Clamp(strength, 1, 3))
    {
        IconColor = Color.LightBlue;
    }
    
    public override void UpdateBuff(Character character)
    {
        // 每拥�?状态强度，提升10%护盾修正
        character.ShieldAdjustment += Strength * 0.1f;
    }
    
    public void OnTurnStart(Character character, BuffHandler buffHandler, BattleSystem battleSystem, List<Character> allCharacters)
    {
        // 回合开始时，根据状态强度为曹仁添加护盾
        int shieldAmount = Strength * (10 + character.FinalDefenseLevel / 2);
        if (battleSystem != null && shieldAmount > 0)
        {
            battleSystem.AddShield(character, shieldAmount);
        }
        
        // 为同队的其他魏国武将添加护盾
        int allyShieldAmount = Strength * (5 + character.FinalDefenseLevel / 4);
        if (battleSystem != null && allyShieldAmount > 0 && allCharacters != null)
        {
            foreach (var chara in allCharacters)
            {
                if (chara != character && chara.Faction == Faction.�?&& chara.IsAlly == character.IsAlly)
                {
                    battleSystem.AddShield(chara, allyShieldAmount);
                }
            }
        }
    }
}
