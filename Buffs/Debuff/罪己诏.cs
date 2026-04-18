using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Buffs;
using TurnBasedRPG.Characters;

namespace TurnBasedRPG.Buffs.Debuff;

public class 罪己诏: BaseBuff
{
    public 罪己诏(int? remainingTurns = 1, int strength = 1) : base("罪己诏", "最终减伤降低（2*强度）%，防御等级降低（1*强度）级；受到攻击时若该次攻击施加了负面状态，则额外扣除相当于曹操最大生命（1*强度）%的护盾，若护盾不足则继续扣除生命", remainingTurns, Math.Clamp(strength, 0, 6), isFactionBuff: false, isBuff: false)
    {
        IconColor = Color.DarkRed;
    }

    public override void UpdateBuff(Character character)
    {
        // 最终减伤降低（2*强度）%
        character.FinalDamageReduction -= Strength * 0.02f;
        // 防御等级降低（1*强度）级
        character.DefenseLevel -= Strength;
    }
}
