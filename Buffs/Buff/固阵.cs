using Microsoft.Xna.Framework;
using TurnBasedRPG.Buffs;
using TurnBasedRPG.Characters;

namespace TurnBasedRPG.Buffs.Buff;

public class 固阵 : BaseBuff
{
    public 固阵(int? remainingTurns = null, int strength = 0) : base("固阵", "持有护盾时临时提升30%最终伤害减免，攻击等级-3，防御等级+4", remainingTurns, strength, isFactionBuff: true)
    {
        IconColor = Color.LightBlue;
    }
    
    public override void UpdateBuff(Character character)
    {
        // 固阵效果：攻击等级-3，防御等级+4
        character.AttackLevelAdjustment -= 3;
        character.DefenseLevelAdjustment += 4;
    }
    
    public void ApplyShieldDamageReduction(Character character, int shieldValue)
    {
        // 持有护盾时临时提升30%最终伤害减免
        if (shieldValue > 0)
        {
            character.FinalDamageReduction += 0.3f;
        }
    }
}
