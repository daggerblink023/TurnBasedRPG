using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using TurnBasedRPG;
using TurnBasedRPG.Buffs;
using TurnBasedRPG.Buffs.Debuff;
using TurnBasedRPG.Characters;

namespace TurnBasedRPG.Systems;

public enum ActionType
{
    None,
    Attack,
    Defend,
    Heal,
    Dodge,
    Counter
}

public enum AttackSkill
{
    Skill1,
    Skill2,
    Skill3
}

public enum DamageType
{
    Physical,
    Magic,
    True
}

public enum AttackType
{
    Slash,
    Blunt,
    Pierce,
    Spell
}

public class ActionSlot
{
    public ActionType Type { get; set; }
    public int BaseValue { get; set; }
    public int CoinValue { get; set; }
    public int CoinCount { get; set; }
    public int[] Coins { get; set; } // 0: 未投, 1: 正面, -1: 反面
    public int CurrentCoinIndex { get; set; }
    public int RandomizedValue { get; private set; }
    public bool IsDestroyed { get; set; }
    public bool IsCompleted { get; set; }
    public int Index { get; set; }
    public AttackSkill? SelectedSkill { get; set; }
    public string SkillName { get; set; } = "";
    public string ExtraEffects { get; set; } = "";
    public DamageType DamageType { get; set; } = DamageType.Physical;
    public AttackType AttackType { get; set; } = AttackType.Slash;
    public BaseSkill Skill { get; set; } = null;
    public List<AttackSkill> SkillPool { get; private set; }
    private List<AttackSkill> SkillSequence { get; set; } = new List<AttackSkill>(); // 技能池随机顺序序列
    private int CurrentSkillIndex { get; set; } = 0; // 当前技能在序列中的索引
    public AttackSkill? NextSkill { get; set; } // 下一回合的技能
    private static Random _random = new Random();
    private List<AttackSkill> _originalSkillSequence { get; set; } = new List<AttackSkill>();
    private int _originalCurrentSkillIndex { get; set; } = 0;
    public bool IsAlternativeSkillSelected { get; set; } = false;
    public bool IsLastCoin { get; set; } = false;
    public bool IsFirstCoin { get; set; } = false;
    public int TotalDamage { get; set; } = 0;
    public float BaseDamageMultiplier { get; set; } = 1.0f;
    public int CompetingPower { get; set; } = 0;
    public int CoinValueBonus { get; set; } = 0;
    public int Speed { get; set; } = 0; // 行动槽的速度值，与对应角色当前回合的速度值一致
    
    // 目标系统相关属性
    public ActionSlot TargetSlot { get; set; } = null; // 瞄准的目标行动槽
    public bool IsUnilateralAttack { get; set; } = false; // 是否为单方面攻击
    public int TargetSelectionOrder { get; set; } = 0; // 目标选择顺序（用于多对一冲突解决）
    public bool IsTargetLocked { get; set; } = false; // 目标是否被锁定（防止被覆盖）
    public bool IsAlly { get; set; } = false; // 是否为我方行动槽

    public ActionSlot(int index)
    {
        Index = index;
        Type = ActionType.None;
        BaseValue = 0;
        CoinValue = 0;
        CoinCount = 0;
        Coins = new int[0];
        CurrentCoinIndex = 0;
        RandomizedValue = 0;
        IsDestroyed = false;
        IsCompleted = false;
        SelectedSkill = null;
        SkillName = "";
        ExtraEffects = "";
        DamageType = DamageType.Physical;
        AttackType = AttackType.Slash;
        GenerateSkillSequence();
        // 保存原始技能序列
        _originalSkillSequence = new List<AttackSkill>(SkillSequence);
        _originalCurrentSkillIndex = CurrentSkillIndex;
    }

    public void GenerateSkillSequence()
    {
        SkillSequence = new List<AttackSkill>();
        // 3个1技能、2个2技能、1个3技能
        for (int i = 0; i < 3; i++) SkillSequence.Add(AttackSkill.Skill1);
        for (int i = 0; i < 2; i++) SkillSequence.Add(AttackSkill.Skill2);
        SkillSequence.Add(AttackSkill.Skill3);
        ShuffleSkillSequence();
        CurrentSkillIndex = 0;
        UpdateCurrentAndNextSkill();
    }

    private void ShuffleSkillSequence()
    {
        for (int i = SkillSequence.Count - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (SkillSequence[i], SkillSequence[j]) = (SkillSequence[j], SkillSequence[i]);
        }
    }

    public void UpdateCurrentAndNextSkill()
    {
        if (SkillSequence.Count > 0 && CurrentSkillIndex < SkillSequence.Count)
        {
            SelectedSkill = SkillSequence[CurrentSkillIndex];
            // 注意：不设置SkillName，因为会在AssignSlotsToCharacters方法中重新设置
            // 设置NextSkill - 始终显示当前回合中未被选入行动槽的那个技能
            if (CurrentSkillIndex + 1 < SkillSequence.Count)
            {
                // 如果当前序列中有下一个技能，使用它作为备选技能
                NextSkill = SkillSequence[CurrentSkillIndex + 1];
            }
            else
            {
                // 如果当前序列中没有下一个技能，从原始技能池中选择一个与当前技能不同的技能
                // 首先获取当前技能
                AttackSkill currentSkill = SelectedSkill.Value;
                // 从原始技能池中选择一个不同的技能
                List<AttackSkill> availableSkills = _originalSkillSequence.Where(skill => skill != currentSkill).ToList();
                if (availableSkills.Count > 0)
                {
                    // 随机选择一个不同的技能作为备选
                    NextSkill = availableSkills[_random.Next(availableSkills.Count)];
                }
                else
                {
                    // 如果所有技能都相同，使用相同的技能
                    NextSkill = currentSkill;
                }
            }
        }
        else
        {
            SelectedSkill = null;
            NextSkill = null;
            // 注意：不设置SkillName，因为会在AssignSlotsToCharacters方法中重新设置
        }
    }
    
    // 打乱列表的辅助方法
    private void ShuffleList<T>(List<T> list)
    {
        Random rng = new Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    public string GetNextSkillName()
    {
        if (NextSkill.HasValue)
        {
            return NextSkill.Value switch
            {
                AttackSkill.Skill1 => "技能 1",
                AttackSkill.Skill2 => "技能 2",
                AttackSkill.Skill3 => "技能 3",
                _ => ""
            };
        }
        return "";
    }
    
    public string GetSkillDisplayName(Character character, BuffHandler buffHandler = null)
    {
        // 检查沉默状态
        bool isSilenced = false;
        if (character != null && buffHandler != null)
        {
            isSilenced = buffHandler.CheckBuff<Silence>(character);
        }
        
        // 如果是沉默状态，只能使用技能1
        if (isSilenced)
        {
            if (Type == ActionType.Attack && character != null)
            {
                BaseSkill skill = character.GetSkillByActionType(ActionType.Attack, AttackSkill.Skill1);
                if (skill != null)
                {
                    return skill.Name;
                }
            }
        }
        
        // 攻击技能使用SelectedSkill获取技能名称
        if (Type == ActionType.Attack && SelectedSkill.HasValue && character != null)
        {
            BaseSkill skill = character.GetSkillByActionType(ActionType.Attack, SelectedSkill.Value);
            if (skill != null)
            {
                return skill.Name;
            }
        }
        // 闪避技能直接通过GetSkillByActionType获取
        else if (Type == ActionType.Dodge && character != null)
        {
            BaseSkill skill = character.GetSkillByActionType(ActionType.Dodge);
            if (skill != null)
            {
                return skill.Name;
            }
        }
        return SkillName;
    }
    
    public string GetNextSkillDisplayName(Character character, BuffHandler buffHandler = null)
    {
        // 检查沉默状态
        bool isSilenced = false;
        if (character != null && buffHandler != null)
        {
            isSilenced = buffHandler.CheckBuff<Silence>(character);
        }
        
        // 如果是沉默状态，不显示备选技能
        if (isSilenced)
        {
            return "";
        }
        
        // 无论当前选择了什么类型的技能，都尝试显示备选攻击技能
        if (NextSkill.HasValue && character != null)
        {
            BaseSkill skill = character.GetSkillByActionType(ActionType.Attack, NextSkill.Value);
            if (skill != null)
            {
                return skill.Name;
            }
        }
        return GetNextSkillName();
    }

    public void MoveToNextSkill()
    {
        if (SkillSequence.Count > 0 && CurrentSkillIndex < SkillSequence.Count)
        {
            // 保存当前技能和备选技能
            AttackSkill? currentSkill = SelectedSkill;
            AttackSkill? alternativeSkill = NextSkill;
            
            // 构建新的技能序列：保留两个技能，只是调整位置
            SkillSequence.Clear();
            
            if (IsAlternativeSkillSelected && alternativeSkill.HasValue && currentSkill.HasValue)
            {
                // 如果选择了备选技能，把备选技能作为新的当前技能，把原来的当前技能放在后面
                SkillSequence.Add(alternativeSkill.Value);
                SkillSequence.Add(currentSkill.Value);
            }
            else if (currentSkill.HasValue && alternativeSkill.HasValue)
            {
                // 如果选择了当前技能，保留当前技能作为新的当前技能，把原来的备选技能放在后面
                SkillSequence.Add(currentSkill.Value);
                SkillSequence.Add(alternativeSkill.Value);
            }
            else if (currentSkill.HasValue)
            {
                // 如果只有当前技能，添加它两次
                SkillSequence.Add(currentSkill.Value);
                SkillSequence.Add(currentSkill.Value);
            }
            else
            {
                // 如果都没有，直接生成新序列
                GenerateSkillSequence();
                IsAlternativeSkillSelected = false;
                return;
            }
            
            // 重置CurrentSkillIndex为0
            CurrentSkillIndex = 0;
            
            // 更新当前和下一个技能
            UpdateCurrentAndNextSkill();
        }
        else
        {
            GenerateSkillSequence();
        }
        
        // 重置IsAlternativeSkillSelected
        IsAlternativeSkillSelected = false;
    }
    
    public void ResetSkillSequence()
    {
        // 恢复原始技能序列
        SkillSequence = new List<AttackSkill>(_originalSkillSequence);
        CurrentSkillIndex = _originalCurrentSkillIndex;
        UpdateCurrentAndNextSkill();
    }
    
    public void SaveSkillSequence()
    {
        // 保存当前技能序列作为原始技能序列
        _originalSkillSequence = new List<AttackSkill>(SkillSequence);
        _originalCurrentSkillIndex = CurrentSkillIndex;
    }

    public virtual void SetAction(ActionType type, BaseSkill skill)
    {
        Type = type;
        Skill = skill;
        
        // 清除SelectedSkill，因为新技能不是攻击技能
        if (type != ActionType.Attack)
        {
            SelectedSkill = null;
        }
        
        if (skill != null)
        {
            BaseValue = skill.BaseValue;
            CoinValue = skill.CoinValue;
            CoinCount = skill.CoinCount;
            DamageType = skill.DamageType;
            AttackType = skill.AttackType;
            SkillName = skill.Name;
            ExtraEffects = skill.ExtraEffects;
            // 把BaseSkill的CompetingPower复制到ActionSlot的CompetingPower
            CompetingPower = skill.CompetingPower;
        }
        else if (type == ActionType.None)
        {
            BaseValue = 0;
            CoinValue = 0;
            CoinCount = 0;
            Coins = new int[0];
            SkillName = "";
            ExtraEffects = "";
        }
        
        ResetCoins();
        CalculateFinalValue();
    }

    protected virtual void ResetCoins()
    {
        Coins = new int[CoinCount];
        CurrentCoinIndex = 0;
    }

    public virtual void FlipCoins(int morale, Character character = null, BuffHandler buffHandler = null)
    {
        if (Coins != null)
        {
            // 检查是否有神威状态，如果有则所有硬币必定为正面
            bool hasShenWei = false;
            if (character != null && buffHandler != null)
            {
                hasShenWei = buffHandler.CheckBuff<TurnBasedRPG.Buffs.Buff.神威>(character);
            }
            
            for (int i = 0; i < Coins.Length; i++)
            {
                if (hasShenWei)
                {
                    // 有神威状态，所有硬币必定为正面
                    Coins[i] = 1;
                }
                else
                {
                    double probability = 0.5 + (morale * 0.02);
                    probability = Math.Max(0.1, Math.Min(0.9, probability));
                    Coins[i] = _random.NextDouble() < probability ? 1 : -1;
                }
            }
        }
    }

    public virtual int CalculateFinalValue()
    {
        int headsCount = 0;
        if (Coins != null)
        {
            for (int i = 0; i < Coins.Length; i++)
            {
                if (Coins[i] == 1)
                    headsCount++;
            }
        }
        
        RandomizedValue = BaseValue + (CoinValue * headsCount);
        
        // 应用特殊效果
        if (Type == ActionType.Defend)
        {
            RandomizedValue = (int)(RandomizedValue * 1.2);
        }
        else if (Type == ActionType.Heal)
        {
            RandomizedValue = (int)(RandomizedValue * 1.2);
        }
        
        return RandomizedValue;
    }

    public virtual bool HasRemainingCoins()
    {
        return Coins != null && Coins.Length > 0;
    }

    public virtual int GetCurrentCoinValue()
    {
        if (Coins != null && Coins.Length > 0)
        {
            return Coins[0] == 1 ? CoinValue : 0;
        }
        return 0;
    }

    public virtual void RemoveLastCoin()
    {
        if (Coins != null && Coins.Length > 0)
        {
            int[] newCoins = new int[Coins.Length - 1];
            Array.Copy(Coins, 0, newCoins, 0, newCoins.Length);
            Coins = newCoins;
        }
    }

    public void Reset()
    {
        Type = ActionType.None;
        BaseValue = 0;
        CoinValue = 0;
        CoinCount = 0;
        Coins = new int[0];
        CurrentCoinIndex = 0;
        RandomizedValue = 0;
        IsDestroyed = false;
        IsCompleted = false;
        SelectedSkill = null;
        SkillName = "";
        ExtraEffects = "";
        DamageType = DamageType.Physical;
        AttackType = AttackType.Slash;
        IsAlternativeSkillSelected = false;
        IsLastCoin = false;
        IsFirstCoin = false;
        TotalDamage = 0;
        BaseDamageMultiplier = 1.0f;
        CompetingPower = 0;
        CoinValueBonus = 0;
        Speed = 0;
        // 重置目标系统相关属性
        TargetSlot = null;
        IsUnilateralAttack = false;
        TargetSelectionOrder = 0;
        IsTargetLocked = false;
        // 注意：IsAlly不重置，因为这是在初始化时设置的固有属性
        GenerateSkillSequence();
    }

    public string GetTypeName()
    {
        return Type switch
        {
            ActionType.Attack => "Attack",
            ActionType.Defend => "Defend",
            ActionType.Heal => "Heal",
            ActionType.Dodge => "Dodge",
            _ => "None"
        };
    }

    public string GetSkillName()
    {
        return SkillName;
    }
    
    public string GetAttackTypeName()
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

    public Color GetTypeColor()
    {
        return Type switch
        {
            ActionType.Attack => Color.Red,
            ActionType.Defend => Color.Blue,
            ActionType.Heal => Color.Green,
            ActionType.Dodge => Color.Yellow,
            ActionType.Counter => Color.Purple, // 反击技能使用紫色
            _ => Color.Gray
        };
    }

    public Color GetSkillColor()
    {
        if (Type == ActionType.Counter)
        {
            return Color.Purple; // 反击技能使用紫色
        }
        return DamageType switch
        {
            DamageType.Physical => Color.Orange, // 物理伤害使用橙黄色
            DamageType.Magic => Color.Cyan, // 魔法伤害使用青色
            DamageType.True => Color.White, // 真实伤害使用白色
            _ => Color.Gray
        };
    }
    
    // 根据当前选择的技能更新DamageType
    public void UpdateDamageTypeFromSkill(Character character)
    {
        if (SelectedSkill.HasValue && character != null)
        {
            BaseSkill skill = character.GetSkillByActionType(ActionType.Attack, SelectedSkill.Value);
            if (skill != null)
            {
                DamageType = skill.DamageType;
            }
        }
    }
}
