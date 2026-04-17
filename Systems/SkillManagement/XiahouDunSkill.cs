using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Characters;
using TurnBasedRPG.Systems;
using TurnBasedRPG.Buffs.Buff;
using TurnBasedRPG.Buffs.Debuff;
using TurnBasedRPG.Characters.Skills.夏侯惇;

namespace TurnBasedRPG.Systems.SkillManagement;

public class XiahouDunSkill : BattleSystem
{
    public XiahouDunSkill(BattleSystem battleSystem) : base()
    {
        if (battleSystem != null)
        {
            Players = battleSystem.Players;
            Enemies = battleSystem.Enemies;
            PlayerSlots = battleSystem.PlayerSlots;
            EnemySlots = battleSystem.EnemySlots;
            _slotToCharacterMap = battleSystem._slotToCharacterMap;
            _characterShields = battleSystem._characterShields;
            BattleLog = battleSystem.BattleLog;
            Statistics = battleSystem.Statistics;
            _buffHandler = battleSystem._buffHandler;
        }
    }
    
    // 魏武固阵的具体实现方法
    public void HandleXiahouDunSkillEffects(Character xiahouDun, ActionSlot slot, Character target, BuffHandler buffHandler, List<Character> allCharacters)
    {
        if (xiahouDun == null || slot == null || target == null)
        {
            return;
        }
        
        // 处理横斩技能命中时的效果：使目标获得2级[脆弱]，持续2回合
        if (slot.SkillName == "横斩")
        {
            buffHandler.AddBuff(target, new 脆弱(2, 2));
            BattleLog.Add($"横斩使{target.Name}获得2级脆弱，持续2回合");
        }
        
        // 处理御甲鸣镝技能攻击后效果：使主要目标与所有次级目标获得[围城]，持续2回合
        if (slot.SkillName == "御甲鸣镝" && slot.IsLastCoin)
        {
            // 给主要目标添加围城状态
            buffHandler.AddBuff(target, new 围城(2, 1));
            BattleLog.Add($"御甲鸣镝使{target.Name}获得围城，持续2回合");
            
            // 给次级目标添加围城状态
            List<Character> secondaryTargets = GetSecondaryTargets(xiahouDun, target);
            if (secondaryTargets != null && secondaryTargets.Count > 0)
            {
                foreach (var secondaryTarget in secondaryTargets)
                {
                    if (secondaryTarget != null && !secondaryTarget.ShouldDie())
                    {
                        buffHandler.AddBuff(secondaryTarget, new 围城(2, 1));
                        BattleLog.Add($"御甲鸣镝使{secondaryTarget.Name}获得围城，持续2回合");
                    }
                }
            }
        }
        
        // 处理铁壁战吼技能最后一枚硬币命中时的效果
        if (slot.SkillName == "铁壁战吼" && slot.IsLastCoin)
        {
            // 计算额外伤害（25%的真实伤害）
            int additionalDamage = (int)(slot.TotalDamage * 0.25f);
            
            // 找到敌方随机目标
            List<Character> enemies = new List<Character>();
            if (target.IsAlly)
            {
                // 如果目标是己方，那么敌方是Enemies
                enemies.AddRange(Enemies);
            }
            else
            {
                // 如果目标是敌方，那么敌方是Players
                enemies.AddRange(Players);
            }
            
            // 排除当前目标
            enemies.Remove(target);
            
            if (enemies.Count > 0)
            {
                // 随机选择一个敌方目标
                Random randomEnemySelector = new Random();
                Character randomEnemy = enemies[randomEnemySelector.Next(enemies.Count)];
                
                // 对随机目标造成额外伤害
                if (additionalDamage > 0)
                {
                    // 使用ApplyDamage方法处理真实伤害
                    ApplyDamage(additionalDamage, randomEnemy, slot, isDirectDamage: true);
                    BattleLog.Add($"铁壁战吼对{randomEnemy.Name}造成{additionalDamage}点真实伤害");
                }
                
                // 使自身获得等同于该次额外伤害200%的护盾值
                int shieldAmount = (int)(additionalDamage * 2.0f);
                if (shieldAmount > 0)
                {
                    // 找到攻击者
                    Character tiebiAttacker = xiahouDun;
                    
                    if (tiebiAttacker != null)
                    {
                        AddShield(tiebiAttacker, shieldAmount);
                    }
                }
                
                // 使主要目标与附加伤害命中的目标获得3级[虚弱]，持续2回合
                buffHandler.AddBuff(target, new 虚弱(2, 3));
                if (randomEnemy != null)
                {
                    buffHandler.AddBuff(randomEnemy, new 虚弱(2, 3));
                }
                BattleLog.Add($"铁壁战吼使{target.Name}和{randomEnemy?.Name ?? "随机目标"}获得3级虚弱，持续2回合");
            }
        }
        
        // 处理窃国者侯技能攻击后效果：使所有目标获得2级[虚弱]，持续2回合
        if (slot.SkillName == "窃国者侯" && slot.IsLastCoin)
        {
            // 给主要目标添加虚弱状态
            buffHandler.AddBuff(target, new 虚弱(2, 2));
            BattleLog.Add($"窃国者侯使{target.Name}获得2级虚弱，持续2回合");
            
            // 给次级目标添加虚弱状态
            List<Character> secondaryTargets = GetSecondaryTargets(xiahouDun, target);
            if (secondaryTargets != null && secondaryTargets.Count > 0)
            {
                foreach (var secondaryTarget in secondaryTargets)
                {
                    if (secondaryTarget != null && !secondaryTarget.ShouldDie())
                    {
                        buffHandler.AddBuff(secondaryTarget, new 虚弱(2, 2));
                        BattleLog.Add($"窃国者侯使{secondaryTarget.Name}获得2级虚弱，持续2回合");
                    }
                }
            }
        }
    }
    
    private List<Character> GetSecondaryTargets(Character attacker, Character primaryTarget)
    {
        List<Character> secondaryTargets = new List<Character>();
        
        // 这里可以根据游戏规则实现获取次级目标的逻辑
        // 例如，获取除主要目标外的其他敌方单位
        
        List<Character> enemies = new List<Character>();
        if (attacker.IsAlly)
        {
            enemies.AddRange(Enemies);
        }
        else
        {
            enemies.AddRange(Players);
        }
        
        foreach (var enemy in enemies)
        {
            if (enemy != primaryTarget && enemy.CurrentHealth > 0)
            {
                secondaryTargets.Add(enemy);
            }
        }
        
        return secondaryTargets;
    }
}