using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Characters;
using TurnBasedRPG.Systems;
using TurnBasedRPG.Buffs.Buff;
using TurnBasedRPG.Buffs.Debuff;

namespace TurnBasedRPG.Systems.SkillManagement;

public class CaoPiSkill : BattleSystem
{
    public CaoPiSkill(BattleSystem battleSystem) : base()
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
    
    public void TriggerCaoPiCounter(Character caoPi, Character target, bool isWeiWuHongLiu = false, BuffHandler buffHandler = null, List<Character> allCharacters = null, BattleSystem battleSystem = null)
    {
        if (buffHandler == null || allCharacters == null)
        {
            return;
        }
        
        // 确定反击目标
        Character counterTarget = target;
        
        // 检查目标是否有效
        if (counterTarget == null)
        {
            List<Character> enemies = new List<Character>();
            if (caoPi.IsAlly)
            {
                enemies.AddRange(Enemies);
            }
            else
            {
                enemies.AddRange(Players);
            }
            
            // 筛选存活且可被选中的敌人，排除曹丕自己
            List<Character> aliveEnemies = enemies.FindAll(e => e.CurrentHealth > 0 && !(e.Name == caoPi.Name && e.IsAlly == caoPi.IsAlly));
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
        else if (counterTarget.CurrentHealth <= 0)
        {
            // 目标已死亡，选择新目标
            List<Character> enemies = new List<Character>();
            if (caoPi.IsAlly)
            {
                enemies.AddRange(Enemies);
            }
            else
            {
                enemies.AddRange(Players);
            }
            
            // 筛选存活且可被选中的敌人，排除曹丕自己
            List<Character> aliveEnemies = enemies.FindAll(e => e.CurrentHealth > 0 && !(e.Name == caoPi.Name && e.IsAlly == caoPi.IsAlly));
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
        else if (counterTarget.Name == caoPi.Name && counterTarget.IsAlly == caoPi.IsAlly)
        {
            // 目标是曹丕自己，选择新目标
            List<Character> enemies = new List<Character>();
            if (caoPi.IsAlly)
            {
                enemies.AddRange(Enemies);
            }
            else
            {
                enemies.AddRange(Players);
            }
            
            // 筛选存活且可被选中的敌人，排除曹丕自己
            List<Character> aliveEnemies = enemies.FindAll(e => e.CurrentHealth > 0 && !(e.Name == caoPi.Name && e.IsAlly == caoPi.IsAlly));
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
        
        // 创建反击技能
        BaseSkill counterSkill;
        if (isWeiWuHongLiu)
        {
            counterSkill = new TurnBasedRPG.Characters.Skills.曹丕.魏武洪流();
        }
        else
        {
            counterSkill = new TurnBasedRPG.Characters.Skills.曹丕.制衡();
        }
        caoPi.CalculateSkillValues(counterSkill);
        // [攻击前]设置基础伤害
        if (isWeiWuHongLiu)
        {
            // 魏武洪流：将本技能的基础伤害设置为（防御等级/4，不低于1）
            int baseDamage = Math.Max(1, caoPi.FinalDefenseLevel / 4);
            counterSkill.BaseValue = baseDamage;
            
            // 魏武洪流：为同队所有持有增益效果不低于3个的魏国武将施加1级持续3回合的天恩
            List<Character> allies = new List<Character>();
            if (caoPi.IsAlly)
            {
                allies.AddRange(Players);
            }
            else
            {
                allies.AddRange(Enemies);
            }
            
            foreach (var ally in allies)
            {
                if (ally.Faction == Faction.魏)
                {
                    var buffs = buffHandler.GetBuffs(ally);
                    int buffCount = buffs.Count;
                    if (buffCount >= 3)
                    {
                        buffHandler.AddBuff(ally, new 天恩(3, 1));
                    }
                }
            }
        }
        else
        {
            // 制衡：将本技能的基础伤害设置为（防御等级/3，不低于1）
            int baseDamage = Math.Max(1, caoPi.FinalDefenseLevel / 3);
            counterSkill.BaseValue = baseDamage;
        }
        
        // 创建临时行动槽来处理反击
        ActionSlot counterSlot = new ActionSlot(0);
        counterSlot.SetAction(ActionType.Attack, counterSkill);
        
        // 投掷硬币
        counterSlot.FlipCoins(caoPi.Morale);
        
        // 添加临时行动槽到映射中，以便找到攻击者
        if (_slotToCharacterMap != null)
        {
            _slotToCharacterMap[counterSlot] = caoPi;
        }
        
        // 记录这次攻击
        BattleLog.Add($"曹丕{(isWeiWuHongLiu ? "的魏武洪流" : "的制衡")}爆发，对{counterTarget.Name}发动了{(isWeiWuHongLiu ? "魏武洪流" : "制衡")}！");
        
        // 直接进行完整的伤害计算
        // 计算当前总的硬币点数
        int headsCount = 0;
        for (int i = 0; i < counterSlot.Coins.Length; i++)
        {
            if (counterSlot.Coins[i] == 1)
                headsCount++;
        }
        int finalValue = counterSkill.BaseValue + (headsCount * counterSkill.CoinValue);
        
        // 确定攻防等级（曹丕的反击技能使用防御等级计算）
        int skillLevel = caoPi.FinalDefenseLevel;
        
        // 计算skillLevelMultiplier（攻防等级修正乘区）（反击技能使用4.5%倍率）
        double multiplierRate = 0.045;
        
        // 反击技能，使用目标的防御等级进行计算
        int targetLevelForCalculation = counterTarget.FinalDefenseLevel;
        int levelDifference = skillLevel - targetLevelForCalculation;
        double skillLevelMultiplier = 1.0 + ((double)levelDifference * multiplierRate);
        skillLevelMultiplier = Math.Max(0.2, skillLevelMultiplier);
        
        // 一类增伤乘区damageMultiplier：(1+攻击者伤害提升-目标伤害减免)，最低0.2
        float damageMultiplier = (1 + caoPi.DamageIncrease - counterTarget.DamageReduction);
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
        float finalDamageMultiplier = (1 + caoPi.FinalDamageIncrease - counterTarget.FinalDamageReduction);
        
        // 暴击判定
        bool isCriticalHit = false;
        float critDamageMultiplier = 1.0f;
        // 曹丕的反击技能攻击者是曹丕
        Character caoPiAttacker = caoPi;
        if (caoPiAttacker != null)
        {
            // 计算暴击概率
            float skillCritRate = counterSkill.CritRate;
            float targetCritResistance = counterTarget.CritResistance;
            float firstStepCritRate = Math.Max(0, skillCritRate - targetCritResistance);
            float finalCritRateStep = counterSkill.FinalCritRate - counterTarget.FinalCritResistance;
            float totalCritRate = Math.Max(0, firstStepCritRate + finalCritRateStep);
            totalCritRate = Math.Min(totalCritRate, 1.0f); // 超出100%视为100%
            
            // 按概率判定是否暴击
            Random caoPiRandom = new Random();
            double randomValue = caoPiRandom.NextDouble();
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
        
        // 应用伤害
        ApplyDamage(damage, counterTarget, counterSlot);
        
        // 添加伤害结算日志
        int shieldBefore = GetCharacterShield(counterTarget);
        int shieldAfter = GetCharacterShield(counterTarget);
        int healthBefore = counterTarget.CurrentHealth;
        int healthAfter = counterTarget.CurrentHealth;
        int shieldDamageTaken = shieldBefore - shieldAfter;
        int healthDamageTaken = healthBefore - healthAfter;
        
        // 记录伤害统计
        Statistics.RecordDamage(caoPi, counterSkill.Name, shieldDamageTaken, healthDamageTaken);
        
        if (shieldDamageTaken > 0 && healthDamageTaken > 0)
        {
            BattleLog.Add($"{(isWeiWuHongLiu ? "魏武洪流" : "制衡")}共造成{shieldDamageTaken}点护盾伤害,{healthDamageTaken}点体力伤害");
        }
        else if (shieldDamageTaken > 0)
        {
            BattleLog.Add($"{(isWeiWuHongLiu ? "魏武洪流" : "制衡")}共造成{shieldDamageTaken}点护盾伤害");
        }
        else if (healthDamageTaken > 0)
        {
            BattleLog.Add($"{(isWeiWuHongLiu ? "魏武洪流" : "制衡")}共造成{healthDamageTaken}点体力伤害");
        }
        else
        {
            BattleLog.Add($"{(isWeiWuHongLiu ? "魏武洪流" : "制衡")}共造成0点伤害");
        }
        
        // 处理魏武洪流技能的效果：使所有目标获得1级虚弱，持续2回合
        if (isWeiWuHongLiu)
        {
            // 使所有敌方单位获得1级虚弱，持续2回合
            List<Character> enemies = new List<Character>();
            if (caoPi.IsAlly)
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
                    buffHandler.AddBuff(enemy, new 虚弱(2, 1));
                }
            }
        }
        
        // 处理制衡技能的效果：为同队所有持有增益效果不低于3个的魏国武将施加1级持续3回合的天恩
        if (!isWeiWuHongLiu)
        {
            // 为同队所有持有增益效果不低于3个的魏国武将施加1级持续3回合的天恩
            List<Character> allies = new List<Character>();
            if (caoPi.IsAlly)
            {
                allies.AddRange(Players);
            }
            else
            {
                allies.AddRange(Enemies);
            }
            
            foreach (var ally in allies)
            {
                if (ally.Faction == Faction.魏)
                {
                    var buffs = buffHandler.GetBuffs(ally);
                    int buffCount = buffs.Count;
                    if (buffCount >= 3)
                    {
                        buffHandler.AddBuff(ally, new 天恩(3, 1));
                    }
                }
            }
        }
        
        // 消耗嗣业承祚强度（仅制衡需要）
        if (!isWeiWuHongLiu)
        {
            var siYeChengZuoBuff = buffHandler.GetBuffs(caoPi).Find(b => b is 嗣业承祚);
            if (siYeChengZuoBuff != null)
            {
                siYeChengZuoBuff.Strength = Math.Max(0, siYeChengZuoBuff.Strength - 1);
            }
        }
        
        // 魏武洪流获得3级嗣业承祚强度
        if (isWeiWuHongLiu)
        {
            var siYeChengZuoBuff = buffHandler.GetBuffs(caoPi).Find(b => b is 嗣业承祚);
            if (siYeChengZuoBuff != null)
            {
                siYeChengZuoBuff.Strength += 3;
                // 处理溢出
                if (siYeChengZuoBuff is 嗣业承祚)
                {
                    ((嗣业承祚)siYeChengZuoBuff).HandleOverflow(caoPi, buffHandler, this);
                }
            }
        }
    }
    
    public void HandleCaoPiSkillEffects(Character caoPi, ActionSlot slot, Character target, BuffHandler buffHandler, List<Character> allCharacters, BattleSystem battleSystem = null)
    {
        if (caoPi == null || slot == null || target == null)
        {
            return;
        }
        
        // 处理魏室初锋技能：[回合开始时]获得1级[嗣业承祚]
        if (slot.SkillName == "魏室初锋")
        {
            var siYeChengZuoBuff = buffHandler.GetBuffs(caoPi).Find(b => b is 嗣业承祚);
            if (siYeChengZuoBuff != null)
            {
                siYeChengZuoBuff.Strength += 1;
                // 处理溢出
                if (siYeChengZuoBuff is 嗣业承祚)
                {
                    ((嗣业承祚)siYeChengZuoBuff).HandleOverflow(caoPi, buffHandler, this);
                }
            }
        }
        
        // 处理定策安邦技能：[攻击前]为同队所有持有增益效果不低于3个的魏国武将施加1级持续3回合的[天恩]与相当于生命上限10%的护盾，并回复1点士气值
        if (slot.SkillName == "定策安邦" && slot.IsFirstCoin)
        {
            // 为同队所有持有增益效果不低于3个的魏国武将施加1级持续3回合的天恩与相当于生命上限10%的护盾，并回复1点士气值
            List<Character> allies = new List<Character>();
            if (caoPi.IsAlly)
            {
                allies.AddRange(Players);
            }
            else
            {
                allies.AddRange(Enemies);
            }
            
            foreach (var ally in allies)
            {
                if (ally.Faction == Faction.魏)
                {
                    var buffs = buffHandler.GetBuffs(ally);
                    int buffCount = buffs.Count;
                    if (buffCount >= 3)
                    {
                        // 施加1级持续3回合的天恩
                        buffHandler.AddBuff(ally, new 天恩(3, 1));
                        
                        // 施加相当于生命上限10%的护盾
                        int shieldAmount = (int)(ally.MaxHealth * 0.1f);
                        if (shieldAmount > 0)
                        {
                            AddShield(ally, shieldAmount);
                        }
                        
                        // 回复1点士气值
                        ally.AdjustMorale(1);
                    }
                }
            }
        }
        
        // 处理定策安邦技能：[攻击后]若目标持有减益效果不低于3个则额外造成相当于（防御等级/4，不低于1）的真实伤害，此附加伤害暴击率固定为50%
        if (slot.SkillName == "定策安邦" && slot.IsLastCoin)
        {
            var debuffs = buffHandler.GetBuffs(target);
            int debuffCount = debuffs.Count;
            if (debuffCount >= 3)
            {
                // 计算额外真实伤害
                int additionalDamage = Math.Max(1, caoPi.FinalDefenseLevel / 4);
                
                // 固定50%暴击率
                bool isCritical = new Random().NextDouble() < 0.5;
                
                // 应用真实伤害
                if (isCritical)
                {
                    additionalDamage = (int)(additionalDamage * 1.5f); // 假设暴击伤害为1.5倍
                    BattleLog.Add($"定策安邦的附加伤害产生了暴击！");
                }
                
                // 使用ApplyDamage方法处理真实伤害
                ApplyDamage(additionalDamage, target, slot, isDirectDamage: true);
                
                BattleLog.Add($"定策安邦对{target.Name}造成{additionalDamage}点真实伤害");
                
                // 若附加伤害产生了暴击，额外获得1级嗣业承祚
                if (isCritical)
                {
                    var siYeChengZuoBuff = buffHandler.GetBuffs(caoPi).Find(b => b is 嗣业承祚);
                    if (siYeChengZuoBuff != null)
                    {
                        siYeChengZuoBuff.Strength += 1;
                        // 处理溢出
                        if (siYeChengZuoBuff is 嗣业承祚)
                        {
                            ((嗣业承祚)siYeChengZuoBuff).HandleOverflow(caoPi, buffHandler, this);
                        }
                    }
                }
            }
        }
        
        // 处理受禅代汉技能：[使用前]使基础伤害提升（[嗣业承祚]的状态强度*10）%
        if (slot.SkillName == "受禅代汉" && slot.IsFirstCoin)
        {
            var siYeChengZuoBuff = buffHandler.GetBuffs(caoPi).Find(b => b is 嗣业承祚);
            if (siYeChengZuoBuff != null)
            {
                float damageMultiplier = 1.0f + (siYeChengZuoBuff.Strength * 0.1f);
                slot.BaseDamageMultiplier = damageMultiplier;
                
                // 使本技能的拼点威力提升4
                slot.CompetingPower += 4;
                
                // 若自身的嗣业承祚强度高于3级，则高出的每级强度使本技能的硬币点数提升1
                if (siYeChengZuoBuff.Strength > 3)
                {
                    int extraStrength = siYeChengZuoBuff.Strength - 3;
                    slot.CoinValueBonus += extraStrength;
                }
            }
            
            // 受禅代汉：[攻击前]为同队所有持有增益效果不低于3个的魏国武将施加1级持续3回合的天恩
            List<Character> allies = new List<Character>();
            if (caoPi.IsAlly)
            {
                allies.AddRange(Players);
            }
            else
            {
                allies.AddRange(Enemies);
            }
            
            foreach (var ally in allies)
            {
                if (ally.Faction == Faction.魏)
                {
                    var buffs = buffHandler.GetBuffs(ally);
                    int buffCount = buffs.Count;
                    if (buffCount >= 3)
                    {
                        buffHandler.AddBuff(ally, new 天恩(3, 1));
                    }
                }
            }
        }
        
        // 处理受禅代汉技能：[攻击后]为全队魏国武将施加护盾，护盾值相当于本技能总伤害量的50%
        if (slot.SkillName == "受禅代汉" && slot.IsLastCoin)
        {
            int totalDamage = slot.TotalDamage;
            int shieldAmount = (int)(totalDamage * 0.5f);
            
            if (shieldAmount > 0)
            {
                // 为全队魏国武将施加护盾
                List<Character> allies = new List<Character>();
                if (caoPi.IsAlly)
                {
                    allies.AddRange(Players);
                }
                else
                {
                    allies.AddRange(Enemies);
                }
                
                foreach (var ally in allies)
                {
                    if (ally.Faction == Faction.魏)
                    {
                        AddShield(ally, shieldAmount);
                    }
                }
            }
            
            // 清零嗣业承祚的状态强度，然后将强度设置为3级
            var siYeChengZuoBuff = buffHandler.GetBuffs(caoPi).Find(b => b is 嗣业承祚);
            if (siYeChengZuoBuff != null)
            {
                siYeChengZuoBuff.Strength = 0;
                siYeChengZuoBuff.Strength = 3;
            }
        }
        
        // 处理御极守成技能：本回合自身持有护盾时临时提升25%伤害减免，且受到护盾伤害时获得1级嗣业承祚强度（每回合至多触发3次）
        if (slot.SkillName == "御极守成")
        {
            // 本回合自身持有护盾时临时提升25%伤害减免
            if (GetCharacterShield(caoPi) > 0)
            {
                caoPi.FinalDamageReduction += 0.25f;
            }
        }
    }
}
