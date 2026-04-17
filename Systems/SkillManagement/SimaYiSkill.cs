using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Characters;
using TurnBasedRPG.Systems;
using TurnBasedRPG.Buffs.Buff;

namespace TurnBasedRPG.Systems.SkillManagement;

public class SimaYiSkill : BattleSystem
{
    public SimaYiSkill(BattleSystem battleSystem) : base()
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
    
    public void HandleLangguTrigger(Character target, ActionSlot slot, bool isDirectDamage, int shieldDamage, int healthDamage, bool isShieldBroken, bool isLastCoinHit = false, BuffHandler buffHandler = null, List<Character> allCharacters = null, Character attacker = null)
    {
        if (BuffHandler == null)
        {
            return;
        }
        
        // 初始化攻击者
        bool isEnemyAttack = false;
        
        if (attacker == null && slot != null && _slotToCharacterMap != null && _slotToCharacterMap.ContainsKey(slot))
        {
            attacker = _slotToCharacterMap[slot];
        }
        
        // 找到所有司马懿（包括友方和敌方）
        List<Character> allSimaYis = new List<Character>();
        if (allCharacters != null)
        {
            foreach (var chara in allCharacters)
            {
                if (chara is TurnBasedRPG.Characters.Allies.司马懿)
                {
                    allSimaYis.Add(chara);
                }
            }
        }
        
        if (allSimaYis.Count == 0)
        {
            return;
        }
        
        // 处理每个司马懿的狼顾触发
        foreach (var simaYi in allSimaYis)
        {
            bool isCounterSkill = IsCounterSkill(slot);
            
            // 判断是否是敌方攻击
            if (attacker != null && attacker.IsAlly != simaYi.IsAlly)
            {
                isEnemyAttack = true;
            }
            else
            {
                isEnemyAttack = false;
            }
            
            // 条件1：自身被敌方的非反击技能命中
            bool isSelfHit = (target != null && simaYi != null && target.Name == simaYi.Name && target.IsAlly == simaYi.IsAlly);
            
            // 条件2：同队魏国武将的护盾被敌方的非反击技能击破
            bool isShieldBrokenWeiAlly = (isShieldBroken && target.Faction == Faction.魏 && target.IsAlly == simaYi.IsAlly);
            
            // 条件3：自身的非反击技能命中敌方
            bool isSelfSkillHit = (attacker != null && simaYi != null && attacker.Name == simaYi.Name && attacker.IsAlly == simaYi.IsAlly && !isCounterSkill && target.IsAlly != simaYi.IsAlly);
            
            // 调用司马懿的CanTriggerLanggu方法
            var simaYiObj = simaYi as TurnBasedRPG.Characters.Allies.司马懿;
            if (simaYiObj != null)
            {
                bool shouldTrigger = simaYiObj.CanTriggerLanggu(attacker, isEnemyAttack, isCounterSkill, isSelfHit, isShieldBrokenWeiAlly, isSelfSkillHit, isLastCoinHit);
                
                if (shouldTrigger)
                {
                    TriggerLangguCounter(simaYiObj, attacker, buffHandler, allCharacters);
                }
            }
        }
    }
    
    private void TriggerLangguCounter(TurnBasedRPG.Characters.Allies.司马懿 simaYi, Character attacker, BuffHandler buffHandler, List<Character> allCharacters)
    {
        if (BuffHandler == null)
        {
            return;
        }
        
        // 确定反击目标
        Character counterTarget = null;
        
        // 第一步：尝试使用最后一次触发狼顾的攻击者
        Character lastAttacker = simaYi.GetLastLangguAttacker();
        if (lastAttacker != null && lastAttacker.CurrentHealth > 0)
        {
            counterTarget = lastAttacker;
        }
        
        // 第二步：如果目标已死亡或不存在，使用传入的攻击者
        if (counterTarget == null || counterTarget.CurrentHealth <= 0)
        {
            if (attacker != null && attacker.CurrentHealth > 0 && attacker.IsAlly != simaYi.IsAlly)
            {
                // 确保攻击者不是司马懿自己
                if (attacker.Name != simaYi.Name || attacker.IsAlly != simaYi.IsAlly)
                {
                    counterTarget = attacker;
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }
        
        // 第三步：如果目标已死亡或不存在，随机选择一个存活且可被选中的敌方单位
        if (counterTarget == null || counterTarget.CurrentHealth <= 0)
        {
            List<Character> enemies = new List<Character>();
            if (simaYi.IsAlly)
            {
                enemies.AddRange(Enemies);
            }
            else
            {
                enemies.AddRange(Players);
            }
            
            // 筛选存活且可被选中的敌人
            List<Character> aliveEnemies = enemies.FindAll(e => e.CurrentHealth > 0);
            if (aliveEnemies.Count > 0)
            {
                // 随机选择一个存活的敌人
                Random random = new Random();
                counterTarget = aliveEnemies[random.Next(aliveEnemies.Count)];
            }
            else
            {
                return;
            }
        }
        
        // 处理攻击前效果
        // 消耗50%的护盾，提升基础点数
        int currentShield = GetCharacterShield(simaYi);
        int shieldToConsume = (int)(currentShield * 0.5f);
        
        if (shieldToConsume > 0)
        {
            _characterShields[simaYi] = currentShield - shieldToConsume;
        }
        
        // 创建狼顾技能
        BaseSkill counterSkill = new TurnBasedRPG.Characters.Skills.司马懿.狼顾();
        simaYi.CalculateSkillValues(counterSkill);
        
        // 每消耗15点护盾，基础点数+1
        int baseValueBonus = shieldToConsume / 15;
        counterSkill.BaseValue += baseValueBonus;
        
        // 创建临时行动槽来处理反击
        ActionSlot counterSlot = new ActionSlot(0);
        counterSlot.SetAction(ActionType.Attack, counterSkill);
        
        // 投掷硬币
        counterSlot.FlipCoins(simaYi.Morale);
        
        // 添加临时行动槽到映射中，以便找到攻击者
        if (_slotToCharacterMap != null)
        {
            _slotToCharacterMap[counterSlot] = simaYi;
        }
        
        // 记录这次攻击
        BattleLog.Add($"司马懿{(simaYi.IsAlly ? "-我方" : "-敌方")}的狼顾之相暴露，对{counterTarget.Name}{(counterTarget.IsAlly ? "-我方" : "-敌方")}发动了狼顾！");
        
        // 直接进行完整的伤害计算
        // 计算当前总的硬币点数
        int headsCount = 0;
        for (int i = 0; i < counterSlot.Coins.Length; i++)
        {
            if (counterSlot.Coins[i] == 1)
                headsCount++;
        }
        int finalValue = counterSkill.BaseValue + (headsCount * counterSkill.CoinValue);
        
        // 确定攻防等级（狼顾使用防御等级计算）
        int skillLevel = simaYi.FinalDefenseLevel;
        
        // 计算skillLevelMultiplier（攻防等级修正乘区）（狼顾是反击技能，使用4.5%倍率）
        double multiplierRate = 0.045;
        
        // 狼顾是反击技能，使用目标的防御等级进行计算
        int targetLevelForCalculation = counterTarget.FinalDefenseLevel;
        int levelDifference = skillLevel - targetLevelForCalculation;
        double skillLevelMultiplier = 1.0 + ((double)levelDifference * multiplierRate);
        skillLevelMultiplier = Math.Max(0.2, skillLevelMultiplier);
        
        // 一类增伤乘区damageMultiplier：(1+攻击者伤害提升-目标伤害减免)，最低0.2
        float damageMultiplier = (1 + simaYi.DamageIncrease - counterTarget.DamageReduction);
        damageMultiplier = Math.Max(0.2f, damageMultiplier);
        
        // 获取伤害种类抗性
        float damageTypeResistance = 1.0f;
        switch (counterSkill.DamageType)
        {
            case DamageType.Physical:
                damageTypeResistance = counterTarget.PhysicalVulnerability;
                break;
            case DamageType.Magic:
                damageTypeResistance = counterTarget.MagicVulnerability;
                break;
            case DamageType.True:
                damageTypeResistance = counterTarget.TrueVulnerability;
                break;
        }
        
        // 获取攻击方式抗性
        float attackTypeResistance = 1.0f;
        switch (counterSkill.AttackType)
        {
            case AttackType.Slash:
                attackTypeResistance = counterTarget.SlashVulnerability;
                break;
            case AttackType.Blunt:
                attackTypeResistance = counterTarget.BluntVulnerability;
                break;
            case AttackType.Pierce:
                attackTypeResistance = counterTarget.PierceVulnerability;
                break;
            case AttackType.Spell:
                attackTypeResistance = counterTarget.SpellVulnerability;
                break;
        }
        
        // 确保抗性值不低于0.1
        damageTypeResistance = Math.Max(0.1f, damageTypeResistance);
        attackTypeResistance = Math.Max(0.1f, attackTypeResistance);
        
        // 最终伤害乘区finalDamageMultiplier：(1+攻击者最终伤害提升-目标最终伤害减免)
        float finalDamageMultiplier = (1 + simaYi.FinalDamageIncrease - counterTarget.FinalDamageReduction);
        
        // 司马懿的最终增伤低于50%时，提升至50%
        if (simaYi.FinalDamageIncrease < 0.5f)
        {
            finalDamageMultiplier = (1 + 0.5f - counterTarget.FinalDamageReduction);
        }
        
        // 暴击判定
        bool isCriticalHit = false;
        float critDamageMultiplier = 1.0f;
        // 狼顾的攻击者是司马懿
        Character langguAttacker = simaYi;
        if (langguAttacker != null)
        {
            // 计算暴击概率
            float skillCritRate = counterSkill.CritRate;
            float targetCritResistance = counterTarget.CritResistance;
            float firstStepCritRate = Math.Max(0, skillCritRate - targetCritResistance);
            float finalCritRateStep = counterSkill.FinalCritRate - counterTarget.FinalCritResistance;
            float totalCritRate = Math.Max(0, firstStepCritRate + finalCritRateStep);
            totalCritRate = Math.Min(totalCritRate, 1.0f); // 超出100%视为100%
            
            // 按概率判定是否暴击
            Random langguRandom = new Random();
            double randomValue = langguRandom.NextDouble();
            if (randomValue < totalCritRate)
            {
                isCriticalHit = true;
            }
            
            // 计算暴击伤害乘区
            if (isCriticalHit)
            {
                float skillCritDamage = counterSkill.CritDamage;
                float targetCritDamageResistance = counterTarget.CritDamageResistance;
                critDamageMultiplier = 1 + (skillCritDamage - targetCritDamageResistance);
                critDamageMultiplier = Math.Max(1.0f, critDamageMultiplier); // 不低于1
            }
        }
        
        // 计算最终伤害
        int damage = (int)(finalValue * skillLevelMultiplier * damageMultiplier * finalDamageMultiplier * attackTypeResistance * damageTypeResistance * critDamageMultiplier);
        
        // 应用伤害（狼顾是反击技能，不是直接伤害）
        ApplyDamage(damage, counterTarget, counterSlot);
        
        // 添加伤害结算日志
        int shieldBefore = GetCharacterShield(counterTarget);
        int shieldAfter = GetCharacterShield(counterTarget);
        int healthBefore = counterTarget.CurrentHealth;
        int healthAfter = counterTarget.CurrentHealth;
        int shieldDamageTaken = shieldBefore - shieldAfter;
        int healthDamageTaken = healthBefore - healthAfter;
        
        // 记录伤害统计
        Statistics.RecordDamage(simaYi, counterSkill.Name, shieldDamageTaken, healthDamageTaken);
        
        if (shieldDamageTaken > 0 && healthDamageTaken > 0)
        {
            BattleLog.Add($"狼顾共造成{shieldDamageTaken}点护盾伤害,{healthDamageTaken}点体力伤害");
        }
        else if (shieldDamageTaken > 0)
        {
            BattleLog.Add($"狼顾共造成{shieldDamageTaken}点护盾伤害");
        }
        else if (healthDamageTaken > 0)
        {
            BattleLog.Add($"狼顾共造成{healthDamageTaken}点体力伤害");
        }
        else
        {
            BattleLog.Add($"狼顾共造成0点伤害");
        }
        
        // 处理狼顾命中时的效果
        // 记录狼顾造成的伤害
        int recordedDamage = shieldDamageTaken + healthDamageTaken;
        
        // 为全队魏国武将提供相当于本次伤害50%的护盾，最高不超过司马懿最大生命的15%
        int maxShieldAmount = (int)(simaYi.MaxHealth * 0.15f);
        int shieldAmountForAllies = (int)(recordedDamage * 0.5f);
        shieldAmountForAllies = Math.Min(shieldAmountForAllies, maxShieldAmount);
        
        if (shieldAmountForAllies > 0)
        {
            // 给攻击者自己加护盾
            AddShield(langguAttacker, shieldAmountForAllies, "狼顾");
            // 记录护盾统计
            Statistics.RecordShield(simaYi, "狼顾", shieldAmountForAllies);
            
            // 给同队其余魏国武将加护盾
            List<Character> allies = new List<Character>();
            if (simaYi.IsAlly)
            {
                allies.AddRange(Players);
            }
            else
            {
                allies.AddRange(Enemies);
            }
            
            foreach (var ally in allies)
            {
                if (ally != langguAttacker && ally.Faction == Faction.魏)
                {
                    AddShield(ally, shieldAmountForAllies, "狼顾");
                    // 记录护盾统计
                    Statistics.RecordShield(simaYi, "狼顾", shieldAmountForAllies);
                }
            }
        }
        
        // 处理狼顾攻击后效果
        // 检查司马懿的韬晦状态
        var simaYiBuffs = GetBuffs(simaYi);
        var taoHuiBuff = simaYiBuffs.Find(b => b is 韬晦);
        
        if (taoHuiBuff != null)
        {
            if (taoHuiBuff.Strength < 6)
            {
                // 韬晦强度低于6：获得1级韬晦
                AddBuff(simaYi, new 韬晦(3, 1));
            }
            else
            {
                // 韬晦强度不低于6：消耗一半强度，额外扣除目标生命值
                int originalStrength = taoHuiBuff.Strength;
                int strengthToConsume = originalStrength / 2;
                taoHuiBuff.Strength = originalStrength - strengthToConsume;
                
                // 额外扣除目标生命值，扣除值相当于伤害记录值
                if (counterTarget.CurrentHealth > 0)
                {
                    // 使用ApplyDamage处理额外伤害，确保先扣除护盾再扣除血量，并且可以被神威状态免疫
                    ApplyDamage(recordedDamage, counterTarget, counterSlot, isDirectDamage: true);
                }
                else
                {
                    // 目标已死亡，寻找可选中的随机敌方单位
                    List<Character> enemies = new List<Character>();
                    if (simaYi.IsAlly)
                    {
                        enemies.AddRange(Enemies);
                    }
                    else
                    {
                        enemies.AddRange(Players);
                    }
                    
                    // 筛选存活且可被选中的敌人
                    List<Character> aliveEnemies = enemies.FindAll(e => e.CurrentHealth > 0);
                    if (aliveEnemies.Count > 0)
                    {
                        // 随机选择一个存活的敌人
                        Random random = new Random();
                        Character randomTarget = aliveEnemies[random.Next(aliveEnemies.Count)];
                        
                        // 使用ApplyDamage处理额外伤害，确保先扣除护盾再扣除血量，并且可以被神威状态免疫
                        ApplyDamage(recordedDamage, randomTarget, counterSlot, isDirectDamage: true);
                    }
                }
            }
        }
        else
        {
            // 没有韬晦状态，获得1级韬晦
            AddBuff(simaYi, new 韬晦(3, 1));
        }
    }
    
    private string GetTeamInfo(Character character)
    {
        return character?.IsAlly == true ? "-我方" : "-敌方";
    }
}
