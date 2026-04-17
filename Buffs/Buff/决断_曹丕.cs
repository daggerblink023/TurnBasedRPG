using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Buffs;
using TurnBasedRPG.Characters;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Buffs.Buff;

public class 决断_曹丕 : BaseBuff
{
    public 决断_曹丕(int? remainingTurns = null, int strength = 1) : base("决断-曹丕", "[回合开始时]每拥有1状态强度为全队魏国武将回复1点士气值，随后为曹丕添加（10+防御等级/2）点护盾", remainingTurns, Math.Clamp(strength, 1, 3))
    {
        IconColor = Color.LightBlue;
    }
    
    public void OnTurnStart(Character character, BuffHandler buffHandler, BattleSystem battleSystem, List<Character> allCharacters)
    {
        // 为全队魏国武将回复士气值
        foreach (var chara in allCharacters)
        {
            if (chara.Faction == Faction.魏 && chara.IsAlly == character.IsAlly)
            {
                chara.AdjustMorale(Strength);
            }
        }
        
        // 为曹丕添加护盾
        int shieldAmount = Strength * (10 + character.FinalDefenseLevel / 2);
        if (battleSystem != null && shieldAmount > 0)
        {
            battleSystem.AddShield(character, shieldAmount);
        }
    }
}