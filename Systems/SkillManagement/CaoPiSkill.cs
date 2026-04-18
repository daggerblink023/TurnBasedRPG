using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Characters;
using TurnBasedRPG.Systems;
using TurnBasedRPG.Buffs.Buff;
using TurnBasedRPG.Buffs.Debuff;
using TurnBasedRPG.Characters.Skills.曹丕;

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
        
        if (isWeiWuHongLiu)
        {
            // 魏武洪流：群攻 - 对所有敌方单位造成相同伤害
            // 投掷硬币
            int headsCount = 0;
            for (int i = 0; i < counterSkill.CoinCount; i++)
            {
                headsCount += new Random().Next(2); // 简单模拟硬币投掷
            }
            int finalValue = counterSkill.BaseValue + (headsCount * counterSkill.CoinValue);
            
            // 初始化_allTargetsDamage列表
            _allTargetsDamage = new List<TargetDamageInfo>();
            
            // 使用ProcessAreaAttack对所有敌方单位造成相同伤害
            ProcessAreaAttack(caoPi, finalValue, counterSkill, recordToAllTargetsDamage: true);
            
            // 处理魏武洪流技能的效果：使所有目标获得1级虚弱，持续2回合
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
            
            // 魏武洪流获得3级嗣业承祚强度
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
        else
        {
            // 制衡：单个目标伤害
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
            
            // 初始化_allTargetsDamage列表
            _allTargetsDamage = new List<TargetDamageInfo>();
            
            // 直接进行完整的伤害计算
            // 计算当前总的硬币点数
            int headsCount = 0;
            for (int i = 0; i < counterSlot.Coins.Length; i++)
            {
                if (counterSlot.Coins[i] == 1)
                    headsCount++;
            }
            int finalValue = counterSkill.BaseValue + (headsCount * counterSkill.CoinValue);
            
            // 使用CalculateDamageForTarget对单个目标造成伤害
            var damageInfo = CalculateDamageForTarget(caoPi, counterTarget, finalValue, counterSkill);
            _allTargetsDamage.Add(damageInfo);
            
            // 记录伤害统计
            Statistics.RecordDamage(caoPi, counterSkill.Name, damageInfo.ShieldDamage, damageInfo.HealthDamage);
            
            // 处理制衡技能的效果：为同队所有持有增益效果不低于3个的魏国武将施加1级持续3回合的天恩
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
            
            // 消耗嗣业承祚强度（仅制衡需要）
            var siYeChengZuoBuff = buffHandler.GetBuffs(caoPi).Find(b => b is Buffs.Buff.嗣业承祚);
            if (siYeChengZuoBuff != null)
            {
                siYeChengZuoBuff.Strength = Math.Max(0, siYeChengZuoBuff.Strength - 1);
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

                }
                
                // 使用ApplyDamage方法处理真实伤害
                ApplyDamage(additionalDamage, target, slot, isDirectDamage: true);

                
                
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
        
        // 处理受禅代汉技能：[使用前]使基础伤害提升（嗣业承祚的状态强度*10%）
        if (slot.SkillName == "受禅代汉" && slot.IsFirstCoin)
        {
            var siYeChengZuoBuff = buffHandler.GetBuffs(caoPi).Find(b => b is 嗣业承祚);
            if (siYeChengZuoBuff != null)
            {
                float damageMultiplier = 1.0f + (siYeChengZuoBuff.Strength * 0.1f);
                slot.BaseDamageMultiplier = damageMultiplier;
                
                // 使本技能的硬币威力提升4
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
        
        // 处理受禅代汉技能：[攻击后]为全队魏国武将施加护盾，护盾值相当于本技能总伤害的50%
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
            
            // 清空嗣业承祚的状态强度，然后将强度设置为3级
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
