using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Characters;
using TurnBasedRPG.Systems;
using TurnBasedRPG.Buffs.Buff;
using TurnBasedRPG.Buffs.Debuff;
using TurnBasedRPG.Characters.Skills.张辽;

namespace TurnBasedRPG.Characters.Allies;

public class 张辽 : Character
{
    private List<(int slotIndex, int skillLevel)> _discardedSkills = new List<(int, int)>();
    private BuffHandler _buffHandler;
    private int _currentKuiPoTriggerThisTurn = 0; // 本回合已触发破溃次数
    private int _maxKuiPoTriggerPerTurn = 1; // 每回合至多触发1次破溃
    private bool _hasUsedWeiZhenXiaoYaoJin = false; // 是否已使用过威震逍遥津
    private bool _diedFromLethalDamage = false; // 是否因致死伤害触发威震逍遥津
    private bool _isInShenWeiState = false; // 是否在神威状态
    private int _benLangSkill1TriggerCount = 0; // 本回合霜戟触发奔狼次数
    private int _benLangSkill2TriggerCount = 0; // 本回合破溃触发奔狼次数
    private int _tianXiaoTriggerCount = 0; // 本回合天啸触发次数
    private int _jiXingCritRateTriggerCount = 0; // 本回合疾行闪避成功触发暴击率次数
    private int _healthBeforeLethalDamage = 0; // 致死伤害前的生命值
    
    public 张辽(bool hasCustomConstructor = false, bool isAlly = true) : base("张辽", 73, 3.5f, 40, 2, -2, isAlly)
    {
        PassiveName = "决断-张辽";
        PassiveSkill = "使自身获得[决断-张辽]与[武道独尊]\n\n张辽的初始暴击率为100%，结算伤害时每溢出1%最终暴击率会临时增加1%暴击伤害\n\n同队魏国武将的技能3命中敌方单位时，张辽会使用[破溃]对该目标进行单方面攻击，此次[破溃]的最终伤害加成降低60%。每回合至多触发一次\n\n[回合结束时]属于自身的每个行动槽会额外丢弃处于备选位置的技能，并将[决断-张辽]的状态强度设置为被额外丢弃的技能中最高的技能等级";
        ShieldEffectiveness = 1.0f;
        Faction = Faction.魏;
        
        // 攻击方式易伤
        SlashVulnerability = 0.8f;
        BluntVulnerability = 1.2f;
        PierceVulnerability = 0.9f;
        SpellVulnerability = 1.3f;
        
        // 伤害类型易伤
        PhysicalVulnerability = 0.75f;
        MagicVulnerability = 1.4f;
        TrueVulnerability = 2.0f;
        
        // 张辽初始暴击率100%
        CritRate = 1.0f;
        
        // 速度范围
        MinSpeed = 5;
        MaxSpeed = 8;
    }
    
    protected override void InitializeSkills()
    {
        AttackSkills = new List<BaseSkill>();
        for (int i = 0; i < 3; i++) AttackSkills.Add(new 霜戟());
        for (int i = 0; i < 2; i++) AttackSkills.Add(new 破溃());
        AttackSkills.Add(new 威震逍遥津());
        ShuffleAttackSkills();
        
        DefendSkill = null;
        HealSkill = null;
        DodgeSkill = null;
        CounterSkill = null;
    }
    
    public override BaseSkill GetSkillByActionType(ActionType actionType, AttackSkill? attackSkill = null)
    {
        switch (actionType)
        {
            case ActionType.Attack:
                if (attackSkill.HasValue)
                {
                    BaseSkill skill = attackSkill.Value switch
                    {
                        AttackSkill.Skill1 => new 霜戟(),
                        AttackSkill.Skill2 => new 破溃(),
                        AttackSkill.Skill3 => _hasUsedWeiZhenXiaoYaoJin ? new 破溃() : new 威震逍遥津(),
                        _ => new 霜戟()
                    };
                    return skill;
                }
                return DrawAttackSkill();
            case ActionType.Defend:
                return null;
            case ActionType.Heal:
                return null;
            case ActionType.Dodge:
                return new 疾行();
            case ActionType.Counter:
                return null;
            default:
                return null;
        }
    }
    
    public void RecordDiscardedSkill(int slotIndex, int skillLevel)
    {
        _discardedSkills.Add((slotIndex, skillLevel));
    }
    
    public void SetBuffHandler(BuffHandler buffHandler)
    {
        _buffHandler = buffHandler;
    }
    
    public void OnTurnStart(BuffHandler buffHandler, List<Character> allCharacters, BattleSystem battleSystem)
    {
        _discardedSkills.Clear();
        _currentKuiPoTriggerThisTurn = 0;
        _benLangSkill1TriggerCount = 0;
        _benLangSkill2TriggerCount = 0;
        _tianXiaoTriggerCount = 0;
        _jiXingCritRateTriggerCount = 0;
        
        // 检查并添加决断-张辽状态（仅在不存在时添加）
        if (!buffHandler.CheckBuff<决断_张辽>(this))
        {
            buffHandler.AddBuff(this, new 决断_张辽(null, 1));
        }
        
        // 检查并添加武道独尊状态（仅在不存在时添加）
        if (!buffHandler.CheckBuff<武道独尊>(this))
        {
            buffHandler.AddBuff(this, new 武道独尊());
        }
        
        // 处理决断-张辽的回合开始效果
        var jueDuanBuff = buffHandler.GetBuffs(this).Find(b => b is 决断_张辽);
        if (jueDuanBuff is 决断_张辽)
        {
            ((决断_张辽)jueDuanBuff).OnTurnStart(this, buffHandler, battleSystem);
        }
    }
    
    public void OnTurnEnd(BuffHandler buffHandler, List<Character> allCharacters)
    {
        if (_discardedSkills.Count > 0)
        {
            int highestSkillLevel = 1;
            
            foreach (var (slotIndex, skillLevel) in _discardedSkills)
            {
                if (skillLevel > highestSkillLevel)
                {
                    highestSkillLevel = skillLevel;
                }
            }
            
            // 检查同队是否有曹操的决断-曹操buff
            bool hasCaoCaoJueDuan = false;
            if (allCharacters != null)
            {
                foreach (var chara in allCharacters)
                {
                    if (chara.Faction == Faction.魏 && chara.IsAlly == this.IsAlly)
                    {
                        var caoCaoJueDuan = buffHandler.GetBuffs(chara).Find(b => b is 决断_曹操);
                        if (caoCaoJueDuan != null)
                        {
                            hasCaoCaoJueDuan = true;
                            break;
                        }
                    }
                }
            }
            
            var existingBuff = buffHandler.GetBuffs(this).Find(b => b is 决断_张辽);
            if (existingBuff is 决断_张辽)
            {
                if (hasCaoCaoJueDuan)
                {
                    // 同队有曹操的决断-曹操，最小层数+1（2），最大层数+1（4），强度+1
                    existingBuff.Strength = Math.Clamp(highestSkillLevel + 1, 2, 4);
                }
                else
                {
                    // 没有曹操的决断-曹操，正常设置
                    existingBuff.Strength = Math.Clamp(highestSkillLevel, 1, 3);
                }
            }
        }
    }
    
    public void OnWeiSkill3Hit(Character target, BattleSystem battleSystem, List<Character> allCharacters, Character attacker)
    {
        if (attacker == this)
            return;
            
        if (attacker.Faction == Faction.魏 && _currentKuiPoTriggerThisTurn < _maxKuiPoTriggerPerTurn)
        {
            Game1.Log($"[张辽-触发判定] 同队魏国武将技能3命中，触发破溃攻击");
            _currentKuiPoTriggerThisTurn++;
            // 破溃触发将在BattleSystem中处理
        }
    }
    
    public bool CanTriggerKuiPo()
    {
        return _currentKuiPoTriggerThisTurn < _maxKuiPoTriggerPerTurn;
    }
    
    public void TriggerKuiPo()
    {
        _currentKuiPoTriggerThisTurn++;
    }
    
    public bool CanTriggerBenLangSkill1()
    {
        return _benLangSkill1TriggerCount < 1;
    }
    
    public void TriggerBenLangSkill1()
    {
        _benLangSkill1TriggerCount++;
    }
    
    public bool IsInShenWeiState()
    {
        return _isInShenWeiState;
    }
    
    public void SetInShenWeiState(bool value)
    {
        _isInShenWeiState = value;
    }
    
    public bool CanTriggerBenLangSkill2()
    {
        // 如果在神威状态，解除奔狼层数叠加限制
        if (_isInShenWeiState)
        {
            return true;
        }
        
        var poZhenBuff = _buffHandler?.GetBuffs(this).Find(b => b is 破阵余威);
        int maxTrigger = poZhenBuff != null ? 2 : 1;
        return _benLangSkill2TriggerCount < maxTrigger;
    }
    
    public void TriggerBenLangSkill2()
    {
        _benLangSkill2TriggerCount++;
    }
    
    public bool CanTriggerTianXiao()
    {
        return _tianXiaoTriggerCount < 1;
    }
    
    public void TriggerTianXiao()
    {
        _tianXiaoTriggerCount++;
    }
    
    public bool CanTriggerJiXingCritRate()
    {
        return _jiXingCritRateTriggerCount < 2;
    }
    
    public void TriggerJiXingCritRate()
    {
        _jiXingCritRateTriggerCount++;
    }
    
    public bool HasUsedWeiZhenXiaoYaoJin()
    {
        return _hasUsedWeiZhenXiaoYaoJin;
    }
    
    public void SetHasUsedWeiZhenXiaoYaoJin(bool value)
    {
        _hasUsedWeiZhenXiaoYaoJin = value;
    }
    
    public bool DiedFromLethalDamage()
    {
        return _diedFromLethalDamage;
    }
    
    public void SetDiedFromLethalDamage(bool value)
    {
        _diedFromLethalDamage = value;
    }
    
    public int GetHealthBeforeLethalDamage()
    {
        return _healthBeforeLethalDamage;
    }
    
    public void SetHealthBeforeLethalDamage(int value)
    {
        _healthBeforeLethalDamage = value;
    }
}
