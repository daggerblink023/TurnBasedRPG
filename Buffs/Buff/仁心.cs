using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Buffs;
using TurnBasedRPG.Characters;

namespace TurnBasedRPG.Buffs.Buff;

public class 仁心 : BaseBuff
{
    public 仁心(int? remainingTurns = null, int strength = 0) : base("仁心", "当同队魏国武将的护盾被击破或当前持有的护盾值不高于最大生命的7.5%时，曹操会消耗此状态当前强度的50%，为该武将添加相当于消耗值的护盾；单次添加的护盾量不超过目标最大生命的50%，超出的部分不会从状态强度中被消耗；每回合最多可通过此效果为队友添加3次护�?, remainingTurns, strength, isFactionBuff: false, isBuff: true)
    {
        IconColor = Color.Green;
    }

    public override void UpdateBuff(Character character)
    {
        // 仁心状态主要通过外部触发来消耗强度，这里暂时不需要UpdateBuff逻辑
    }
}
