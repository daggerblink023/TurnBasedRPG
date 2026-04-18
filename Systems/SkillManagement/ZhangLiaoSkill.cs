using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Characters;
using TurnBasedRPG.Characters.Allies;
using TurnBasedRPG.Systems;
using TurnBasedRPG.Buffs.Buff;
using TurnBasedRPG.Buffs.Debuff;
using TurnBasedRPG.Characters.Skills.张辽;

namespace TurnBasedRPG.Systems.SkillManagement;

public class ZhangLiaoSkill : BattleSystem
{
    public ZhangLiaoSkill(BattleSystem battleSystem) : base()
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
    
    public void TriggerZhangLiaoKuiPo(Character zhangLiao, Character target, bool isReducedDamage = false, BuffHandler buffHandler = null, List<Character> allCharacters = null, BattleSystem battleSystem = null)
    {
        string zhangLiaoTeamInfo = zhangLiao.IsAlly ? "-我方" : "-敌方";
        string targetTeamInfo = target.IsAlly ? "-我方" : "-敌方";
        
        if (buffHandler == null || allCharacters == null)
        {
            return;
        }
        
        // 确定攻击目标
        Character attackTarget = target;
        
        // 检查目标是否有效
        if (attackTarget == null)
        {
            List<Character> enemies = new List<Character>();
            if (zhangLiao.IsAlly)
            {
                enemies.AddRange(Enemies);
            }
            else
            {
                enemies.AddRange(Players);
            }
            
            // 筛选存活的敌人
            List<Character> aliveEnemies = enemies.FindAll(e => e.CurrentHealth > 0);
            if (aliveEnemies.Count > 0)
            {
                Random random = new Random();
                attackTarget = aliveEnemies[random.Next(aliveEnemies.Count)];
            }
            else
            {
                return;
            }
        }
        else if (attackTarget.CurrentHealth <= 0)
        {
            List<Character> enemies = new List<Character>();
            if (zhangLiao.IsAlly)
            {
                enemies.AddRange(Enemies);
            }
            else
            {
                enemies.AddRange(Players);
            }
            
            List<Character> aliveEnemies = enemies.FindAll(e => e.CurrentHealth > 0);
            if (aliveEnemies.Count > 0)
            {
                Random random = new Random();
                attackTarget = aliveEnemies[random.Next(aliveEnemies.Count)];
            }
            else
            {
                return;
            }
        }
        
        // 创建破溃技能
        BaseSkill kuiPoSkill = new 破溃();
        zhangLiao.CalculateSkillValues(kuiPoSkill);
        
        // 如果是同队魏国技能3触发的，降低60%最终伤害加成
        float savedFinalDamageIncrease = 0f;
        if (isReducedDamage)
        {
            savedFinalDamageIncrease = zhangLiao.FinalDamageIncrease;
            zhangLiao.FinalDamageIncrease -= 0.6f;
        }
        
        // 创建临时行动槽来处理破溃追击
        ActionSlot kuiPoSlot = new ActionSlot(0);
        kuiPoSlot.SetAction(ActionType.Attack, kuiPoSkill);
        
        // 投掷硬币
        kuiPoSlot.FlipCoins(zhangLiao.Morale);
        
        // 添加临时行动槽到映射中
        if (_slotToCharacterMap != null)
        {
            _slotToCharacterMap[kuiPoSlot] = zhangLiao;
        }
        
        // 记录这次攻击

        
        // 直接进行完整的伤害计算
        
        // 计算当前总的硬币点数
        int headsCount = 0;
        for (int i = 0; i < kuiPoSlot.Coins.Length; i++)
        {
            if (kuiPoSlot.Coins[i] == 1)
                headsCount++;
        }
        int finalValue = kuiPoSkill.BaseValue + (headsCount * kuiPoSkill.CoinValue);
        
        // 张辽的技能使用攻击等级计算
        int skillLevel = zhangLiao.FinalAttackLevel;
        
        // 计算skillLevelMultiplier（攻防等级修正乘区）
        double multiplierRate = 0.045;
        
        // 破溃是攻击技能，使用目标的防御等级进行计算
        int targetLevelForCalculation = attackTarget.FinalDefenseLevel;
        int levelDifference = skillLevel - targetLevelForCalculation;
        double skillLevelMultiplier = 1.5 + ((double)levelDifference * multiplierRate);
        skillLevelMultiplier = Math.Max(0.2, skillLevelMultiplier);
        
        // 一类增伤乘区damageMultiplier：(1+攻击者伤害提升-目标伤害减免)，最低0.2
        float damageMultiplier = (1 + zhangLiao.DamageIncrease - attackTarget.DamageReduction);
        damageMultiplier = Math.Max(0.2f, damageMultiplier);
        
        // 获取伤害种类抗性
        float damageTypeResistance = 1.0f;
        switch (kuiPoSkill.DamageType)
        {
            case DamageType.Physical:
                damageTypeResistance = attackTarget.PhysicalVulnerability;
                break;
            case DamageType.Magic:
                damageTypeResistance = attackTarget.MagicVulnerability;
                break;
            case DamageType.True:
                damageTypeResistance = attackTarget.TrueVulnerability;
                break;
        }
        
        // 获取攻击方式抗性
        float attackTypeResistance = 1.0f;
        switch (kuiPoSkill.AttackType)
        {
            case AttackType.Slash:
                attackTypeResistance = attackTarget.SlashVulnerability;
                break;
            case AttackType.Blunt:
                attackTypeResistance = attackTarget.BluntVulnerability;
                break;
            case AttackType.Pierce:
                attackTypeResistance = attackTarget.PierceVulnerability;
                break;
            case AttackType.Spell:
                attackTypeResistance = attackTarget.SpellVulnerability;
                break;
        }
        
        // 确保抗性值不低于0.1
        damageTypeResistance = Math.Max(0.1f, damageTypeResistance);
        attackTypeResistance = Math.Max(0.1f, attackTypeResistance);
        
        // 最终伤害乘区finalDamageMultiplier：(1+攻击者最终伤害提升-目标最终伤害减免)
        float finalDamageMultiplier = (1 + zhangLiao.FinalDamageIncrease - attackTarget.FinalDamageReduction);
        
        // 暴击判定
        bool isCriticalHit = false;
        float critDamageMultiplier = 1.0f;
        // 张辽初始暴击率100%
        float totalCritRate = Math.Min(1.0f, kuiPoSkill.CritRate + zhangLiao.CritRate + zhangLiao.FinalCritRate - attackTarget.CritResistance - attackTarget.FinalCritResistance);
        totalCritRate = Math.Max(0, totalCritRate);
        
        Random randomZLC = new Random();
        double randomValue = randomZLC.NextDouble();
        if (randomValue < totalCritRate)
        {
            isCriticalHit = true;
        }
        
        // 计算暴击伤害乘区
        if (isCriticalHit)
        {
            float skillCritDamage = kuiPoSkill.CritDamage + zhangLiao.CritDamage;
            float targetCritDamageResistance = attackTarget.CritDamageResistance;
            critDamageMultiplier = 1 + (skillCritDamage - targetCritDamageResistance);
            critDamageMultiplier = Math.Max(1.0f, critDamageMultiplier); // 不低于1
        }
        
        // 计算最终伤害
        int damage = (int)(finalValue * skillLevelMultiplier * damageMultiplier * finalDamageMultiplier * attackTypeResistance * damageTypeResistance * critDamageMultiplier);
        
        // 记录伤害计算前的护盾和血量值
        int shieldBefore = GetCharacterShield(attackTarget);
        int healthBefore = attackTarget.CurrentHealth;
        
        // 应用伤害
        ApplyDamage(damage, attackTarget, kuiPoSlot);
        
        // 添加伤害结算日志
        int shieldAfter = GetCharacterShield(attackTarget);
        int healthAfter = attackTarget.CurrentHealth;
        int shieldDamageTaken = shieldBefore - shieldAfter;
        int healthDamageTaken = healthBefore - healthAfter;
        int totalDamageTaken = shieldDamageTaken + healthDamageTaken;
        
        // 记录伤害统计
        if (battleSystem != null)
        {
            battleSystem.Statistics.RecordDamage(zhangLiao, kuiPoSkill.Name, shieldDamageTaken, healthDamageTaken);
        }
        else
        {
            Statistics.RecordDamage(zhangLiao, kuiPoSkill.Name, shieldDamageTaken, healthDamageTaken);
        }
        
        if (shieldDamageTaken > 0 && healthDamageTaken > 0)
        {

        }
        else if (shieldDamageTaken > 0)
        {

        }
        else if (healthDamageTaken > 0)
        {

        }
        else
        {

        }
        
        // 恢复最终伤害加成
        if (isReducedDamage)
        {
            zhangLiao.FinalDamageIncrease = savedFinalDamageIncrease;
        }
        
        // 处理张辽技能效果
        HandleZhangLiaoSkillEffects(zhangLiao, kuiPoSlot, attackTarget, buffHandler, allCharacters);
    }
    
    public void HandleZhangLiaoSkillEffects(Character zhangLiao, ActionSlot slot, Character target, BuffHandler buffHandler, List<Character> allCharacters)
    {
        if (zhangLiao == null || slot == null || target == null)
        {
            return;
        }
        
        if (!(zhangLiao is 张辽))
            return;
            
        张辽 zhangLiaoCharacter = (张辽)zhangLiao;
        
        // 处理霜戟技能
        if (slot.SkillName == "霜戟")
        {
            // [命中时]为目标施加1级持续3回合的[怖吓]
            buffHandler.AddBuff(target, new 怖吓(3, 1));
            
            // [攻击后]使自身获得1级[奔狼]，每回合至多触发一次
            if (slot.IsLastCoin && zhangLiaoCharacter.CanTriggerBenLangSkill1())
            {
                var benLangBuff = buffHandler.GetBuffs(zhangLiao).Find(b => b is 奔狼);
                if (benLangBuff != null)
                {
                    benLangBuff.Strength += 1;
                    benLangBuff.UpdateBuff(zhangLiao);
                }
                else
                {
                    buffHandler.AddBuff(zhangLiao, new 奔狼(1));
                }
                zhangLiaoCharacter.TriggerBenLangSkill1();
            }
            
            // 检查是否有破阵余威
            var poZhenBuff = buffHandler.GetBuffs(zhangLiao).Find(b => b is 破阵余威);
            if (poZhenBuff != null && slot.IsLastCoin)
            {
                // [最后一枚硬币命中时]额外对目标造成一次基于（最终点数/2）的真实伤害
                int headsCount = 0;
                for (int i = 0; i < slot.Coins.Length; i++)
                {
                    if (slot.Coins[i] == 1)
                        headsCount++;
                }
                int finalValue = slot.Skill.BaseValue + (headsCount * slot.Skill.CoinValue);
                int trueDamage = finalValue / 2;
                
                if (trueDamage > 0)
                {
                    ApplyDamage(trueDamage, target, slot, isDirectDamage: true);

                }
            }
        }
        
        // 处理破溃技能
        if (slot.SkillName == "破溃")
        {
            // 检查是否有破阵余威或神威
            var poZhenBuff = buffHandler.GetBuffs(zhangLiao).Find(b => b is 破阵余威);
            var shenWeiBuff = buffHandler.GetBuffs(zhangLiao).Find(b => b is 神威);
            
            // [最后一枚硬币命中时]为目标施加1级持续3回合的[怖吓]（持有破阵余威或神威时）
            if ((poZhenBuff != null || shenWeiBuff != null) && slot.IsLastCoin)
            {
                buffHandler.AddBuff(target, new 怖吓(3, 1));
            }
            
            // [攻击后]处理效果
            if (slot.IsLastCoin)
            {
                // 若自身的[奔狼]强度不低于3，额外为同队所有魏国武将施加1级持续2回合的[天啸]，每回合至多触发一次
                var benLangBuff = buffHandler.GetBuffs(zhangLiao).Find(b => b is 奔狼);
                if (benLangBuff != null && benLangBuff.Strength >= 3 && zhangLiaoCharacter.CanTriggerTianXiao())
                {
                    List<Character> allies = new List<Character>();
                    if (zhangLiao.IsAlly)
                    {
                        allies.AddRange(Players);
                    }
                    else
                    {
                        allies.AddRange(Enemies);
                    }
                    
                    foreach (var ally in allies)
                    {
                        if (ally.Faction == Faction.魏 && ally.CurrentHealth > 0)
                        {
                            buffHandler.AddBuff(ally, new 天啸(2, 1));
                        }
                    }
                    zhangLiaoCharacter.TriggerTianXiao();
                }
                
                // 使自身获得2级[奔狼]
                if (zhangLiaoCharacter.CanTriggerBenLangSkill2())
                {
                    if (benLangBuff != null)
                    {
                        benLangBuff.Strength += 2;
                        benLangBuff.UpdateBuff(zhangLiao);
                    }
                    else
                    {
                        buffHandler.AddBuff(zhangLiao, new 奔狼(2));
                    }
                    zhangLiaoCharacter.TriggerBenLangSkill2();
                }
            }
        }
        
        // 处理威震逍遥津技能
        if (slot.SkillName == "威震逍遥津")
        {
            // 标记已使用
            zhangLiaoCharacter.SetHasUsedWeiZhenXiaoYaoJin(true);
            
            // [使用时]使自身获得持续2回合的[神威]
            buffHandler.AddBuff(zhangLiao, new 神威(2));
            
            // 触发[使用时]效果：额外对敌方所有单位造成一次基于最终点数的真实伤害
            // 如果是立即使用（没有硬币），直接使用基础值
            int finalValue = slot.Skill.BaseValue;
            if (slot.Coins != null && slot.Coins.Length > 0)
            {
                int headsCount = 0;
                for (int i = 0; i < slot.Coins.Length; i++)
                {
                    if (slot.Coins[i] == 1)
                        headsCount++;
                }
                finalValue = slot.Skill.BaseValue + (headsCount * slot.Skill.CoinValue);
            }
            
            // 自身每损失1%生命，此伤害临时获得1%最终伤害提升
            float healthLossPercent = 1.0f - ((float)zhangLiao.CurrentHealth / zhangLiao.MaxHealth);
            float damageBonus = healthLossPercent * 1.0f;
            zhangLiao.FinalDamageIncrease += damageBonus;
            
            // 对敌方所有单位造成真实伤害
            List<Character> enemies = new List<Character>();
            if (zhangLiao.IsAlly)
            {
                enemies.AddRange(Enemies);
            }
            else
            {
                enemies.AddRange(Players);
            }
            
            foreach (var enemy in enemies)
            {
                if (enemy.CurrentHealth > 0)
                {
                    ApplyDamage(finalValue, enemy, slot, isDirectDamage: true);

                }
            }
            
            // 恢复最终伤害加成
            zhangLiao.FinalDamageIncrease -= damageBonus;
        }
    }
}
