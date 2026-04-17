using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Buffs;
using TurnBasedRPG.Characters;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Buffs.Buff;

public class 嗣业承祚 : BaseBuff
{
    public 嗣业承祚(int? remainingTurns = null, int strength = 0) : base("嗣业承祚", "每拥有1状态强度，会使得曹丕临时获得2级防御等级\n若在强度到达5层后获取强度，每级溢出的状态强度会使自身获得相当于2.5%最大生命的护盾，并永久获得（溢出的状态强度*10）%最终伤害提升（上限为50%）", remainingTurns, Math.Clamp(strength, 0, 6))
    {
        IconColor = Color.Gold;
    }
    
    public override void UpdateBuff(Character character)
    {
        // 每拥有1状态强度，临时获得2级防御等级
        character.DefenseLevelAdjustment += Strength * 2;
    }
    
    public void HandleOverflow(Character character, BuffHandler buffHandler, BattleSystem battleSystem)
    {
        if (Strength > 5)
        {
            int overflowStrength = Strength - 5;
            Game1.Log($"[嗣业承祚-溢出] 溢出强度={overflowStrength}");
            
            // 每级溢出的状态强度使自身获得相当于2.5%最大生命的护盾
            int shieldAmount = (int)(character.MaxHealth * 0.025f * overflowStrength);
            if (battleSystem != null && shieldAmount > 0)
            {
                battleSystem.AddShield(character, shieldAmount);
                Game1.Log($"[嗣业承祚-溢出] 添加护盾={shieldAmount}");
            }
            
            // 永久获得（溢出的状态强度*10）%最终伤害提升（上限为50%）
            // 检查是否已有御极状态
            var yujiBuff = buffHandler.GetBuffs(character).Find(b => b is 御极);
            if (yujiBuff is 御极)
            {
                yujiBuff.Strength = Math.Min(yujiBuff.Strength + overflowStrength, 5);
                Game1.Log($"[嗣业承祚-溢出] 御极状态强度增加至{yujiBuff.Strength}");
            }
            else
            {
                buffHandler.AddBuff(character, new 御极(null, overflowStrength));
                Game1.Log($"[嗣业承祚-溢出] 创建御极状态，强度={overflowStrength}");
            }
            
            // 将强度设置为5
            Strength = 5;
            Game1.Log($"[嗣业承祚-溢出] 嗣业承祚状态强度重置为5");
        }
    }
}