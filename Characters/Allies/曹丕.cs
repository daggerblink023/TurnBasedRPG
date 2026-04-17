using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Characters;
using TurnBasedRPG.Systems;
using TurnBasedRPG.Buffs.Buff;
using TurnBasedRPG.Buffs.Debuff;
using TurnBasedRPG.Characters.Skills.曹丕;

namespace TurnBasedRPG.Characters.Allies;

public class 曹丕 : Character
{
    private List<(int slotIndex, int skillLevel)> _discardedSkills = new List<(int, int)>();
    private BuffHandler _buffHandler;
    private int _weiSkillHitCount = 0; // 同队所有魏国武将的攻击/反击技能累计命中次数
    private int _counterTriggerCount = 0; // 反击技能[制衡]触发次数
    private int _maxCounterTriggerPerTurn = 5; // 每回合至多触发5次[制衡]（全队累计命中触发）
    private int _currentCounterTriggerThisTurn = 0; // 本回合已触发[制衡]次数（全队累计命中触发）
    private int _currentCaoPiAttackCounterTriggerThisTurn = 0; // 本回合已触发[制衡]次数（曹丕自身攻击命中触发）
    private int _maxCaoPiAttackCounterTriggerPerTurn = 1; // 每回合至多触发1次[制衡]（曹丕自身攻击命中触发）
    private int _currentWeiWuHongLiuTriggerThisTurn = 0; // 本回合已触发[魏武洪流]次数
    private int _maxWeiWuHongLiuTriggerPerTurn = 1; // 每回合至多触发1次[魏武洪流]
    private int _shieldDamageTriggerCount = 0; // 本回合受到护盾伤害触发次数
    private int _maxShieldDamageTriggerPerTurn = 3; // 每回合至多触发3次护盾伤害效果
    
    public 曹丕(bool hasCustomConstructor = false, bool isAlly = true) : base("曹丕", 80, 5, 40, 0, 0, isAlly)
    {
        PassiveName = "决断-曹丕";
        PassiveSkill = "使自身获得[决断-曹丕]与[嗣业承祚]，为同队所有魏国武将施加[镇国]\n\n[镇国]的状态强度等同于曹丕自身[决断-曹丕]的状态强度\n\n每当全队魏国武将的攻击/反击技能累计命中至少5次，曹丕会消耗一级[嗣业承祚]的状态强度，向该次技能命中的目标单位释放反击技能[制衡]，每回合至多触发5次\n\n曹丕的攻击技能命中敌方时，也会向该次技能命中的目标单位释放反击技能[制衡]，每回合至多触发一次\n\n若触发此效果时[嗣业承祚]的状态强度为0则会取消使用并返还触发次数，在下一次魏国武将的攻击/反击技能命中时尝试释放\n\n满足以下条件之一时，曹丕会使用强化反击技能[魏武洪流]攻击所有敌方单位\n- [嗣业承祚]的状态强度到达3时：每回合至多触发1次\n- 反击技能[制衡]每累计触发3次时：每回合至多触发1次\n\n[回合结束时]属于自身的每个行动槽会额外丢弃处于备选位置的技能，并将[决断-曹丕]的状态强度设置为被额外丢弃的技能中最高的技能等级";
        ShieldEffectiveness = 1.0f;
        Faction = Faction.魏;
        
        // 攻击方式易伤
        SlashVulnerability = 1.1f;
        BluntVulnerability = 1.1f;
        PierceVulnerability = 1.0f;
        SpellVulnerability = 0.8f;
        
        // 伤害类型易伤
        PhysicalVulnerability = 0.8f;
        MagicVulnerability = 1.2f;
        TrueVulnerability = 2.0f;
        
        // 速度范围
        MinSpeed = 4;
        MaxSpeed = 7;
    }
    
    protected override void InitializeSkills()
    {
        AttackSkills = new List<BaseSkill>();
        for (int i = 0; i < 3; i++) AttackSkills.Add(new 魏室初锋());
        for (int i = 0; i < 2; i++) AttackSkills.Add(new 定策安邦());
        AttackSkills.Add(new 受禅代汉());
        ShuffleAttackSkills();
        
        DefendSkill = new DefendSkill(); // 使用默认的DefendSkill，御极守成通过GetSkillByActionType方法返回
        HealSkill = null;
        DodgeSkill = null;
        CounterSkill = new CounterSkill(); // 使用默认的CounterSkill，制衡和魏武洪流通过特殊方式触发
        CounterSkill.CanBeSelected = false; // 曹丕的反击技能不能被主动选择
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
                        AttackSkill.Skill1 => new 魏室初锋(),
                        AttackSkill.Skill2 => new 定策安邦(),
                        AttackSkill.Skill3 => new 受禅代汉(),
                        _ => new 魏室初锋()
                    };
                    return skill;
                }
                return DrawAttackSkill();
            case ActionType.Defend:
                return new 御极守成();
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
    
    public void SetBuffHandler(BuffHandler buffHandler)
    {
        _buffHandler = buffHandler;
    }
    
    public void OnTurnStart(BuffHandler buffHandler, List<Character> allCharacters, BattleSystem battleSystem)
    {
        _discardedSkills.Clear();
        _weiSkillHitCount = 0;
        _counterTriggerCount = 0;
        _currentCounterTriggerThisTurn = 0;
        _currentCaoPiAttackCounterTriggerThisTurn = 0;
        _currentWeiWuHongLiuTriggerThisTurn = 0;
        _shieldDamageTriggerCount = 0;
        
        // 检查并添加决断-曹丕状态（仅在不存在时添加）
        if (!buffHandler.CheckBuff<决断_曹丕>(this))
        {
            buffHandler.AddBuff(this, new 决断_曹丕(null, 1));
        }
        
        // 检查并添加嗣业承祚状态（仅在不存在时添加）
        if (!buffHandler.CheckBuff<嗣业承祚>(this))
        {
            buffHandler.AddBuff(this, new 嗣业承祚(null, 0));
        }
        
        // 处理决断-曹丕的回合开始效果
        var jueDuanBuff = buffHandler.GetBuffs(this).Find(b => b is 决断_曹丕);
        if (jueDuanBuff is 决断_曹丕)
        {
            ((决断_曹丕)jueDuanBuff).OnTurnStart(this, buffHandler, battleSystem, allCharacters);
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
            
            var existingBuff = buffHandler.GetBuffs(this).Find(b => b is 决断_曹丕);
            if (existingBuff is 决断_曹丕)
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
            }
        }
    }
    
    public void ProcessSkillExtraEffects(List<ActionSlot> playerSlots, Dictionary<ActionSlot, Character> slotToCharacterMap, List<Character> allCharacters, BattleSystem battleSystem)
    {
        if (playerSlots == null || playerSlots.Count == 0)
            return;
        
        // 查找曹丕的行动槽并处理技能
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
                // 处理魏室初锋技能：[回合开始时]获得1级[嗣业承祚]
                if (slot.SkillName == "魏室初锋")
                {
                    var siYeChengZuoBuff = _buffHandler?.GetBuffs(this).Find(b => b is 嗣业承祚);
                    if (siYeChengZuoBuff != null)
                    {
                        Game1.Log($"[曹丕-嗣业承祚] 魏室初锋技能，嗣业承祚强度从{siYeChengZuoBuff.Strength}增加到{siYeChengZuoBuff.Strength + 1}");
                        siYeChengZuoBuff.Strength += 1;
                        // 处理溢出
                        if (siYeChengZuoBuff is 嗣业承祚)
                        {
                            ((嗣业承祚)siYeChengZuoBuff).HandleOverflow(this, _buffHandler, battleSystem);
                        }
                        Game1.Log($"[曹丕-嗣业承祚] 魏室初锋技能处理完成，当前嗣业承祚强度={siYeChengZuoBuff.Strength}");
                    }
                    else
                    {
                        Game1.Log($"[曹丕-嗣业承祚] 魏室初锋技能，未找到嗣业承祚Buff");
                    }
                }
                // 处理定策安邦技能：[回合开始时]获得1级[嗣业承祚]
                else if (slot.SkillName == "定策安邦")
                {
                    var siYeChengZuoBuff = _buffHandler?.GetBuffs(this).Find(b => b is 嗣业承祚);
                    if (siYeChengZuoBuff != null)
                    {
                        Game1.Log($"[曹丕-嗣业承祚] 定策安邦技能，嗣业承祚强度从{siYeChengZuoBuff.Strength}增加到{siYeChengZuoBuff.Strength + 1}");
                        siYeChengZuoBuff.Strength += 1;
                        // 处理溢出
                        if (siYeChengZuoBuff is 嗣业承祚)
                        {
                            ((嗣业承祚)siYeChengZuoBuff).HandleOverflow(this, _buffHandler, battleSystem);
                        }
                        Game1.Log($"[曹丕-嗣业承祚] 定策安邦技能处理完成，当前嗣业承祚强度={siYeChengZuoBuff.Strength}");
                    }
                    else
                    {
                        Game1.Log($"[曹丕-嗣业承祚] 定策安邦技能，未找到嗣业承祚Buff");
                    }
                }
                // 处理受禅代汉技能：[回合开始时]获得1级[嗣业承祚]
                else if (slot.SkillName == "受禅代汉")
                {
                    var siYeChengZuoBuff = _buffHandler?.GetBuffs(this).Find(b => b is 嗣业承祚);
                    if (siYeChengZuoBuff != null)
                    {
                        Game1.Log($"[曹丕-嗣业承祚] 受禅代汉技能，嗣业承祚强度从{siYeChengZuoBuff.Strength}增加到{siYeChengZuoBuff.Strength + 1}");
                        siYeChengZuoBuff.Strength += 1;
                        // 处理溢出
                        if (siYeChengZuoBuff is 嗣业承祚)
                        {
                            ((嗣业承祚)siYeChengZuoBuff).HandleOverflow(this, _buffHandler, battleSystem);
                        }
                        Game1.Log($"[曹丕-嗣业承祚] 受禅代汉技能处理完成，当前嗣业承祚强度={siYeChengZuoBuff.Strength}");
                    }
                    else
                    {
                        Game1.Log($"[曹丕-嗣业承祚] 受禅代汉技能，未找到嗣业承祚Buff");
                    }
                }
            }
        }
    }
    
    public void OnWeiSkillHit(Character target, BattleSystem battleSystem, List<Character> allCharacters, Character attacker = null, bool isCounterSkill = false)
    {
        string teamInfo = IsAlly ? "-我方" : "-敌方";
        string attackerTeamInfo = attacker?.IsAlly == true ? "-我方" : "-敌方";
        string targetTeamInfo = target?.IsAlly == true ? "-我方" : "-敌方";
        
        _weiSkillHitCount++;
        Game1.Log($"[曹丕-触发判定] 曹丕{teamInfo}魏国技能命中，命中计数增加至{_weiSkillHitCount}，攻击者={attacker?.Name}{attackerTeamInfo}，目标={target?.Name}{targetTeamInfo}，是否反击技能={isCounterSkill}");
        
        // 条件1：全队魏国武将的攻击/反击技能累计命中至少5次，触发制衡
        if (_weiSkillHitCount >= 5 && _currentCounterTriggerThisTurn < _maxCounterTriggerPerTurn)
        {
            Game1.Log($"[曹丕-触发判定] 曹丕{teamInfo}满足条件1：全队魏国武将累计命中{_weiSkillHitCount}次，达到触发阈值5次，当前制衡触发次数={_currentCounterTriggerThisTurn}/{_maxCounterTriggerPerTurn}");
            
            var siYeChengZuoBuff = _buffHandler?.GetBuffs(this).Find(b => b is 嗣业承祚);
            if (siYeChengZuoBuff != null && siYeChengZuoBuff.Strength > 0)
            {
                Game1.Log($"[曹丕-触发判定] 曹丕{teamInfo}嗣业承祚强度={siYeChengZuoBuff.Strength}，足够消耗，开始触发制衡");
                
                _currentCounterTriggerThisTurn++;
                _counterTriggerCount++;
                _weiSkillHitCount -= 5;
                
                // 消耗一级嗣业承祚
                siYeChengZuoBuff.Strength -= 1;
                Game1.Log($"[曹丕-嗣业承祚] 曹丕{teamInfo}消耗一级嗣业承祚，当前强度={siYeChengZuoBuff.Strength}");
                
                // 调用BattleSystem中的TriggerCaoPiCounter方法触发制衡
                battleSystem.TriggerCaoPiCounter(this, target, false);
                
                Game1.Log($"[曹丕-制衡] 曹丕{teamInfo}全队魏国武将累计命中触发制衡技能，当前触发次数={_currentCounterTriggerThisTurn}/{_maxCounterTriggerPerTurn}");
                
                // 检查是否可以触发魏武洪流（制衡累计触发3次）
                if (_counterTriggerCount >= 3 && _currentWeiWuHongLiuTriggerThisTurn < _maxWeiWuHongLiuTriggerPerTurn)
                {
                    Game1.Log($"[曹丕-触发判定] 曹丕{teamInfo}满足魏武洪流条件：制衡累计触发{_counterTriggerCount}次，达到触发阈值3次");
                    _currentWeiWuHongLiuTriggerThisTurn++;
                    _counterTriggerCount = 0; // 重置制衡触发次数
                    
                    // 调用BattleSystem中的TriggerCaoPiCounter方法触发魏武洪流
                    battleSystem.TriggerCaoPiCounter(this, target, true);
                    Game1.Log($"[曹丕-魏武洪流] 曹丕{teamInfo}制衡累计触发3次，触发魏武洪流技能，当前触发次数={_currentWeiWuHongLiuTriggerThisTurn}/{_maxWeiWuHongLiuTriggerPerTurn}");
                }
            }
            else if (siYeChengZuoBuff != null && siYeChengZuoBuff.Strength == 0)
            {
                // 嗣业承祚强度为0，取消使用并返还触发次数
                Game1.Log($"[曹丕-制衡] 曹丕{teamInfo}嗣业承祚强度为0，取消触发制衡");
            }
            else
            {
                Game1.Log($"[曹丕-制衡] 曹丕{teamInfo}未找到嗣业承祚Buff，取消触发制衡");
            }
        }
        
        // 条件2：曹丕的攻击技能命中敌方时，触发制衡（每回合至多触发一次）
        if (attacker == this && !isCounterSkill && target.IsAlly != this.IsAlly && _currentCaoPiAttackCounterTriggerThisTurn < _maxCaoPiAttackCounterTriggerPerTurn)
        {
            Game1.Log($"[曹丕-触发判定] 曹丕{teamInfo}满足条件2：自身攻击命中敌方，当前触发次数={_currentCaoPiAttackCounterTriggerThisTurn}/{_maxCaoPiAttackCounterTriggerPerTurn}");
            _currentCaoPiAttackCounterTriggerThisTurn++;
            
            // 调用BattleSystem中的TriggerCaoPiCounter方法触发制衡
            battleSystem.TriggerCaoPiCounter(this, target, false);
            
            Game1.Log($"[曹丕-制衡] 曹丕{teamInfo}自身攻击命中触发制衡技能，当前触发次数={_currentCaoPiAttackCounterTriggerThisTurn}/{_maxCaoPiAttackCounterTriggerPerTurn}");
        }
        
        // 检查是否可以触发魏武洪流（嗣业承祚强度到达3）
        var siYeChengZuoBuffCheck = _buffHandler?.GetBuffs(this).Find(b => b is 嗣业承祚);
        if (siYeChengZuoBuffCheck != null && siYeChengZuoBuffCheck.Strength >= 3 && _currentWeiWuHongLiuTriggerThisTurn < _maxWeiWuHongLiuTriggerPerTurn)
        {
            Game1.Log($"[曹丕-触发判定] 曹丕{teamInfo}满足魏武洪流条件：嗣业承祚强度={siYeChengZuoBuffCheck.Strength}，达到触发阈值3级");
            _currentWeiWuHongLiuTriggerThisTurn++;
            
            // 调用BattleSystem中的TriggerCaoPiCounter方法触发魏武洪流
            battleSystem.TriggerCaoPiCounter(this, target, true);
            Game1.Log($"[曹丕-魏武洪流] 曹丕{teamInfo}嗣业承祚强度到达3，触发魏武洪流技能，当前触发次数={_currentWeiWuHongLiuTriggerThisTurn}/{_maxWeiWuHongLiuTriggerPerTurn}");
        }
    }
    
    public void OnShieldDamage(BattleSystem battleSystem)
    {
        if (_shieldDamageTriggerCount < _maxShieldDamageTriggerPerTurn)
        {
            _shieldDamageTriggerCount++;
            
            // 获得1级嗣业承祚强度
            var siYeChengZuoBuff = _buffHandler?.GetBuffs(this).Find(b => b is 嗣业承祚);
            if (siYeChengZuoBuff != null)
            {
                Game1.Log($"[曹丕-嗣业承祚] 御极守成技能，受到护盾伤害，嗣业承祚强度从{siYeChengZuoBuff.Strength}增加到{siYeChengZuoBuff.Strength + 1}");
                siYeChengZuoBuff.Strength += 1;
                // 处理溢出
                if (siYeChengZuoBuff is 嗣业承祚)
                {
                    ((嗣业承祚)siYeChengZuoBuff).HandleOverflow(this, _buffHandler, battleSystem);
                }
                Game1.Log($"[曹丕-嗣业承祚] 御极守成技能处理完成，当前嗣业承祚强度={siYeChengZuoBuff.Strength}");
            }
            else
            {
                Game1.Log($"[曹丕-嗣业承祚] 御极守成技能，未找到嗣业承祚Buff");
            }
            
            Game1.Log($"[曹丕-御极守成] 受到护盾伤害，获得1级嗣业承祚，当前触发次数={_shieldDamageTriggerCount}/{_maxShieldDamageTriggerPerTurn}");
        }
    }
    
    public void SyncZhenGuoStrengthToAllWeiCharacters(List<Character> allCharacters)
    {
        if (_buffHandler == null)
            return;
            
        // 获取曹丕自己的决断-曹丕强度
        var jueDuanBuff = _buffHandler.GetBuffs(this).Find(b => b is 决断_曹丕);
        if (jueDuanBuff == null)
            return;
            
        int targetStrength = jueDuanBuff.Strength;
        
        // 同步给同队所有魏国武将
        foreach (var chara in allCharacters)
        {
            if (chara.Faction == Faction.魏 && chara.IsAlly == this.IsAlly)
            {
                var zhenGuoBuff = _buffHandler.GetBuffs(chara).Find(b => b is 镇国);
                if (zhenGuoBuff != null)
                {
                    zhenGuoBuff.Strength = targetStrength;
                }
            }
        }
    }
}