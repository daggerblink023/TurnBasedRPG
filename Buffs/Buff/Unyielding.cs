using System;
using TurnBasedRPG.Characters;
using Microsoft.Xna.Framework;

namespace TurnBasedRPG.Buffs.Buff
{
    public class Unyielding : BaseBuff
    {
        public Unyielding(int remainingTurns, int strength)
            : base("不屈", "受到的伤害降低x%", remainingTurns, strength)
        {
            IconColor = Color.LightBlue;
        }
        
        public override void UpdateBuff(Character character)
        {
            // 使目标单位的减伤属性提�?增益强度×10%"
            character.DamageReduction += Strength * 0.1f;
        }
        
        public override void OnAdded(Character character)
        {
            // 当buff被添加时的效�?        }
        
        public override void OnRemoved(Character character)
        {
            // 当buff被移除时的效�?        }
    }
}
