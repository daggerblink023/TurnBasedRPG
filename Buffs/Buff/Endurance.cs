using Microsoft.Xna.Framework;
using TurnBasedRPG.Buffs;
using TurnBasedRPG.Characters;

namespace TurnBasedRPG.Buffs.Buff;

public class Endurance : BaseBuff
{
    public Endurance(int remainingTurns, int strength) : base("蹇嶈€", "鍙楀埌鐨勪激瀹抽檷浣巤{strength * 10}%", remainingTurns, strength)
    {
        IconColor = Color.LightBlue;
    }
    
    public override void UpdateBuff(Character character)
    {
        // 蹇嶈€愭晥鏋滐細鍑忎激灞炴€ф彁鍗囷紙寮哄害鍊硷級x10%
        character.DamageReduction += Strength * 0.1f;
    }
}
