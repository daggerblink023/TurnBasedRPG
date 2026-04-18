using System;
using Microsoft.Xna.Framework;

namespace TurnBasedRPG.Systems;

public abstract class BaseSkill
{
    public string Name { get; set; }
    public ActionType ActionType { get; set; }
    public DamageType DamageType { get; set; }
    public AttackType AttackType { get; set; }
    public int BaseValue { get; set; }
    public int CoinValue { get; set; }
    public int CoinCount { get; set; }
    public int AttackLevelModifier { get; set; } = 0;
    public int DefenseLevelModifier { get; set; } = 0;
    public float BaseEffectiveness { get; set; } = 4.0f; // 姣忓崌澶氬皯绾ф彁鍗?鐐瑰熀纭€鐐规暟
    public float CoinEffectiveness { get; set; } = 12.0f; // 姣忓崌澶氬皯绾ф彁鍗?鐐圭‖甯佺偣鏁?
    public bool CanBeSelected { get; set; } = true; // 鏄惁鍙互琚富鍔ㄩ€夋嫨
    public string ExtraEffects { get; set; } = "杩欐槸涓€涓ず渚嬫妧鑳斤紝娌℃湁棰濆鏁堟灉"; // 鎶€鑳界殑棰濆鏁堟灉
    public int Level { get; set; } = 1; // 鎶€鑳界瓑绾?
    public int CompetingPower { get; set; } = 0; // 鎷肩偣濞佸姏锛屼粎鍦ㄥ弻鏂规妧鑳芥瘮鎷肩偣鏁扮粨绠楀姞绠楀弬涓庢渶缁堢偣鏁帮紝涓嶅弬涓庝激瀹崇粨绠楅樁娈?    
    public bool IsSpecialCounterSkill { get; set; } = false; // 是否是特殊反击技能

    // 暴击相关属性
    public float CritRate { get; set; } = 0f; // 鏈妧鑳芥毚鍑荤巼锛岄粯璁や负0%
    public float FinalCritRate { get; set; } = 0f; // 鏈妧鑳芥渶缁堟毚鍑荤巼锛岄粯璁や负0%
    public float CritDamage { get; set; } = 0f; // 鏈妧鑳芥毚鍑讳激瀹筹紝榛樿涓?%


    protected BaseSkill(string name, ActionType actionType, DamageType damageType, AttackType attackType)
    {
        Name = name;
        ActionType = actionType;
        DamageType = damageType;
        AttackType = attackType;
    }

    public abstract void CalculateValues(int attackLevel, int defenseLevel, int morale = 0);

    public virtual Color GetSkillColor()
    {
        return DamageType switch
        {
            DamageType.Physical => Color.Orange, // 鐗╃悊浼ゅ浣跨敤姗欓粍鑹?            DamageType.Magic => Color.Cyan, // 榄旀硶浼ゅ浣跨敤闈掕壊
            DamageType.True => Color.White, // 鐪熷疄浼ゅ浣跨敤鐧借壊
            _ => Color.Gray
        };
    }

    public virtual string GetAttackTypeName()
    {
        return AttackType switch
        {
            AttackType.Slash => "斩击",
            AttackType.Blunt => "閽濆嚮",
            AttackType.Pierce => "绌垮埡",
            AttackType.Spell => "娉曟湳",
            _ => ""
        };
    }
}