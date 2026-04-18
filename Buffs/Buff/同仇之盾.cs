﻿using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Buffs;
using TurnBasedRPG.Characters;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Buffs.Buff;

public class 同仇之盾 : BaseBuff
{
    private bool _isCaoRen;

    public 同仇之盾(bool isCaoRen = false, int? remainingTurns = null, int strength = 0)
        : base("同仇之盾",
               isCaoRen
                   ? "[回合开始时]使自身本回合内提升（30+6*状态强度）%护盾修正，持有护盾时临时提升30%最终伤害减免\n同队魏国武将（包括自身）的护盾被消耗时，曹仁获得一层[默守]；每当[默守]达到6层，曹仁会消耗全部层数，对最后一个攻击护盾的敌方单位发动反击技能[默守蓄锋]"
                   : "[回合开始时]使自身本回合内提升（25+5*状态强度）%护盾修正，持有护盾时临时提升30%最终伤害减免",
               remainingTurns, Math.Min(5, strength), isFactionBuff: true)
    {
        _isCaoRen = isCaoRen;
        IconColor = Color.Gold;
    }

    public override void UpdateBuff(Character character)
    {
        // 提升护盾修正：曹仁是（30+6*状态强度）%，其他角色是（25+5*状态强度）%
        if (_isCaoRen)
        {
            character.ShieldAdjustment += (0.30f + 0.06f * Strength);
        }
        else
        {
            character.ShieldAdjustment += (0.25f + 0.05f * Strength);
        }

        // 持有护盾时临时提升30%最终伤害减免
        // 护盾检查将在BattleSystem中进行
    }

    public void ApplyShieldDamageReduction(Character character, BattleSystem battleSystem)
    {
        // 检查角色是否有护盾
        int shieldValue = battleSystem.GetCharacterShield(character);
        if (shieldValue > 0)
        {
            character.FinalDamageReduction += 0.3f;
        }
    }
}
