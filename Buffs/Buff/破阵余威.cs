using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Buffs;
using TurnBasedRPG.Characters;

namespace TurnBasedRPG.Buffs.Buff;

public class 破阵余威 : BaseBuff
{
    public 破阵余威() : base("破阵余威", "速度最小值与速度最大值分�?1\n基础点数+2，暴击率+30%\n使技�?叠加[奔狼]的每回合触发次数限制提升�?，且技�?命中时为目标施加[怖吓]\n使技�?额外对目标造成一次基于（最终点�?2）的真实伤害", null, 0, false, true)
    {
        IconColor = Color.Red;
    }
    
    public override void UpdateBuff(Character character)
    {
        // 速度最小值与速度最大值分�?1
        character.MinSpeedAdjustment += 1;
        character.MaxSpeedAdjustment += 1;
    }
    
    public override void OnAdded(Character character)
    {

        character.FinalCritRate += 0.3f;
    }
    
    public override void OnRemoved(Character character)
    {

        character.FinalCritRate -= 0.3f;
    }
}
