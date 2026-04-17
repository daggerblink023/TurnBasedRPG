using Microsoft.Xna.Framework;
using TurnBasedRPG.Buffs;
using TurnBasedRPG.Characters;

namespace TurnBasedRPG.Buffs.Buff;

public class Endurance : BaseBuff
{
    public Endurance(int remainingTurns, int strength) : base("忍�?, $"受到的伤害降低{strength * 10}%", remainingTurns, strength)
    {
        IconColor = Color.LightBlue;
    }
    
    public override void UpdateBuff(Character character)
    {
        // 忍耐效果：减伤属性提升（强度值）x10%
        character.DamageReduction += Strength * 0.1f;
    }
}
