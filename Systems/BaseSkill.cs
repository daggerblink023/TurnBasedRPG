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
    public float BaseEffectiveness { get; set; } = 4.0f; // 每升多少级提升1点基础点数
    public float CoinEffectiveness { get; set; } = 12.0f; // 每升多少级提升1点硬币点数
    public bool CanBeSelected { get; set; } = true; // 是否可以被主动选择
    public string ExtraEffects { get; set; } = "这是一个示例技能，没有额外效果"; // 技能的额外效果
    public int Level { get; set; } = 1; // 技能等级
    public int CompetingPower { get; set; } = 0; // 拼点威力，仅在双方技能比拼点数结算加算参与最终点数，不参与伤害结算阶段
    
    // 暴击相关属性
    public float CritRate { get; set; } = 0f; // 本技能暴击率，默认为0%
    public float FinalCritRate { get; set; } = 0f; // 本技能最终暴击率，默认为0%
    public float CritDamage { get; set; } = 0f; // 本技能暴击伤害，默认为0%
    
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
            DamageType.Physical => Color.Orange, // 物理伤害使用橙黄色
            DamageType.Magic => Color.Cyan, // 魔法伤害使用青色
            DamageType.True => Color.White, // 真实伤害使用白色
            _ => Color.Gray
        };
    }
    
    public virtual string GetAttackTypeName()
    {
        return AttackType switch
        {
            AttackType.Slash => "斩击",
            AttackType.Blunt => "钝击",
            AttackType.Pierce => "穿刺",
            AttackType.Spell => "法术",
            _ => ""
        };
    }
}