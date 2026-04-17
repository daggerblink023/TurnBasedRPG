using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Characters;
using TurnBasedRPG.Characters.Allies;
using TurnBasedRPG.Systems;
using TurnBasedRPG.Buffs.Buff;

namespace TurnBasedRPG.Systems.SkillManagement;

public class CaoRenSkill : BattleSystem
{
    public CaoRenSkill(BattleSystem battleSystem) : base()
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
    
    public void HandleCaoRenShieldHit(Character caoRen, Character attacker, BuffHandler buffHandler, List<Character> allCharacters)
    {
        if (caoRen == null || attacker == null)
        {
            return;
        }
        
        // 检查曹仁是否持有默守状态
        var moshouBuff = buffHandler.GetBuffs(caoRen).Find(b => b is 默守);
        if (moshouBuff is 默守 moshou)
        {
            // 增加默守层数
            moshou.AddLayer();
            
            // 记录最后一次命中护盾的敌方单位
            if (attacker != null)
            {
                // 只记录敌方单位
                if (attacker.IsAlly != caoRen.IsAlly)
                {
                    moshou.LastShieldHitAttacker = attacker;
                }
            }
            
            // 检查是否达到6层，触发反击
            if (moshou.ShouldTriggerCounter())
            {
                // 先尝试触发反击，消耗默守强度在TriggerCaoRenCounter方法中处理
                TriggerCaoRenCounter(caoRen, moshou, buffHandler, allCharacters);
            }
        }
    }
    
    private void TriggerCaoRenCounter(Character caoRen, 默守 moshou, BuffHandler buffHandler, List<Character> allCharacters)
    {
        // 确定反击目标
        Character counterTarget = null;
        
        // 第一步：尝试使用最后一次命中护盾的敌方单位
        if (moshou.LastShieldHitAttacker != null && moshou.LastShieldHitAttacker.CurrentHealth > 0)
        {
            counterTarget = moshou.LastShieldHitAttacker;
        }
        
        // 第二步：如果目标已死亡或不存在，随机选择一个存活且可被选中的敌方单位
        if (counterTarget == null || counterTarget.CurrentHealth <= 0)
        {
            List<Character> enemies = new List<Character>();
            if (caoRen.IsAlly)
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
        
        // 创建默守蓄锋技能
        BaseSkill counterSkill = new TurnBasedRPG.Characters.Skills.曹仁.默守蓄锋();
        caoRen.CalculateSkillValues(counterSkill);
        
        // 创建临时行动槽来处理反击
        ActionSlot counterSlot = new ActionSlot(0);
        counterSlot.SetAction(ActionType.Attack, counterSkill);
        
        // 投掷硬币
        counterSlot.FlipCoins(caoRen.Morale);
        
        // 添加临时行动槽到映射中，以便找到攻击者
        _slotToCharacterMap[counterSlot] = caoRen;
        
        // 记录这次攻击
        BattleLog.Add($"曹仁积蓄的锋芒爆发，对{counterTarget.Name}发动了默守蓄锋！");
        
        // 直接进行完整的伤害计算
        // 计算当前总的硬币点数
        int headsCount = 0;
        for (int i = 0; i < counterSlot.Coins.Length; i++)
        {
            if (counterSlot.Coins[i] == 1)
                headsCount++;
        }
        int finalValue = counterSkill.BaseValue + (headsCount * counterSkill.CoinValue);
        
        // 确定攻防等级（默守蓄锋使用防御等级计算）
        int skillLevel = caoRen.FinalDefenseLevel;
        
        // 计算skillLevelMultiplier（攻防等级修正乘区）（默守蓄锋是反击技能，使用4.5%倍率）
        double multiplierRate = 0.045;
        
        // 默守蓄锋是反击技能，使用目标的防御等级进行计算
        int targetLevelForCalculation = counterTarget.FinalDefenseLevel;
        int levelDifference = skillLevel - targetLevelForCalculation;
        double skillLevelMultiplier = 1.0 + ((double)levelDifference * multiplierRate);
        skillLevelMultiplier = Math.Max(0.2, skillLevelMultiplier);
        
        // 一类增伤乘区damageMultiplier：(1+攻击者伤害提升-目标伤害减免)，最低0.2
        float damageMultiplier = (1 + caoRen.DamageIncrease - counterTarget.DamageReduction);
        
        // 夏侯惇决断-夏侯惇真实伤害增伤：来源为夏侯惇、来源拥有【决断-夏侯惇】状态强度、伤害类型为真实伤害
        Character jueDuanAttacker = caoRen; // 默守蓄锋的攻击者是曹仁
        if (jueDuanAttacker != null && jueDuanAttacker is 夏侯惇 && counterSkill.DamageType == DamageType.True)
        {
            var buffs = buffHandler.GetBuffs(jueDuanAttacker);
            var jueDuanBuff = buffs.Find(b => b is 决断_夏侯惇);
            if (jueDuanBuff != null)
            {
                damageMultiplier += jueDuanBuff.Strength * 0.1f;
            }
        }
        
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
        float finalDamageMultiplier = (1 + caoRen.FinalDamageIncrease - counterTarget.FinalDamageReduction);
        
        // 暴击判定
        bool isCriticalHit = false;
        float critDamageMultiplier = 1.0f;
        // 默守蓄锋的攻击者是曹仁
        Character moshouAttacker = caoRen;
        if (moshouAttacker != null)
        {
            // 计算暴击概率
            float skillCritRate = counterSkill.CritRate;
            float targetCritResistance = counterTarget.CritResistance;
            float firstStepCritRate = Math.Max(0, skillCritRate - targetCritResistance);
            float finalCritRateStep = counterSkill.FinalCritRate - counterTarget.FinalCritResistance;
            float totalCritRate = Math.Max(0, firstStepCritRate + finalCritRateStep);
            totalCritRate = Math.Min(totalCritRate, 1.0f); // 超出100%视为100%
            
            // 按概率判定是否暴击
            Random moshouRandom = new Random();
            double randomValue = moshouRandom.NextDouble();
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
        
        // 记录伤害计算前的护盾和血量值
        int shieldBefore = GetCharacterShield(counterTarget);
        int healthBefore = counterTarget.CurrentHealth;
        
        // 应用伤害
        ApplyDamage(damage, counterTarget, counterSlot);
        
        // 添加伤害结算日志
        int shieldAfter = GetCharacterShield(counterTarget);
        int healthAfter = counterTarget.CurrentHealth;
        int shieldDamageTaken = shieldBefore - shieldAfter;
        int healthDamageTaken = healthBefore - healthAfter;
        int totalDamageTaken = shieldDamageTaken + healthDamageTaken;
        
        // 记录伤害统计
        Statistics.RecordDamage(caoRen, counterSkill.Name, shieldDamageTaken, healthDamageTaken);
        
        if (shieldDamageTaken > 0 && healthDamageTaken > 0)
        {
            BattleLog.Add($"默守蓄锋共造成{shieldDamageTaken}点护盾伤害,{healthDamageTaken}点体力伤害");
        }
        else if (shieldDamageTaken > 0)
        {
            BattleLog.Add($"默守蓄锋共造成{shieldDamageTaken}点护盾伤害");
        }
        else if (healthDamageTaken > 0)
        {
            BattleLog.Add($"默守蓄锋共造成{healthDamageTaken}点体力伤害");
        }
        else
        {
            BattleLog.Add($"默守蓄锋共造成0点伤害");
        }
        
        // 处理默守蓄锋技能的效果：为自身与同队其余魏国武将获得最大生命10%的护盾
        int maxHealth = caoRen.MaxHealth;
        int shieldAmount = (int)(maxHealth * 0.1f);
        
        if (shieldAmount > 0)
        {
            // 收集所有需要添加护盾的目标
            List<Character> shieldTargets = new List<Character>();
            
            // 先添加自己
            shieldTargets.Add(caoRen);
            
            // 再添加同队其余魏国武将
            List<Character> allies = new List<Character>();
            if (caoRen.IsAlly)
            {
                allies.AddRange(Players);
            }
            else
            {
                allies.AddRange(Enemies);
            }
            
            foreach (var ally in allies)
            {
                if (ally != caoRen && ally.Faction == Faction.魏)
                {
                    shieldTargets.Add(ally);
                }
            }
            
            // 使用批量添加护盾的方法
            AddShieldToMultiple(caoRen, shieldTargets, shieldAmount, "默守蓄锋");
        }
        
        // 成功释放默守蓄锋后，消耗6级默守强度
        moshou.Strength = moshou.Strength - 6;
        if (moshou.Strength < 0)
        {
            moshou.Strength = 0;
        }
    }
}
