using Microsoft.Xna.Framework;
using TurnBasedRPG.Buffs;
using TurnBasedRPG.Characters;

namespace TurnBasedRPG.Buffs.Buff;

public class Ganglie : BaseBuff
{
    public Ganglie(int remainingTurns, int strength = 1) : base("鍒氱儓", "鏈洖鍚堣嚜韬寔鏈夋姢鐩炬椂涓存椂鎻愬崌50%浼ゅ鍑忓厤锛屽苟灏嗗彈鍒扮殑鎶ょ浘浼ゅ浠?00%姣斾緥鐨勭湡瀹炰激瀹冲弽寮圭粰浼ゅ鏉ユ簮", remainingTurns, strength)
    {
        IconColor = Color.LightBlue;
    }
    
    public override void UpdateBuff(Character character)
    {
        // 鍒氱儓鏁堟灉灏嗙敱BuffHandler澶勭悊
    }
    
    public void ApplyShieldDamageReduction(Character character, int shieldValue)
    {
        // 鎸佹湁鎶ょ浘鏃朵复鏃舵彁鍗?0%浼ゅ鍑忓厤
        if (shieldValue > 0)
        {
            character.FinalDamageReduction += 0.5f;
        }
    }
}
