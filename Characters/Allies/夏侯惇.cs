using System;
using System.Collections.Generic;
using TurnBasedRPG.Characters;
using TurnBasedRPG.Systems;
using TurnBasedRPG.Buffs.Buff;
using TurnBasedRPG.Buffs.Debuff;
using TurnBasedRPG.Characters.Skills.夏侯惇;

namespace TurnBasedRPG.Characters.Allies;

public class 夏侯惇 : Character
{
    private bool _silenceAppliedThisTurn = false;
    private bool _enduranceSkill2AppliedThisTurn = false;
    private bool _enduranceSkill3AppliedThisTurn = false;
    private bool _ganglieAppliedThisTurn = false;
    private List<(int slotIndex, int skillLevel)> _discardedSkills = new List<(int, int)>();
    
    public 夏侯惇(bool hasCustomConstructor = false, bool isAlly = true) : base("夏侯惇", 79, 5, 40, -2, 3, isAlly)
    {
        PassiveName = "决断-夏侯惇";
        PassiveSkill = "使自身获得[决断-夏侯惇]与[固阵]\n\n若同一阵营存在其他魏国武将，则不会获得[固阵]，而是使本阵营的所有魏国武将获得[魏武固阵]\n\n[回合结束时]属于自身的每个行动槽会额外丢弃处于备选位置的技能，并将[决断-夏侯惇]的状态强度设置为被额外丢弃的技能中最高的技能等级";
        ShieldEffectiveness = 1.33f;
        Faction = Faction.魏;
        
        SlashVulnerability = 1.0f;
        BluntVulnerability = 0.6f;
        PierceVulnerability = 1.3f;
        SpellVulnerability = 1.2f;
        
        PhysicalVulnerability = 1.1f;
        MagicVulnerability = 0.8f;
        TrueVulnerability = 2.0f;
        
        // 速度范围
        MinSpeed = 3;
        MaxSpeed = 6;
    }
    
    protected override void InitializeSkills()
    {
        AttackSkills = new List<BaseSkill>();
        for (int i = 0; i < 3; i++) AttackSkills.Add(new 横斩());
        for (int i = 0; i < 2; i++) AttackSkills.Add(new 拔矢啖睛());
        AttackSkills.Add(new 铁壁战吼());
        ShuffleAttackSkills();
        
        DefendSkill = new 刚烈之魂();
        HealSkill = null;
        DodgeSkill = null;
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
                        AttackSkill.Skill1 => new 横斩(),
                        AttackSkill.Skill2 => new 拔矢啖睛(),
                        AttackSkill.Skill3 => new 铁壁战吼(),
                        _ => new 横斩()
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
            default:
                return null;
        }
    }
    
    public void RecordDiscardedSkill(int slotIndex, int skillLevel)
    {
        _discardedSkills.Add((slotIndex, skillLevel));
    }
    
    public void OnTurnStart(BuffHandler buffHandler, List<Character> allCharacters, BattleSystem battleSystem)
    {
        _silenceAppliedThisTurn = false;
        _enduranceSkill2AppliedThisTurn = false;
        _enduranceSkill3AppliedThisTurn = false;
        _ganglieAppliedThisTurn = false;
        _discardedSkills.Clear();
        
        // 检查并添加决断-夏侯惇状态（仅在不存在时添加）
        if (!buffHandler.CheckBuff<决断_夏侯惇>(this))
        {
            buffHandler.AddBuff(this, new 决断_夏侯惇(null, 1));
        }
        
        // 处理决断-夏侯惇的回合开始效果
        var jueDuanBuff = buffHandler.GetBuffs(this).Find(b => b is 决断_夏侯惇);
        if (jueDuanBuff is 决断_夏侯惇)
        {
            ((决断_夏侯惇)jueDuanBuff).OnTurnStart(this, buffHandler, battleSystem);
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
            
            var existingBuff = buffHandler.GetBuffs(this).Find(b => b is 决断_夏侯惇);
            if (existingBuff is 决断_夏侯惇)
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
    
    public void HandleAttackSkill2Effect(BuffHandler buffHandler, List<Character> allCharacters)
    {
        if (!_enduranceSkill2AppliedThisTurn)
        {
            // 给同队所有魏国武将加2级忍耐
            foreach (var chara in allCharacters)
            {
                if (chara.Faction == Faction.魏 && chara.IsAlly == this.IsAlly)
                {
                    buffHandler.AddBuff(chara, new Endurance(2, 2));
                }
            }
            _enduranceSkill2AppliedThisTurn = true;
        }
    }
    
    public void HandleAttackSkill3Effect(BuffHandler buffHandler)
    {
        if (!_enduranceSkill3AppliedThisTurn)
        {
            buffHandler.AddBuff(this, new Endurance(1, 4));
            _enduranceSkill3AppliedThisTurn = true;
        }
    }
    
    public void HandleAttackSkill3SilenceEffect(BuffHandler buffHandler)
    {
        if (!_silenceAppliedThisTurn)
        {
            buffHandler.AddBuff(this, new Silence(2)); // 持续2回合
            _silenceAppliedThisTurn = true;
        }
    }
    
    public void HandleDefendSkillEffect(BuffHandler buffHandler)
    {
        if (!_ganglieAppliedThisTurn)
        {
            buffHandler.AddBuff(this, new Ganglie(1));
            _ganglieAppliedThisTurn = true;
        }
    }
}
