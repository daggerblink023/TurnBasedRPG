using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Characters;
using TurnBasedRPG.Systems;
using TurnBasedRPG.Buffs.Buff;
using TurnBasedRPG.Buffs.Debuff;
using TurnBasedRPG.Characters.Skills.司马懿;

namespace TurnBasedRPG.Characters.Allies;

public class 司马懿 : Character
{
    private List<(int slotIndex, int skillLevel)> _discardedSkills = new List<(int, int)>();
    private BuffHandler _buffHandler;
    private int _langguTriggerCount = 0; // 狼顾触发次数
    private int _langguMaxTriggerCount = 2; // 狼顾每回合初始可触发次数
    private List<Character> _langguTriggeredEnemies = new List<Character>(); // 已经触发狼顾的敌方单位，每个敌方单位每回合至多触发一次
    private List<Character> _langguLastHitEnemies = new List<Character>(); // 已经被最后一枚硬币命中的敌方单位，每个敌方单位每回合至多触发一次
    private Character _lastLangguAttacker = null; // 最后一次触发狼顾的攻击者
    
    public 司马懿(bool hasCustomConstructor = false, bool isAlly = true) : base("司马懿", 76, 4, 40, -2, 2, isAlly)
    {
        PassiveName = "决断-司马懿";
        PassiveSkill = "使自身获得[决断-司马懿]与[韬晦]，同队的其他魏国武将同样获得[韬晦]\n\n满足以下条件之一时，司马懿会使用反击技能[狼顾]攻击目标，每回合初始可触发2次；每回合的第一对行动槽行动前，司马懿的[韬晦]强度每有3级，使本回合[狼顾]的可用次数+1\n\n-自身被敌方的非反击技能的最后一枚硬币命中时：每个敌方单位每回合至多触发1次\n\n-同队魏国武将的护盾被敌方的非反击技能击破时：每个敌方单位每回合至多触发1次\n\n-自身的非反击技能命中敌方时：有（30+[韬晦]强度*5）%概率触发\n\n[回合结束时]属于自身的每个行动槽会额外丢弃处于备选位置的技能，并将[决断-司马懿]的状态强度设置为被额外丢弃的技能中最高的技能等级";
        ShieldEffectiveness = 1.2f;
        Faction = Faction.魏;
        
        // 攻击方式易损
        SlashVulnerability = 1.4f;
        BluntVulnerability = 1.0f;
        PierceVulnerability = 0.8f;
        SpellVulnerability = 0.9f;
        
        // 伤害种类易损
        PhysicalVulnerability = 1.0f;
        MagicVulnerability = 1.0f;
        TrueVulnerability = 2.0f;
        
        // 速度范围
        MinSpeed = 2;
        MaxSpeed = 5;
    }
    
    protected override void InitializeSkills()
    {
        AttackSkills = new List<BaseSkill>();
        for (int i = 0; i < 3; i++) AttackSkills.Add(new 机先());
        for (int i = 0; i < 2; i++) AttackSkills.Add(new 汲魂());
        AttackSkills.Add(new 窃国者侯());
        ShuffleAttackSkills();
        
        DefendSkill = new 料敌();
        HealSkill = null;
        DodgeSkill = null;
        CounterSkill = new CounterSkill(); // 使用默认的CounterSkill，狼顾通过特殊方式触发
        CounterSkill.CanBeSelected = false; // 司马懿的反击技能不能被主动选择
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
                        AttackSkill.Skill1 => new 机先(),
                        AttackSkill.Skill2 => new 汲魂(),
                        AttackSkill.Skill3 => new 窃国者侯(),
                        _ => new 机先()
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
    
    public void SetSlotsAndSystem(BuffHandler buffHandler)
    {
        _buffHandler = buffHandler;
    }
    
    private void SyncTaohuiStrengthToAllWeiCharacters(List<Character> allCharacters)
    {
        if (_buffHandler == null)
            return;
            
        // 获取司马懿自己的韬晦强度
        var simaYiTaohui = _buffHandler.GetBuffs(this).Find(b => b is 韬晦);
        if (simaYiTaohui == null)
            return;
            
        int targetStrength = simaYiTaohui.Strength;
        
        // 同步给同队所有魏国武将
        foreach (var chara in allCharacters)
        {
            if (chara.Faction == Faction.魏 && chara.IsAlly == this.IsAlly && chara != this)
            {
                var taohuiBuff = _buffHandler.GetBuffs(chara).Find(b => b is 韬晦);
                if (taohuiBuff != null)
                {
                    taohuiBuff.Strength = targetStrength;
                }
            }
        }
    }
    
    public void CalculateLangguMaxTriggerCount()
    {
        _langguMaxTriggerCount = 2; // 每回合初始可触发2次
        
        // 每回合的第一对行动槽行动前，司马懿的[韬晦]强度每有3级，使本回合[狼顾]的可用次数+1
        var taohuiBuff = _buffHandler?.GetBuffs(this).Find(b => b is 韬晦);
        if (taohuiBuff != null)
        {
            int extraCount = taohuiBuff.Strength / 3;
            _langguMaxTriggerCount += extraCount;
            Game1.Log($"[司马懿-狼顾] 计算最大触发次数：初始2次 + 韬晦强度{taohuiBuff.Strength}/3={extraCount}次 = 总共{_langguMaxTriggerCount}次");
        }
    }
    
    public void OnTurnStart(BuffHandler buffHandler, List<Character> allCharacters, BattleSystem battleSystem)
    {
        _discardedSkills.Clear();
        _langguTriggerCount = 0;
        _langguMaxTriggerCount = 2; // 每回合初始可触发2次
        _langguTriggeredEnemies.Clear();
        _langguLastHitEnemies.Clear();
        _lastLangguAttacker = null;
        
        // 检查并添加决断-司马懿状态（仅在不存在时添加）
        if (!buffHandler.CheckBuff<决断_司马懿>(this))
        {
            buffHandler.AddBuff(this, new 决断_司马懿(null, 1));
        }
        
        // 为同队所有魏国武将添加韬晦状态
        foreach (var chara in allCharacters)
        {
            if (chara.Faction == Faction.魏 && chara.IsAlly == this.IsAlly)
            {
                // 检查并添加韬晦状态（仅在不存在时添加）
                if (!buffHandler.CheckBuff<韬晦>(chara))
                {
                    buffHandler.AddBuff(chara, new 韬晦(null, 0));
                }
            }
        }
        
        // 处理决断-司马懿的回合开始效果
        var jueDuanBuff = buffHandler.GetBuffs(this).Find(b => b is 决断_司马懿);
        if (jueDuanBuff is 决断_司马懿)
        {
            ((决断_司马懿)jueDuanBuff).OnTurnStart(this, buffHandler, battleSystem);
        }
    }
    
    public void ProcessSkillExtraEffects(List<ActionSlot> playerSlots, Dictionary<ActionSlot, Character> slotToCharacterMap, List<Character> allCharacters, BattleSystem battleSystem = null)
    {
        if (playerSlots == null || playerSlots.Count == 0)
            return;
            
        Game1.Log($"[司马懿-处理技能额外效果] 开始处理技能额外效果");
        
        // 查找司马懿的行动槽并处理技能
        foreach (var slot in playerSlots)
        {
            // 获取行动槽对应的角色
            Character slotOwner = null;
            if (slotToCharacterMap.ContainsKey(slot))
            {
                slotOwner = slotToCharacterMap[slot];
            }
            
            if (slotOwner == this)
            {
                // 处理机先技能：[回合开始时]获得1级[韬晦]
                if (slot.SkillName == "机先")
                {
                    Game1.Log($"[司马懿-机先] 处理机先技能额外效果，获得1级韬晦");
                    var taohuiBuff = _buffHandler?.GetBuffs(this).Find(b => b is 韬晦);
                    if (taohuiBuff != null)
                    {
                        taohuiBuff.Strength = Math.Min(taohuiBuff.Strength + 1, 9); // 韬晦上限9
                        Game1.Log($"[司马懿-机先] 韬晦强度增加至{taohuiBuff.Strength}");
                    }
                }
                // 处理汲魂技能：[回合开始时]获得1级[韬晦]
                else if (slot.SkillName == "汲魂")
                {
                    Game1.Log($"[司马懿-汲魂] 处理汲魂技能额外效果，获得1级韬晦");
                    var taohuiBuff = _buffHandler?.GetBuffs(this).Find(b => b is 韬晦);
                    if (taohuiBuff != null)
                    {
                        taohuiBuff.Strength = Math.Min(taohuiBuff.Strength + 1, 9); // 韬晦上限9
                        Game1.Log($"[司马懿-汲魂] 韬晦强度增加至{taohuiBuff.Strength}");
                    }
                }
                // 处理窃国者侯技能：[回合开始时]获得3级[韬晦]
                else if (slot.SkillName == "窃国者侯")
                {
                    Game1.Log($"[司马懿-窃国者侯] 处理窃国者侯技能额外效果，获得3级韬晦");
                    var taohuiBuff = _buffHandler?.GetBuffs(this).Find(b => b is 韬晦);
                    if (taohuiBuff != null)
                    {
                        taohuiBuff.Strength = Math.Min(taohuiBuff.Strength + 3, 9); // 韬晦上限9
                        Game1.Log($"[司马懿-窃国者侯] 韬晦强度增加至{taohuiBuff.Strength}");
                    }
                }
            }
        }
        
        // 同步韬晦强度给同队所有魏国武将
        SyncTaohuiStrengthToAllWeiCharacters(allCharacters);
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
            
            Game1.Log($"[司马懿-回合结束] 被额外丢弃的技能最高等级={highestSkillLevel}");
            
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
            
            var existingBuff = buffHandler.GetBuffs(this).Find(b => b is 决断_司马懿);
            if (existingBuff is 决断_司马懿)
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
                Game1.Log($"[司马懿-回合结束] 决断-司马懿状态强度设置为={existingBuff.Strength}");
            }
        }
    }
    
    public bool CanTriggerLanggu(Character attacker, bool isEnemyAttack, bool isCounterSkill, bool isSelfHit, bool isShieldBroken, bool isSelfSkillHit, bool isLastCoinHit = false)
    {
        string teamInfo = IsAlly ? "-我方" : "-敌方";
        Game1.Log($"[狼顾-触发判定] 司马懿{teamInfo}开始判定狼顾触发条件，当前触发次数={_langguTriggerCount}/{_langguMaxTriggerCount}");
        
        if (_langguTriggerCount >= _langguMaxTriggerCount)
        {
            Game1.Log($"[狼顾-触发判定] 司马懿{teamInfo}触发次数已达{_langguMaxTriggerCount}次上限，跳过");
            return false;
        }
        
        // 检查attacker是否为null
        if (attacker == null)
        {
            Game1.Log($"[狼顾-触发判定] 司马懿{teamInfo}攻击者为null，跳过");
            return false;
        }
        
        bool shouldTrigger = false;
        string attackerTeamInfo = attacker.IsAlly ? "-我方" : "-敌方";
        
        // 条件1：自身被敌方的非反击技能的最后一枚硬币命中时：每个敌方单位每回合至多触发1次
        if (isSelfHit && isEnemyAttack && !isCounterSkill && isLastCoinHit && !_langguLastHitEnemies.Contains(attacker))
        {
            Game1.Log($"[狼顾-触发判定] 司马懿{teamInfo}条件1满足：自身被敌方非反击技能的最后一枚硬币命中，攻击者={attacker.Name}{attackerTeamInfo}本回合未触发过");
            shouldTrigger = true;
        }
        // 条件2：同队魏国武将的护盾被敌方的非反击技能击破时：每个敌方单位每回合至多触发一次
        else if (isShieldBroken && isEnemyAttack && !isCounterSkill && !_langguTriggeredEnemies.Contains(attacker))
        {
            Game1.Log($"[狼顾-触发判定] 司马懿{teamInfo}条件2满足：同队魏国武将的护盾被敌方非反击技能击破，攻击者={attacker.Name}{attackerTeamInfo}本回合未触发过");
            shouldTrigger = true;
        }
        // 条件3：自身的非反击技能命中敌方时：有（30+[韬晦]强度*5）%概率触发
        else if (isSelfSkillHit && !isCounterSkill)
        {
            var taohuiBuff = _buffHandler?.GetBuffs(this).Find(b => b is 韬晦);
            if (taohuiBuff != null)
            {
                float probability = 0.3f + (taohuiBuff.Strength * 0.05f); // 30% + 韬晦强度*5%
                Random random = new Random();
                double randomValue = random.NextDouble();
                if (randomValue < probability)
                {
                    Game1.Log($"[狼顾-触发判定] 司马懿{teamInfo}条件3满足：自身非反击技能命中敌方，韬晦强度={taohuiBuff.Strength}，概率={(probability * 100):0.0}%，随机值={randomValue:F4}，触发成功");
                    shouldTrigger = true;
                }
                else
                {
                    Game1.Log($"[狼顾-触发判定] 司马懿{teamInfo}条件3未满足：自身非反击技能命中敌方，韬晦强度={taohuiBuff.Strength}，概率={(probability * 100):0.0}%，随机值={randomValue:F4}，触发失败");
                }
            }
            else
            {
                Game1.Log($"[狼顾-触发判定] 司马懿{teamInfo}条件3未满足：未找到韬晦Buff");
            }
        }
        else
        {
            Game1.Log($"[狼顾-触发判定] 司马懿{teamInfo}所有条件均未满足，isSelfHit={isSelfHit}, isEnemyAttack={isEnemyAttack}, !isCounterSkill={!isCounterSkill}, isLastCoinHit={isLastCoinHit}, !_langguLastHitEnemies.Contains(attacker)={!_langguLastHitEnemies.Contains(attacker)}");
            Game1.Log($"[狼顾-触发判定] 司马懿{teamInfo}额外条件：isShieldBroken={isShieldBroken}, !_langguTriggeredEnemies.Contains(attacker)={!_langguTriggeredEnemies.Contains(attacker)}, isSelfSkillHit={isSelfSkillHit}");
        }
        
        if (shouldTrigger)
        {
            _langguTriggerCount++;
            if (isSelfHit && isEnemyAttack && isLastCoinHit)
            {
                _langguLastHitEnemies.Add(attacker);
                Game1.Log($"[狼顾-触发判定] 司马懿{teamInfo}添加攻击者{attacker.Name}{attackerTeamInfo}到_lastLangguAttacker列表");
            }
            if (isShieldBroken && isEnemyAttack)
            {
                _langguTriggeredEnemies.Add(attacker);
                Game1.Log($"[狼顾-触发判定] 司马懿{teamInfo}添加攻击者{attacker.Name}{attackerTeamInfo}到_langguTriggeredEnemies列表");
            }
            _lastLangguAttacker = attacker;
            Game1.Log($"[狼顾-触发判定] 司马懿{teamInfo}触发狼顾，当前触发次数={_langguTriggerCount}/{_langguMaxTriggerCount}");
        }
        else
        {
            Game1.Log($"[狼顾-触发判定] 司马懿{teamInfo}狼顾触发失败");
        }
        
        return shouldTrigger;
    }
    
    public Character GetLastLangguAttacker()
    {
        return _lastLangguAttacker;
    }
}
