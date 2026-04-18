using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Characters;
using TurnBasedRPG.Systems;
using TurnBasedRPG.Buffs.Buff;
using TurnBasedRPG.Buffs.Debuff;
using TurnBasedRPG.Characters.Skills.曹操;

namespace TurnBasedRPG.Systems.SkillManagement;

public class CaoCaoSkill : BattleSystem
{
    private BattleSystem _battleSystem;
    
    public CaoCaoSkill(BattleSystem battleSystem) : base()
    {
        _battleSystem = battleSystem;
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
    
    // 处理霸道强度达到8时触发青釭开天
    public void CheckAndTriggerQinggangFromBadao(Character caoCao, BuffHandler buffHandler, List<Character> allCharacters, BattleSystem battleSystem = null)
    {
        var caoCaoObj = caoCao as TurnBasedRPG.Characters.Allies.曹操;
        if (caoCaoObj == null)
            return;

        var badaoBuff = buffHandler.GetBuffs(caoCao).Find(b => b is 霸道);
        if (badaoBuff == null || badaoBuff.Strength < 8)
            return;

        if (!caoCaoObj.CanTriggerQinggangFromBadao())
        {
            return;
        }
        badaoBuff.Strength = 0; // 清空霸道强度
        caoCaoObj.TriggerQinggangFromBadao();
        TriggerQinggangCounter(caoCaoObj, buffHandler, allCharacters, battleSystem);
    }

    private void TriggerQinggangCounter(TurnBasedRPG.Characters.Allies.曹操 caoCao, BuffHandler buffHandler, List<Character> allCharacters, BattleSystem battleSystem = null)
    {
        if (battleSystem == null) return;
        
        // 获取所有敌方单位
        List<Character> enemies = new List<Character>();
        if (caoCao.IsAlly)
        {
            enemies.AddRange(battleSystem.Enemies);
        }
        else
        {
            enemies.AddRange(battleSystem.Players);
        }

        // 筛选存活的敌人
        List<Character> aliveEnemies = enemies.FindAll(e => e.CurrentHealth > 0);
        if (aliveEnemies.Count == 0)
            return;

        // 创建青釭开天技能
        BaseSkill counterSkill = new 青釭开天();
        caoCao.CalculateSkillValues(counterSkill);

        // 创建临时行动槽
        ActionSlot counterSlot = new ActionSlot(0);
        counterSlot.SetAction(ActionType.Counter, counterSkill);
        counterSlot.FlipCoins(caoCao.Morale);

        if (battleSystem._slotToCharacterMap != null)
        {
            battleSystem._slotToCharacterMap[counterSlot] = caoCao;
        }



        // 对每个敌方单位造成伤害
        foreach (var enemy in aliveEnemies)
        {
            ApplySkillDamage(caoCao, enemy, counterSkill, counterSlot, battleSystem);
        }

        // 处理青釭开天攻击后效果
        ProcessQinggangPostAttackEffects(caoCao, aliveEnemies, buffHandler, allCharacters, battleSystem);
    }

    private void ApplySkillDamage(Character attacker, Character target, BaseSkill skill, ActionSlot slot, BattleSystem battleSystem = null)
    {
        if (battleSystem == null) return;
        
        // 计算硬币点数
        int headsCount = 0;
        for (int i = 0; i < slot.Coins.Length; i++)
        {
            if (slot.Coins[i] == 1)
                headsCount++;
        }
        int finalValue = skill.BaseValue + (headsCount * skill.CoinValue);

        // 计算攻防等级
        int skillLevel = attacker.FinalAttackLevel;
        int targetLevelForCalculation = target.FinalDefenseLevel;
        int levelDifference = skillLevel - targetLevelForCalculation;
        double skillLevelMultiplier = 1.5 + ((double)levelDifference * 0.045);
        skillLevelMultiplier = Math.Max(0.2, skillLevelMultiplier);

        // 计算伤害乘区
        float damageMultiplier = (1 + attacker.DamageIncrease - target.DamageReduction);
        damageMultiplier = Math.Max(0.2f, damageMultiplier);

        // 获取伤害种类抗性
        float damageTypeResistance = 1.0f;
        switch (skill.DamageType)
        {
            case DamageType.Physical:
                damageTypeResistance = target.PhysicalVulnerability;
                break;
            case DamageType.Magic:
                damageTypeResistance = target.MagicVulnerability;
                break;
            case DamageType.True:
                damageTypeResistance = target.TrueVulnerability;
                break;
        }

        // 获取攻击方式抗性
        float attackTypeResistance = 1.0f;
        switch (skill.AttackType)
        {
            case AttackType.Slash:
                attackTypeResistance = target.SlashVulnerability;
                break;
            case AttackType.Blunt:
                attackTypeResistance = target.BluntVulnerability;
                break;
            case AttackType.Pierce:
                attackTypeResistance = target.PierceVulnerability;
                break;
            case AttackType.Spell:
                attackTypeResistance = target.SpellVulnerability;
                break;
        }

        damageTypeResistance = Math.Max(0.1f, damageTypeResistance);
        attackTypeResistance = Math.Max(0.1f, attackTypeResistance);

        float finalDamageMultiplier = (1 + attacker.FinalDamageIncrease - target.FinalDamageReduction);

        int damage = (int)(finalValue * skillLevelMultiplier * damageMultiplier * finalDamageMultiplier * attackTypeResistance * damageTypeResistance);
        battleSystem.ApplyDamage(damage, target, slot);

        int shieldBefore = battleSystem.GetCharacterShield(target);
        int shieldAfter = battleSystem.GetCharacterShield(target);
        int healthBefore = target.CurrentHealth;
        int healthAfter = target.CurrentHealth;
        int shieldDamageTaken = shieldBefore - shieldAfter;
        int healthDamageTaken = healthBefore - healthAfter;

        battleSystem.Statistics.RecordDamage(attacker, skill.Name, shieldDamageTaken, healthDamageTaken);
    }

    public void ProcessQinggangPostAttackEffects(Character caoCao, List<Character> targets, BuffHandler buffHandler, List<Character> allCharacters, BattleSystem battleSystem = null)
    {
        if (battleSystem == null) return;
        
        // 为所有命中目标施加罪己诏
        foreach (var target in targets)
        {
            if (target.CurrentHealth > 0)
            {
                buffHandler.AddBuff(target, new 罪己诏(1, 1));
            }
        }

        // 消耗我方全部单位持有的护盾并重新分配
        List<Character> allies = new List<Character>();
        if (caoCao.IsAlly)
        {
            allies.AddRange(battleSystem.Players);
        }
        else
        {
            allies.AddRange(battleSystem.Enemies);
        }

        List<Character> aliveAllies = allies.FindAll(a => a.CurrentHealth > 0);
        int totalConsumedShield = 0;

        // 先记录并消耗所有护盾
        foreach (var ally in aliveAllies)
        {
            int shield = battleSystem.GetCharacterShield(ally);
            totalConsumedShield += shield;
            if (battleSystem._characterShields.ContainsKey(ally))
            {
                battleSystem._characterShields[ally] = 0;
            }
        }

        if (aliveAllies.Count > 0 && totalConsumedShield > 0)
        {
            int shieldPerAlly = totalConsumedShield / aliveAllies.Count;

            foreach (var ally in aliveAllies)
            {
                int shieldAmount = shieldPerAlly;
                if (ally.Faction == Faction.魏)
                {
                    shieldAmount = (int)(shieldPerAlly * 1.25);
                }
                battleSystem.AddShield(ally, shieldAmount, "青釭开天");
                battleSystem.Statistics.RecordShield(caoCao, "青釭开天", shieldAmount);
            }
        }

        // 使仁心状态强度提升，提升值相当于记录值的25%
        var renxinBuff = buffHandler.GetBuffs(caoCao).Find(b => b is 仁心);
        if (renxinBuff != null)
        {
            int renxinIncrease = (int)(totalConsumedShield * 0.25);
            renxinBuff.Strength += renxinIncrease;
        }
    }

    // 处理煮酒论英攻击前效果
    public void HandleZhujiulunyingPreAttack(Character target, BuffHandler buffHandler, ActionSlot slot)
    {
        // 检查目标减益状态数量
        int debuffCount = 0;
        var targetBuffs = buffHandler.GetBuffs(target);
        foreach (var buff in targetBuffs)
        {
            if (!buff.IsBuff)
                debuffCount++;
        }

        if (debuffCount >= 3)
        {
            slot.CompetingPower += 4;
        }
    }

    // 处理煮酒论英攻击后效果
    public void HandleZhujiulunyingPostAttack(Character caoCao, Character target, int totalDamage, BuffHandler buffHandler)
    {
        // 为目标施加罪己诏
        buffHandler.AddBuff(target, new 罪己诏(1, 1));

        // 使仁心状态强度提升
        var renxinBuff = buffHandler.GetBuffs(caoCao).Find(b => b is 仁心);
        if (renxinBuff != null)
        {
            renxinBuff.Strength += totalDamage;
        }
    }

    // 处理天下归心触发青釭开天
    public void TryTriggerQinggangFromTianxiaguixin(Character caoCao, BuffHandler buffHandler, List<Character> allCharacters, BattleSystem battleSystem = null)
    {
        var caoCaoObj = caoCao as TurnBasedRPG.Characters.Allies.曹操;
        if (caoCaoObj == null)
            return;

        if (!caoCaoObj.CanTriggerQinggangFromTianxiaguixin())
        {
            return;
        }

        caoCaoObj.TriggerQinggangFromTianxiaguixin();
        TriggerQinggangCounter(caoCaoObj, buffHandler, allCharacters, battleSystem);
    }

    // 处理屯田固本攻击后效果
    public void HandleTuntianGubenPostAttack(Character caoCao, Character target, int totalDamage, BuffHandler buffHandler)
    {
        // 使仁心状态强度提升，提升值相当于此技能总伤害的100%
        var renxinBuff = buffHandler.GetBuffs(caoCao).Find(b => b is 仁心);
        if (renxinBuff != null)
        {
            int renxinIncrease = totalDamage;
            renxinBuff.Strength += renxinIncrease;
        }
    }

    // 处理天下归心命中时效果
    public void HandleTianxiaguixinOnHit(Character caoCao, int totalConsumedValue, BuffHandler buffHandler)
    {
        // 使仁心状态强度提升，提升值相当于总消耗值的200%
        var renxinBuff = buffHandler.GetBuffs(caoCao).Find(b => b is 仁心);
        if (renxinBuff != null)
        {
            int renxinIncrease = totalConsumedValue * 2;
            renxinBuff.Strength += renxinIncrease;
        }
    }

    // 处理临危授命防御后效果
    public void HandleLinweiShoumingPostDefend(Character caoCao, int totalShieldAdded, Character? targetForShield, BuffHandler buffHandler)
    {
        int totalShieldForRenxin = totalShieldAdded;
        
        // 如果有目标，为目标添加相当于自己护盾50%的护盾
        if (targetForShield != null && _battleSystem != null)
        {
            int shieldForTarget = (int)(totalShieldAdded * 0.5f);
            _battleSystem.AddShield(targetForShield, shieldForTarget, "临危授命", caoCao);
            _battleSystem.Statistics.RecordShield(caoCao, "临危授命", shieldForTarget);
            totalShieldForRenxin += shieldForTarget;
        }
        
        // 使仁心状态强度提升，提升值相当于此技能施加的总护盾值的50%
        var renxinBuff = buffHandler.GetBuffs(caoCao).Find(b => b is 仁心);
        if (renxinBuff != null)
        {
            int renxinIncrease = (int)(totalShieldForRenxin * 0.5f);
            renxinBuff.Strength += renxinIncrease;
        }
    }

    // 处理仁心效果
    public void HandleRenxinEffect(Character caoCao, Character target, BuffHandler buffHandler, BattleSystem battleSystem = null)
    {
        if (battleSystem == null) return;
        
        var caoCaoObj = caoCao as TurnBasedRPG.Characters.Allies.曹操;
        if (caoCaoObj == null)
            return;
        
        // 检查目标是否是同队的魏国武将
        if (target.IsAlly != caoCao.IsAlly)
        {
            return;
        }
        if (target.Faction != Faction.魏)
        {
            return;
        }

        if (!caoCaoObj.CanTriggerRenxin())
        {
            return;
        }

        var renxinBuff = buffHandler.GetBuffs(caoCao).Find(b => b is 仁心);
        if (renxinBuff == null || renxinBuff.Strength <= 0)
        {
            return;
        }

        // 计算消耗的仁心强度和添加的护盾
        int consumedStrength = renxinBuff.Strength / 2;
        int maxShield = (int)(target.MaxHealth * 0.5);
        int shieldToAdd = Math.Min(consumedStrength, maxShield);

        if (shieldToAdd <= 0)
        {
            return;
        }

        // 消耗仁心强度
        renxinBuff.Strength -= shieldToAdd;

        // 为目标添加护盾
        battleSystem.AddShield(target, shieldToAdd, "仁心");
        caoCaoObj.TriggerRenxin();

        battleSystem.Statistics.RecordShield(caoCao, "仁心", shieldToAdd);
    }
}
