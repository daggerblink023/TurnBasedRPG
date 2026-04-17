using Microsoft.Xna.Framework;
using TurnBasedRPG.Buffs;
using TurnBasedRPG.Characters;

namespace TurnBasedRPG.Buffs.Buff;

public class 魏武固阵 : BaseBuff
{
    public 魏武固阵(int? remainingTurns = null, int strength = 0) : base("魏武固阵", "持有护盾时临时提�?0%最终伤害减免，攻击等级-6，防御等�?8；所有技能中，涉及攻击等级的数值计算均改为以防御等级进行计�?, remainingTurns, strength, isFactionBuff: true)
    {
        IconColor = Color.LightBlue;
    }
    
    public override void UpdateBuff(Character character)
    {
        // 魏武固阵效果：攻击等�?6，防御等�?8
        character.AttackLevelAdjustment -= 6;
        character.DefenseLevelAdjustment += 8;
        character.HasWeiWuGuZhen = true;
    }
    
    public void ApplyShieldDamageReduction(Character character, int shieldValue)
    {
        // 持有护盾时临时提�?0%最终伤害减�?        if (shieldValue > 0)
        {
            character.FinalDamageReduction += 0.3f;
        }
    }
}
