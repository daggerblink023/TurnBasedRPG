using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Buffs;
using TurnBasedRPG.Characters;
using TurnBasedRPG.Characters.Allies;

namespace TurnBasedRPG.Buffs.Buff;

public class 神威 : BaseBuff
{
    public 神威(int remainingTurns = 2) : base("神威", "所有向自身施加的减益状态与对自身施加的伤害无效，速度最小值与速度最大值分�?2\n自身投掷的所有硬币必定为正面，拼点威�?�?+攻击等级/20），且技能的最后一枚硬币命中时扣除目标1点士气值\n行动槽中的所有技能被替换为技�?，并暂时取消技�?叠加[奔狼]的每回合触发次数限制\n[状态结束时]为自身施加[破阵余威]", remainingTurns, 0, false, true)
    {
        IconColor = Color.Purple;
    }
    
    public override void UpdateBuff(Character character)
    {
        // 速度最小值与速度最大值分�?2
        character.MinSpeedAdjustment += 2;
        character.MaxSpeedAdjustment += 2;
    }
    
    public override void OnAdded(Character character)
    {

    }
    
    public override void OnRemoved(Character character)
    {

        
        // 如果角色是张辽，取消神威状态标�?        if (character is 张辽 zhangLiao)
        {
            zhangLiao.SetInShenWeiState(false);
        }
        
        // 为自身施加破阵余�?        // 这里需要BuffHandler的引用，暂时不实现，在BattleSystem中处�?        // 检查是否是因致死伤害触发的威震逍遥津，如果是则使张辽死�?        if (character is TurnBasedRPG.Characters.Allies.张辽 zhangLiao2 && zhangLiao2.DiedFromLethalDamage())
        {

            character.CurrentHealth = 0;
        }
    }
}
