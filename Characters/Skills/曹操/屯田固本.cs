using System;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Characters.Skills.曹操;

public class 屯田固本 : BaseSkill
{
    public 屯田固本() : base("屯田固本", ActionType.Attack, DamageType.Magic, AttackType.Slash)
    {
        BaseEffectiveness = 4.0f;
        CoinEffectiveness = 12.0f;
        Level = 2;
        ExtraEffects = "[使用前]随机选取主目标外一个敌方单位作为次级目标，本技能的硬币命中时会对次级目标同样造成100%伤害\n[攻击前]若目标持有的减益状态不低于3个，则使此技能的拼点威力提升4\n[攻击后]为此技能命中的所有目标施加一层\"罪己诏\"，为己方全体施加1级2回合\"魏之坚壁\"；使\"仁心\"状态强度提升，提升值相当于此技能总伤害的100%";
    }

    public override void CalculateValues(int attackLevel, int defenseLevel, int morale = 0)
    {
        int effectiveLevel = attackLevel + AttackLevelModifier;
        BaseValue = 8 + (int)(effectiveLevel / 4);
        CoinValue = 2 + (int)(effectiveLevel / 12);
        CoinCount = 2;
    }
}
