using System;
using TurnBasedRPG.Characters;
using Microsoft.Xna.Framework;

namespace TurnBasedRPG.Buffs.Buff
{
    public class Unyielding : BaseBuff
    {
        public Unyielding(int remainingTurns, int strength)
            : base("涓嶅眻", "鍙楀埌鐨勪激瀹抽檷浣巟%", remainingTurns, strength)
        {
            IconColor = Color.LightBlue;
        }
        
        public override void UpdateBuff(Character character)
        {
            // 浣跨洰鏍囧崟浣嶇殑鍑忎激灞炴€ф彁楂?澧炵泭寮哄害脳10%"
            character.DamageReduction += Strength * 0.1f;
        }
        
        public override void OnAdded(Character character)
        {
            // 褰揵uff琚坊鍔犳椂鐨勬晥鏋?
            }
        
        public override void OnRemoved(Character character)
        {
            // 褰揵uff琚Щ闄ゆ椂鐨勬晥鏋?
            }
    }
}
