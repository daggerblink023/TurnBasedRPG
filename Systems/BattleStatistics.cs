using System;
using System.Collections.Generic;
using System.Linq;
using TurnBasedRPG.Characters;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Systems;

public class BattleStatistics
{
    // 角色统计数据
    public class CharacterStats
    {
        public string CharacterName { get; set; }
        public bool IsAlly { get; set; }
        public int CurrentRoundTotalDamage { get; set; } = 0;
        public int AllRoundsTotalDamage { get; set; } = 0;
        public int CurrentRoundTotalHealing { get; set; } = 0;
        public int AllRoundsTotalHealing { get; set; } = 0;
        public int CurrentRoundTotalShield { get; set; } = 0;
        public int AllRoundsTotalShield { get; set; } = 0;
        public int CurrentRoundShieldDamage { get; set; } = 0;
        public int CurrentRoundHealthDamage { get; set; } = 0;
        
        // 技能统计数据
        public Dictionary<string, SkillStats> SkillStats { get; set; } = new Dictionary<string, SkillStats>();
    }
    
    // 技能统计数据
    public class SkillStats
    {
        public int ReleaseCount { get; set; } = 0;
        public int TotalShieldDamage { get; set; } = 0;
        public int TotalHealthDamage { get; set; } = 0;
        public int TotalHealing { get; set; } = 0;
        public int TotalShield { get; set; } = 0;
    }
    
    // 所有角色的统计数据
    private Dictionary<string, CharacterStats> _characterStats = new Dictionary<string, CharacterStats>();
    private int _currentRound = 1;
    
    // 获取或创建角色统计数据
    private CharacterStats GetOrCreateCharacterStats(Character character)
    {
        string key = $"{character.Name}-{(character.IsAlly ? "我方" : "敌方")}";
        if (!_characterStats.ContainsKey(key))
        {
            _characterStats[key] = new CharacterStats
            {
                CharacterName = character.Name,
                IsAlly = character.IsAlly
            };
        }
        return _characterStats[key];
    }
    
    // 获取或创建技能统计数据
    private SkillStats GetOrCreateSkillStats(CharacterStats characterStats, string skillName)
    {
        if (!characterStats.SkillStats.ContainsKey(skillName))
        {
            characterStats.SkillStats[skillName] = new SkillStats();
        }
        return characterStats.SkillStats[skillName];
    }
    
    // 记录伤害
    public void RecordDamage(Character source, string skillName, int shieldDamage, int healthDamage)
    {
        if (source == null) return;
        
        var characterStats = GetOrCreateCharacterStats(source);
        var skillStats = GetOrCreateSkillStats(characterStats, skillName);
        
        // 更新角色总伤害
        characterStats.CurrentRoundTotalDamage += shieldDamage + healthDamage;
        characterStats.AllRoundsTotalDamage += shieldDamage + healthDamage;
        characterStats.CurrentRoundShieldDamage += shieldDamage;
        characterStats.CurrentRoundHealthDamage += healthDamage;
        
        // 更新技能伤害
        skillStats.ReleaseCount++;
        skillStats.TotalShieldDamage += shieldDamage;
        skillStats.TotalHealthDamage += healthDamage;
    }
    
    // 记录治疗
    public void RecordHealing(Character source, string skillName, int healingAmount)
    {
        if (source == null) return;
        
        var characterStats = GetOrCreateCharacterStats(source);
        var skillStats = GetOrCreateSkillStats(characterStats, skillName);
        
        // 更新角色总治疗
        characterStats.CurrentRoundTotalHealing += healingAmount;
        characterStats.AllRoundsTotalHealing += healingAmount;
        
        // 更新技能治疗
        skillStats.ReleaseCount++;
        skillStats.TotalHealing += healingAmount;
    }
    
    // 记录护盾
    public void RecordShield(Character source, string skillName, int shieldAmount)
    {
        if (source == null) return;
        
        var characterStats = GetOrCreateCharacterStats(source);
        var skillStats = GetOrCreateSkillStats(characterStats, skillName);
        
        // 更新角色总护盾
        characterStats.CurrentRoundTotalShield += shieldAmount;
        characterStats.AllRoundsTotalShield += shieldAmount;
        
        // 更新技能护盾
        skillStats.ReleaseCount++;
        skillStats.TotalShield += shieldAmount;
    }
    
    // 增加回合数
    public void NextRound()
    {
        _currentRound++;
        
        // 重置本回合统计数据
        foreach (var stats in _characterStats.Values)
        {
            stats.CurrentRoundTotalDamage = 0;
            stats.CurrentRoundTotalHealing = 0;
            stats.CurrentRoundTotalShield = 0;
            stats.CurrentRoundShieldDamage = 0;
            stats.CurrentRoundHealthDamage = 0;
        }
    }
    
    // 输出统计信息
    public void OutputStatistics()
    {
        Game1.Log($"当前回合：{_currentRound}");
        Game1.Log("");
        
        foreach (var kvp in _characterStats)
        {
            var stats = kvp.Value;
            string characterKey = kvp.Key;
            
            // 输出角色总贡献
            Game1.Log($"{characterKey}");
            Game1.Log($"本回合总伤害：{stats.CurrentRoundTotalDamage} 所有回合总伤害：{stats.AllRoundsTotalDamage}");
            Game1.Log($"本回合总治疗：{stats.CurrentRoundTotalHealing} 所有回合总治疗：{stats.AllRoundsTotalHealing}");
            Game1.Log($"本回合总护盾施加：{stats.CurrentRoundTotalShield} 所有回合总护盾施加：{stats.AllRoundsTotalShield}");
            Game1.Log($"本回合总护盾伤害：{stats.CurrentRoundShieldDamage} 本回合总血量伤害：{stats.CurrentRoundHealthDamage}");
            
            // 输出技能统计
            foreach (var skillKvp in stats.SkillStats)
            {
                var skillStats = skillKvp.Value;
                string skillName = skillKvp.Key;
                
                Game1.Log($"{skillName} 释放次数：{skillStats.ReleaseCount} 总护盾伤害：{skillStats.TotalShieldDamage} 总血量伤害：{skillStats.TotalHealthDamage} 施加治疗：{skillStats.TotalHealing} 施加护盾：{skillStats.TotalShield}");
            }
            
            Game1.Log("");
        }
    }
}