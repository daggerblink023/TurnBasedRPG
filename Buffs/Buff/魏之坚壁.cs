﻿using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Buffs;
using TurnBasedRPG.Characters;

namespace TurnBasedRPG.Buffs.Buff;

public class 魏之坚壁 : BaseBuff
{
    public 魏之坚壁(int? remainingTurns = 2, int strength = 1) : base("魏之坚壁", "持有护盾时减伤提升（状态强度*2）%，最终减伤提升（状态强度*2）%；受到伤害时若持有护盾，则受到的最终伤害值降低，降低值为自身生命上限的（状态强度/3）%", remainingTurns, Math.Clamp(strength, 0, 5), isFactionBuff: false, isBuff: true)
    {
        IconColor = Color.Blue;
    }

    public override void UpdateBuff(Character character)
    {
        // 持有护盾时减伤提升（状态强度*2）%
        // 这里暂时只设置基础效果，实际持有护盾时的效果在技能管理中处理
        character.DamageReduction += Strength * 0.02f;
        character.FinalDamageReduction += Strength * 0.02f;
    }
}
