using Microsoft.Xna.Framework;
using TurnBasedRPG.Buffs;
using TurnBasedRPG.Characters;

namespace TurnBasedRPG.Buffs.Debuff;

public class Silence : BaseBuff
{
    public Silence(int remainingTurns, int strength = 1) : base("沉默", "无法使用主动技�?, remainingTurns, strength, false, false)
    {
        IconColor = Color.Gray;
    }
    
    public override void UpdateBuff(Character character)
    {
        // 沉默效果将由BuffHandler处理
    }
}
