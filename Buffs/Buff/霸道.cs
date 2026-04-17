using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Buffs;
using TurnBasedRPG.Characters;

namespace TurnBasedRPG.Buffs.Buff;

public class 霸道 : BaseBuff
{
    public 霸道(int? remainingTurns = null, int strength = 0) : base("霸道", "曹操每次使用技能时，此状态强�?1；同队武将（包括自身）每次使用技能向敌方单位施加减益时，此状态强�?1（每个行动槽只计算一次）", remainingTurns, Math.Clamp(strength, 0, 8), isFactionBuff: false, isBuff: true)
    {
        IconColor = Color.Red;
    }

    public override void UpdateBuff(Character character)
    {
        // 霸道状态主要通过外部触发来增加强度，这里暂时不需要UpdateBuff逻辑
    }
}
