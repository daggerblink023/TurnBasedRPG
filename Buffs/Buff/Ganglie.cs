using Microsoft.Xna.Framework;
using TurnBasedRPG.Buffs;
using TurnBasedRPG.Characters;

namespace TurnBasedRPG.Buffs.Buff;

public class Ganglie : BaseBuff
{
    public Ganglie(int remainingTurns, int strength = 1) : base("刚烈", "本回合自身持有护盾时临时提升50%伤害减免，并将受到的护盾伤害�?00%比例的真实伤害反弹给伤害来源", remainingTurns, strength)
    {
        IconColor = Color.LightBlue;
    }
    
    public override void UpdateBuff(Character character)
    {
        // 刚烈效果将由BuffHandler处理
    }
    
    public void ApplyShieldDamageReduction(Character character, int shieldValue)
    {
        // 持有护盾时临时提�?0%伤害减免
        if (shieldValue > 0)
        {
            character.FinalDamageReduction += 0.5f;
        }
    }
}
