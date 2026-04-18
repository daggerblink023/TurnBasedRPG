﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Characters;
using TurnBasedRPG.Systems;
using TurnBasedRPG.Buffs.Buff;
using TurnBasedRPG.Buffs.Debuff;
using TurnBasedRPG.Characters.Skills.曹仁;

namespace TurnBasedRPG.Characters.Allies;

public class 曹仁 : Character
{
    private bool _ironWallAppliedThisTurn = false;
    private List<(int slotIndex, int skillLevel)> _discardedSkills = new List<(int, int)>();
    public Character LastShieldHitAttacker { get; set; }
    
    public 曹仁(bool hasCustomConstructor = false, bool isAlly = true) : base("曹仁", 85, 6, 40, -3, 2, isAlly)
    {
        PassiveName = "决断-曹仁";
        PassiveSkill = "使自身获得[决断-曹仁]与[同仇之盾]，同队的其他魏国武将同样获得[同仇之盾]\n\n[同仇之盾]的状态强度等同于同队中除曹仁外持有[同仇之盾]的武将数量，上限为5\n\n[回合结束时]属于自身的每个行动槽会额外丢弃处于备选位置的技能，并将[决断-曹仁]的状态强度设置为被额外丢弃的技能中最高的技能等级";
        ShieldEffectiveness = 1.33f;
        Faction = Faction.魏;
        
        // 攻击方式易损
        SlashVulnerability = 0.8f;
        BluntVulnerability = 0.9f;
        PierceVulnerability = 1.3f;
        SpellVulnerability = 1.1f;
        
        // 伤害种类易损
        PhysicalVulnerability = 1.0f;
        MagicVulnerability = 0.9f;
        TrueVulnerability = 2.0f;
        
        // 速度范围
        MinSpeed = 2;
        MaxSpeed = 4;
    }
    
    protected override void InitializeSkills()
    {
        AttackSkills = new List<BaseSkill>();
        for (int i = 0; i < 3; i++) AttackSkills.Add(new 盾击());
        for (int i = 0; i < 2; i++) AttackSkills.Add(new 镇岳反攻());
        AttackSkills.Add(new 御甲鸣镝());
        ShuffleAttackSkills();
        
        DefendSkill = new 铁壁();
        HealSkill = null;
        DodgeSkill = null;
        CounterSkill = new CounterSkill(); // 使用默认的CounterSkill，默守蓄锋通过特殊方式触发
        CounterSkill.CanBeSelected = false; // 曹仁的反击技能不能被主动选择
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
                        AttackSkill.Skill1 => new 盾击(),
                        AttackSkill.Skill2 => new 镇岳反攻(),
                        AttackSkill.Skill3 => new 御甲鸣镝(),
                        _ => new 盾击()
                    };
                    return skill;
                }
                return DrawAttackSkill();
            case ActionType.Defend:
                return DefendSkill;
            case ActionType.Heal:
                return null;
            case ActionType.Dodge:
                return null;
            case ActionType.Counter:
                return CounterSkill;
            default:
                return null;
        }
    }
    
    public void RecordDiscardedSkill(int slotIndex, int skillLevel)
    {
        _discardedSkills.Add((slotIndex, skillLevel));
    }
    
    public void OnTurnStart(BuffHandler buffHandler, List<Character> allCharacters, BattleSystem battleSystem)
    {
        _ironWallAppliedThisTurn = false;
        _discardedSkills.Clear();
        
        // 检查并添加决断-曹仁状态（仅在不存在时添加）
        if (!buffHandler.CheckBuff<决断_曹仁>(this))
        {
            buffHandler.AddBuff(this, new 决断_曹仁(null, 1));
        }
        
        // 处理决断-曹仁的回合开始效果
        var jueDuanBuff = buffHandler.GetBuffs(this).Find(b => b is 决断_曹仁);
        if (jueDuanBuff is 决断_曹仁)
        {
            ((决断_曹仁)jueDuanBuff).OnTurnStart(this, buffHandler, battleSystem, allCharacters);
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
            
            var existingBuff = buffHandler.GetBuffs(this).Find(b => b is 决断_曹仁);
            if (existingBuff is 决断_曹仁)
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
    
    public void HandleDefendSkillEffect(BuffHandler buffHandler)
    {
        if (!_ironWallAppliedThisTurn)
        {
            buffHandler.AddBuff(this, new Endurance(2, 2));
            _ironWallAppliedThisTurn = true;
        }
    }
}
