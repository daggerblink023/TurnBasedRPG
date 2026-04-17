using System;
using System.Collections.Generic;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Characters;

// 势力枚举
public enum Faction
{
    无,
    魏,
    蜀,
    吴,
    群
}

// 攻击技能1类
public class CombatSkill1 : BaseSkill
{
    public CombatSkill1()
        : base("技能 1", ActionType.Attack, DamageType.Physical, AttackType.Slash)
    {
        // 设置默认值
        BaseEffectiveness = 4.0f; // 每4级提升1点基础点数
        CoinEffectiveness = 12.0f; // 每12级提升1点硬币点数
        Level = 1;
    }
    
    public override void CalculateValues(int attackLevel, int defenseLevel, int morale = 0)
    {
        int effectiveAttackLevel = attackLevel + AttackLevelModifier;
        BaseValue = 4 + (int)(effectiveAttackLevel / BaseEffectiveness);
        CoinValue = 4 + (int)(effectiveAttackLevel / CoinEffectiveness);
        CoinCount = 2;
    }
}

// 攻击技能2类
public class CombatSkill2 : BaseSkill
{
    public CombatSkill2()
        : base("技能 2", ActionType.Attack, DamageType.Physical, AttackType.Slash)
    {
        // 设置默认值
        BaseEffectiveness = 4.0f; // 每4级提升1点基础点数
        CoinEffectiveness = 12.0f; // 每12级提升1点硬币点数
        Level = 2;
    }
    
    public override void CalculateValues(int attackLevel, int defenseLevel, int morale = 0)
    {
        int effectiveAttackLevel = attackLevel + AttackLevelModifier;
        BaseValue = 4 + (int)(effectiveAttackLevel / BaseEffectiveness);
        CoinValue = 3 + (int)(effectiveAttackLevel / CoinEffectiveness);
        CoinCount = 3;
    }
}

// 攻击技能3类
public class CombatSkill3 : BaseSkill
{
    public CombatSkill3()
        : base("技能 3", ActionType.Attack, DamageType.Physical, AttackType.Slash)
    {
        // 设置默认值
        BaseEffectiveness = 4.0f; // 每4级提升1点基础点数
        CoinEffectiveness = 9.0f; // 每9级提升1点硬币点数（按要求调整）
        AttackLevelModifier = 3; // 攻击等级修正+3
        Level = 3;
    }
    
    public override void CalculateValues(int attackLevel, int defenseLevel, int morale = 0)
    {
        int effectiveAttackLevel = attackLevel + AttackLevelModifier;
        BaseValue = 5 + (int)(effectiveAttackLevel / BaseEffectiveness);
        CoinValue = 5 + (int)(effectiveAttackLevel / CoinEffectiveness);
        CoinCount = 2;
    }
}

// 防御技能类
public class DefendSkill : BaseSkill
{
    public DefendSkill()
        : base("防御", ActionType.Defend, DamageType.Physical, AttackType.Blunt)
    {
        // 设置默认值
        BaseEffectiveness = 4.0f; // 每4级提升1点基础点数
        CoinEffectiveness = 12.0f; // 每12级提升1点硬币点数
        Level = 1;
    }
    
    public override void CalculateValues(int attackLevel, int defenseLevel, int morale = 0)
    {
        int effectiveDefenseLevel = defenseLevel + DefenseLevelModifier;
        BaseValue = 10 + (int)(effectiveDefenseLevel / BaseEffectiveness);
        CoinValue = 4 + (int)(effectiveDefenseLevel / CoinEffectiveness);
        CoinCount = 1;
    }
}

// 治疗技能类
public class HealSkill : BaseSkill
{
    public HealSkill()
        : base("治疗", ActionType.Heal, DamageType.Magic, AttackType.Spell)
    {
        // 设置默认值
        BaseEffectiveness = 4.0f; // 每4级提升1点基础点数
        CoinEffectiveness = 12.0f; // 每12级提升1点硬币点数
        Level = 1;
    }
    
    public override void CalculateValues(int attackLevel, int defenseLevel, int morale = 0)
    {
        int effectiveAttackLevel = attackLevel + AttackLevelModifier;
        BaseValue = 6 + (int)(effectiveAttackLevel / BaseEffectiveness);
        CoinValue = 4 + (int)(effectiveAttackLevel / CoinEffectiveness);
        CoinCount = 2;
    }
}

// 闪避技能类
// 反击技能类
public class CounterSkill : BaseSkill
{
    public CounterSkill()
        : base("反击技能", ActionType.Counter, DamageType.True, AttackType.Blunt)
    {
        // 设置默认值
        BaseEffectiveness = 3.0f; // 每3级提升1点基础点数
        CoinEffectiveness = 8.0f; // 每8级提升1点硬币点数
        Level = 1;
    }
    
    public override void CalculateValues(int attackLevel, int defenseLevel, int morale = 0)
    {
        int effectiveAttackLevel = attackLevel + AttackLevelModifier;
        BaseValue = 10 + (int)(effectiveAttackLevel / BaseEffectiveness);
        CoinValue = 4 + (int)(effectiveAttackLevel / CoinEffectiveness);
        CoinCount = 2;
    }
    
    public override Microsoft.Xna.Framework.Color GetSkillColor()
    {
        return Microsoft.Xna.Framework.Color.Purple; // 反击技能使用紫色
    }
}

// 闪避技能类
public class DodgeSkill : BaseSkill
{
    public DodgeSkill()
        : base("闪避", ActionType.Dodge, DamageType.Physical, AttackType.Slash)
    {
        // 设置默认值
        BaseEffectiveness = 4.0f; // 每4级提升1点基础点数
        CoinEffectiveness = 12.0f; // 每12级提升1点硬币点数
        Level = 1;
    }
    
    public override void CalculateValues(int attackLevel, int defenseLevel, int morale = 0)
    {
        int effectiveDefenseLevel = defenseLevel + DefenseLevelModifier;
        BaseValue = 4 + (int)(effectiveDefenseLevel / BaseEffectiveness);
        CoinValue = 10 + (int)(effectiveDefenseLevel / CoinEffectiveness);
        CoinCount = 1;
    }
}

public abstract class Character
{
    public string Name { get; set; }
    public int Level { get; set; }
    public int AttackLevel { get; set; }
    public int DefenseLevel { get; set; }
    public int AttackLevelModifier { get; set; }
    public int DefenseLevelModifier { get; set; }
    public int MaxHealth { get; set; }
    public int CurrentHealth { get; set; }
    public int AttackPower { get; set; }
    public int Morale { get; set; }
    public bool IsDefending { get; set; }
    private bool _ally; // 阵营属性：true表示己方，false表示敌方
    
    public bool IsAlly => _ally; // 公共getter方法
    // 新增属性：减伤，增伤，最终减伤，最终增伤，默认为0%
    public float DamageReduction { get; set; } = 0f;
    public float DamageIncrease { get; set; } = 0f;
    public float FinalDamageReduction { get; set; } = 0f;
    public float FinalDamageIncrease { get; set; } = 0f;
    
    // 暴击相关属性：暴击率，暴击抗性，最终暴击率，最终暴击抗性，默认为0%，不低于0%
    public float CritRate { get; set; } = 0.5f; // 50%暴击率
    public float CritResistance { get; set; } = 0.4f; // 40%暴击抗性
    public float FinalCritRate { get; set; } = 0f; // 0%最终暴击率
    public float FinalCritResistance { get; set; } = 0f; // 0%最终暴击抗性
    
    // 暴击伤害相关属性：暴击伤害，暴击伤害抗性，默认为0%，不低于0%
    public float CritDamage { get; set; } = 0.5f; // 50%暴击伤害
    public float CritDamageResistance { get; set; } = 0f; // 0%暴击伤害抗性
    // 护盾效果倍率，默认值为1.0
    public float ShieldEffectiveness { get; set; } = 1.0f;
    // 攻击等级和防御等级修正值
    public int AttackLevelAdjustment { get; set; } = 0;
    public int DefenseLevelAdjustment { get; set; } = 0;
    // 护盾修正值，默认值为0
    public float ShieldAdjustment { get; set; } = 0.0f;
    // 最终攻击等级和防御等级
    public int FinalAttackLevel { get; set; }
    public int FinalDefenseLevel { get; set; }
    public bool HasWeiWuGuZhen { get; set; } = false; // 是否有魏武固阵状态
    public bool HasWuDaoDuZun { get; set; } = false; // 是否有武道独尊状态
    
    // 速度相关属性
    public int Speed { get; set; }
    public int MinSpeed { get; set; } = 3;
    public int MaxSpeed { get; set; } = 6;
    public int MinSpeedAdjustment { get; set; }
    public int MaxSpeedAdjustment { get; set; }
    public int FinalMinSpeed { get; set; }
    public int FinalMaxSpeed { get; set; }
    public int SelectionOrder { get; set; } // 角色选择顺序，用于同速度时排序
    public int FixedSlotCount { get; set; } // 整局游戏内固定的行动槽数量
    public List<BaseSkill> AttackSkills { get; protected set; }
    public DefendSkill DefendSkill { get; protected set; }
    public HealSkill HealSkill { get; protected set; }
    public DodgeSkill DodgeSkill { get; protected set; }
    public CounterSkill CounterSkill { get; protected set; }
    public string PassiveName { get; set; } = "默认被动"; // 角色被动技能的名称
    public string PassiveSkill { get; set; } = "这是一个模板角色，没有被动技能"; // 角色的被动技能
    public Faction Faction { get; set; } = Faction.无; // 角色所属势力
    
    // 伤害种类抗性
    public float PhysicalVulnerability { get; set; } = 1.0f; // 物理伤害易损1.0
    public float MagicVulnerability { get; set; } = 1.0f; // 魔法伤害易损1.0
    public float TrueVulnerability { get; set; } = 2.0f; // 真实伤害易损2.0
    
    // 攻击方式抗性
    public float SlashVulnerability { get; set; } = 1.0f; // 斩击伤害易损1.0
    public float BluntVulnerability { get; set; } = 1.0f; // 钝击伤害易损1.0
    public float PierceVulnerability { get; set; } = 1.0f; // 穿刺伤害易损1.0
    public float SpellVulnerability { get; set; } = 1.0f; // 法术伤害易损1.0

    public Character(string name, int baseHealth, float healthGrowth, int level = 40, int attackLevelModifier = 0, int defenseLevelModifier = 0, bool isAlly = true)
    {
        Name = name;
        Level = level;
        AttackLevelModifier = attackLevelModifier;
        DefenseLevelModifier = defenseLevelModifier;
        AttackLevel = Math.Max(1, Level + attackLevelModifier);
        DefenseLevel = Math.Max(1, Level + defenseLevelModifier);
        // 初始化最终攻击等级和防御等级
        FinalAttackLevel = Math.Max(1, AttackLevel + AttackLevelAdjustment);
        FinalDefenseLevel = Math.Max(1, DefenseLevel + DefenseLevelAdjustment);
        // 所有角色的基础生命与生命成长调整为原有的1.25倍
        MaxHealth = (int)((baseHealth + (healthGrowth * (level - 1))) * 1.25f);
        CurrentHealth = MaxHealth;
        AttackPower = 20 + (level / 5);
        Morale = 0;
        IsDefending = false;
        _ally = isAlly; // 初始化阵营属性，默认为true（己方）
        
        // 初始化速度相关属性
        Speed = MinSpeed; // 默认速度为最小速度
        SelectionOrder = 0; // 选择顺序在战斗初始化时设置
        FixedSlotCount = 0; // 固定行动槽数量在战斗初始化时设置
        MinSpeedAdjustment = 0; // 最小速度修正值
        MaxSpeedAdjustment = 0; // 最大速度修正值
        FinalMinSpeed = MinSpeed; // 最终最小速度
        FinalMaxSpeed = MaxSpeed; // 最终最大速度
        
        InitializeSkills();
        // 设置技能初始属性
        SetSkillInitialProperties();
    }
    
    protected virtual void InitializeSkills()
    {
        // 初始化攻击技能池
        AttackSkills = new List<BaseSkill>();
        // 3个1技能、2个2技能、1个3技能
        for (int i = 0; i < 3; i++) AttackSkills.Add(new CombatSkill1());
        for (int i = 0; i < 2; i++) AttackSkills.Add(new CombatSkill2());
        AttackSkills.Add(new CombatSkill3());
        ShuffleAttackSkills();
        
        // 初始化其他技能
        DefendSkill = new DefendSkill();
        HealSkill = new HealSkill();
        DodgeSkill = new DodgeSkill();
        CounterSkill = new CounterSkill();
    }
    
    protected void ShuffleAttackSkills()
    {
        Random random = new Random();
        for (int i = AttackSkills.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (AttackSkills[i], AttackSkills[j]) = (AttackSkills[j], AttackSkills[i]);
        }
    }
    
    public BaseSkill DrawAttackSkill()
    {
        if (AttackSkills.Count > 0)
        {
            BaseSkill skill = AttackSkills[0];
            AttackSkills.RemoveAt(0);
            return skill;
        }
        else
        {
            InitializeSkills();
            BaseSkill skill = AttackSkills[0];
            AttackSkills.RemoveAt(0);
            return skill;
        }
    }
    
    public virtual BaseSkill GetSkillByActionType(ActionType actionType, AttackSkill? attackSkill = null)
    {
        switch (actionType)
        {
            case ActionType.Attack:
                if (attackSkill.HasValue)
                {
                    BaseSkill skill = attackSkill.Value switch
                    {
                        AttackSkill.Skill1 => new CombatSkill1(),
                        AttackSkill.Skill2 => new CombatSkill2(),
                        AttackSkill.Skill3 => new CombatSkill3(),
                        _ => new CombatSkill1()
                    };
                    return skill;
                }
                return DrawAttackSkill();
            case ActionType.Defend:
                return DefendSkill;
            case ActionType.Heal:
                return HealSkill;
            case ActionType.Dodge:
                return DodgeSkill;
            case ActionType.Counter:
                return CounterSkill;
            default:
                return null;
        }
    }
    
    public void CalculateSkillValues(BaseSkill skill)
    {
        int effectiveAttackLevel;
        if (HasWuDaoDuZun)
        {
            // 武道独尊强制使用攻击等级
            effectiveAttackLevel = FinalAttackLevel;
        }
        else if (HasWeiWuGuZhen)
        {
            // 魏武固阵使用防御等级
            effectiveAttackLevel = FinalDefenseLevel;
        }
        else
        {
            // 正常使用攻击等级
            effectiveAttackLevel = FinalAttackLevel;
        }
        skill.CalculateValues(effectiveAttackLevel, FinalDefenseLevel, Morale);
    }
    
    // 用于设置技能初始属性的钩子函数
    public virtual void SetSkillInitialProperties()
    {
        // 分别设置每个技能的初始属性
        SetAttackSkill1Properties();
        SetAttackSkill2Properties();
        SetAttackSkill3Properties();
        SetDefendSkillProperties();
        SetHealSkillProperties();
        SetDodgeSkillProperties();
        SetCounterSkillProperties();
    }
    
    // 设置攻击技能1的初始属性
    protected virtual void SetAttackSkill1Properties()
    {
        // 可以在子类中重写此方法来调整技能属性
    }
    
    // 设置攻击技能2的初始属性
    protected virtual void SetAttackSkill2Properties()
    {
        // 可以在子类中重写此方法来调整技能属性
    }
    
    // 设置攻击技能3的初始属性
    protected virtual void SetAttackSkill3Properties()
    {
        // 攻击等级每提升10（而非12）硬币点数+1，攻击等级修正+3
        // 为所有攻击技能3实例设置属性
        foreach (var skill in AttackSkills)
        {
            if (skill is CombatSkill3)
            {
                var combatSkill3 = (CombatSkill3)skill;
                // 已经在CombatSkill3构造函数中设置了默认值
                // 这里可以进行额外的调整
            }
        }
    }
    
    // 设置防御技能的初始属性
    protected virtual void SetDefendSkillProperties()
    {
        // 可以在子类中重写此方法来调整技能属性
    }
    
    // 设置治疗技能的初始属性
    protected virtual void SetHealSkillProperties()
    {
        // 可以在子类中重写此方法来调整技能属性
    }
    
    // 设置闪避技能的初始属性
    protected virtual void SetDodgeSkillProperties()
    {
        // 可以在子类中重写此方法来调整技能属性
    }
    
    // 设置反击技能的初始属性
    protected virtual void SetCounterSkillProperties()
    {
        // 可以在子类中重写此方法来调整技能属性
    }

    public virtual bool IsAlive => CurrentHealth > 0;

    public virtual bool ShouldDie()
    {
        return CurrentHealth <= 0;
    }

    public virtual void OnDeath()
    {
        CurrentHealth = 0;
    }

    public virtual int Attack(Character target)
    {
        int damage = AttackPower;
        if (target.IsDefending)
        {
            damage /= 2;
        }
        target.CurrentHealth -= damage;
        if (target.CurrentHealth < 0)
        {
            target.CurrentHealth = 0;
        }
        return damage;
    }

    public virtual void Heal()
    {
        int healAmount = MaxHealth / 4;
        CurrentHealth += healAmount;
        if (CurrentHealth > MaxHealth)
        {
            CurrentHealth = MaxHealth;
        }
    }

    public virtual void AdjustMorale(int amount)
    {
        Morale = Math.Max(-20, Math.Min(20, Morale + amount));
    }
    
    // 处理角色特定的战斗操作，如自动闪避
    public virtual ActionSlot HandleSpecialBattleAction(ActionSlot attackSlot, bool isDodgeTriggeredThisRound)
    {
        // 默认实现：不执行任何特殊操作
        return null;
    }
    
    // 重置属性到默认值
    public virtual void ResetAttributes()
    {
        DamageReduction = 0f;
        DamageIncrease = 0f;
        FinalDamageReduction = 0f;
        FinalDamageIncrease = 0f;
        AttackLevelAdjustment = 0;
        DefenseLevelAdjustment = 0;
        ShieldAdjustment = 0f;
        HasWeiWuGuZhen = false;
        HasWuDaoDuZun = false;
        CritRate = 0.5f; // 50%暴击率
        CritResistance = 0.4f; // 40%暴击抗性
        FinalCritRate = 0f; // 0%最终暴击率
        FinalCritResistance = 0f; // 0%最终暴击抗性
        CritDamage = 0.5f; // 50%暴击伤害
        CritDamageResistance = 0f; // 0%暴击伤害抗性
        // 重置速度修正值
        MinSpeedAdjustment = 0;
        MaxSpeedAdjustment = 0;
    }
    
    // 更新buff效果
    public virtual void UpdateBuffs(BuffHandler buffHandler = null)
    {
        // 重置属性
        ResetAttributes();
        
        // 重新计算基础攻击等级和防御等级，确保它们不会被意外覆盖
        AttackLevel = Math.Max(1, Level + AttackLevelModifier);
        DefenseLevel = Math.Max(1, Level + DefenseLevelModifier);
        
        // 由BuffHandler处理buff效果
        if (buffHandler != null)
        {
            buffHandler.UpdateBuffs(this);
        }
        
        // 计算最终攻击等级和防御等级
        FinalAttackLevel = Math.Max(1, AttackLevel + AttackLevelAdjustment);
        FinalDefenseLevel = Math.Max(1, DefenseLevel + DefenseLevelAdjustment);
        
        // 计算最终最小速度和最大速度
        FinalMinSpeed = MinSpeed + MinSpeedAdjustment;
        FinalMaxSpeed = MaxSpeed + MaxSpeedAdjustment;
        // 确保最终最小速度不高于最终最大速度
        FinalMinSpeed = Math.Min(FinalMinSpeed, FinalMaxSpeed);
    }
}
