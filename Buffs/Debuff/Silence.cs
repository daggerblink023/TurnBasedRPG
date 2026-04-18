using Microsoft.Xna.Framework;
using TurnBasedRPG.Buffs;
using TurnBasedRPG.Characters;

namespace TurnBasedRPG.Buffs.Debuff;

public class Silence : BaseBuff
{
    public Silence(int remainingTurns, int strength = 1) : base("娌夐粯", "鏃犳硶浣跨敤涓诲姩鎶€鑳?", remainingTurns, strength, false, false)
    {
        IconColor = Color.Gray;
    }
    
    public override void UpdateBuff(Character character)
    {
        // 娌夐粯鏁堟灉灏嗙敱BuffHandler澶勭悊
    }
}
