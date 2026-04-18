﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Characters;
using TurnBasedRPG.Systems;
using TurnBasedRPG.Buffs.Buff;
using TurnBasedRPG.Buffs.Debuff;
using TurnBasedRPG.Characters.Skills.曹操;

namespace TurnBasedRPG.Characters.Allies;

public class 曹操 : Character
{
    private List<(int slotIndex, int skillLevel)> _discardedSkills = new List<(int, int)>();
    private BuffHandler _buffHandler;
    private int _qinggangTriggerCount = 0; // 青釭开天通过霸道触发的次数
    private int _qinggangMaxTriggerCountFromBadao = 2; // 霸道每回合最多触发2次
    private int _qinggangTriggerCountFromTianxiaguixin = 0; // 青釭开天通过天下归心触发的次数
    private int _qinggangMaxTriggerCountFromTianxiaguixin = 1; // 天下归心每回合最多触发1次
    private int _renxinTriggerCount = 0; // 仁心每回合触发次数
    private int _renxinMaxTriggerCount = 3; // 仁心每回合最多触发3次

    public 曹操(bool hasCustomConstructor = false, bool isAlly = true) : base("曹操", 85, 5, 40, 0, 3, isAlly)
    {
        PassiveName = "决断-曹操";
        PassiveSkill = "为自身施加[决断-曹操]，[霸道]与[仁心]\n\n每当[霸道]状态强度达到8，会清空状态强度使用特殊反击[青釭开天]攻击敌方全体单位，每回合至多通过此方式使用2次\n\n[回合开始时]曹操会为敌方全体施加1级持续1回合的[罪己诏]\n\n[回合结束时]属于自身的每个行动槽会额外丢弃处于备选位置的技能，并将 [决断-曹操]的状态强度设置为被额外丢弃的技能中最高的技能等级";
        ShieldEffectiveness = 1.2f;
        Faction = Faction.魏;

        // 攻击方式易损
        SlashVulnerability = 1.0f;
        BluntVulnerability = 1.2f;
        PierceVulnerability = 1.0f;
        SpellVulnerability = 0.9f;

        // 伤害种类易损
        PhysicalVulnerability = 1.0f;
        MagicVulnerability = 1.0f;
        TrueVulnerability = 2.0f;

        // 速度范围
        MinSpeed = 3;
        MaxSpeed = 6;
    }

    protected override void InitializeSkills()
    {
        AttackSkills = new List<BaseSkill>();
        for (int i = 0; i < 3; i++) AttackSkills.Add(new 煮酒论英());
        for (int i = 0; i < 2; i++) AttackSkills.Add(new 屯田固本());
        AttackSkills.Add(new 天下归心());
        ShuffleAttackSkills();

        DefendSkill = new 临危授命();
        HealSkill = null;
        DodgeSkill = null;
        CounterSkill = new CounterSkill();
        CounterSkill.CanBeSelected = false;
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
                        AttackSkill.Skill1 => new 煮酒论英(),
                        AttackSkill.Skill2 => new 屯田固本(),
                        AttackSkill.Skill3 => new 天下归心(),
                        _ => new 煮酒论英()
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
                return new 青釭开天();
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
        _qinggangTriggerCount = 0;
        _qinggangMaxTriggerCountFromBadao = 2;
        _qinggangTriggerCountFromTianxiaguixin = 0;
        _qinggangMaxTriggerCountFromTianxiaguixin = 1;
        _renxinTriggerCount = 0;
        _renxinMaxTriggerCount = 3;

        // 检查并添加决断-曹操状态（仅在不存在时添加）
        if (!buffHandler.CheckBuff<决断_曹操>(this))
        {
            buffHandler.AddBuff(this, new 决断_曹操(null, 1));
        }

        // 检查并添加霸道状态（仅在不存在时添加）
        if (!buffHandler.CheckBuff<霸道>(this))
        {
            buffHandler.AddBuff(this, new 霸道(null, 0));
        }

        // 检查并添加仁心状态（仅在不存在时添加）
        if (!buffHandler.CheckBuff<仁心>(this))
        {
            buffHandler.AddBuff(this, new 仁心(null, 0));
        }

        // 先调用UpdateBuffs，确保FinalDefenseLevel等属性被正确计算
        UpdateBuffs(buffHandler);

        // 回合开始时，为敌方全体施加1级持续1回合的罪己诏

        foreach (var enemy in allCharacters)
        {
            if (enemy.IsAlly != this.IsAlly && enemy.CurrentHealth > 0)
            {
                buffHandler.AddBuff(enemy, new 罪己诏(1, 1));

            }
        }

        // 处理决断-曹操的回合开始效果
        var jueDuanBuff = buffHandler.GetBuffs(this).Find(b => b is 决断_曹操);
        if (jueDuanBuff is 决断_曹操)
        {
            ((决断_曹操)jueDuanBuff).OnTurnStart(this, buffHandler, battleSystem);
        }
    }

    public void ProcessSkillExtraEffects(List<ActionSlot> playerSlots, Dictionary<ActionSlot, Character> slotToCharacterMap, List<Character> allCharacters, BattleSystem battleSystem = null)
    {
        if (playerSlots == null || playerSlots.Count == 0)
            return;



        // 查找曹操的行动槽并处理技能
        foreach (var slot in playerSlots)
        {
            Character slotOwner = null;
            if (slotToCharacterMap.ContainsKey(slot))
            {
                slotOwner = slotToCharacterMap[slot];
            }

            if (slotOwner == this)
            {
                // 每次使用技能时，霸道强度+1
                var badaoBuff = _buffHandler?.GetBuffs(this).Find(b => b is 霸道);
                if (badaoBuff != null)
                {
                    badaoBuff.Strength = Math.Min(badaoBuff.Strength + 1, 8);

                }
            }
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



            var existingBuff = buffHandler.GetBuffs(this).Find(b => b is 决断_曹操);
            if (existingBuff is 决断_曹操)
            {
                // 曹操自己的决断-曹操，最小层数+1（2），最大层数+1（4），强度+1
                existingBuff.Strength = Math.Clamp(highestSkillLevel + 1, 2, 4);

            }
        }
    }

    public bool CanTriggerQinggangFromBadao()
    {
        return _qinggangTriggerCount < _qinggangMaxTriggerCountFromBadao;
    }

    public void TriggerQinggangFromBadao()
    {
        _qinggangTriggerCount++;
    }

    public bool CanTriggerQinggangFromTianxiaguixin()
    {
        return _qinggangTriggerCountFromTianxiaguixin < _qinggangMaxTriggerCountFromTianxiaguixin;
    }

    public void TriggerQinggangFromTianxiaguixin()
    {
        _qinggangTriggerCountFromTianxiaguixin++;
    }

    public bool CanTriggerRenxin()
    {
        return _renxinTriggerCount < _renxinMaxTriggerCount;
    }

    public void TriggerRenxin()
    {
        _renxinTriggerCount++;
    }
}
