using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TurnBasedRPG;
using TurnBasedRPG.Characters;
using TurnBasedRPG.Characters.Allies;
using TurnBasedRPG.Characters.Enemies;
using TurnBasedRPG.Characters.Skills.曹仁;
using TurnBasedRPG.Characters.Skills.夏侯惇;
using TurnBasedRPG.Characters.Skills.司马懿;
using TurnBasedRPG.Characters.Skills.张辽;
using TurnBasedRPG.Buffs;
using TurnBasedRPG.Buffs.Buff;
using TurnBasedRPG.Buffs.Debuff;
using TurnBasedRPG.Systems.SkillManagement;

namespace TurnBasedRPG.Systems;

// 伤害类型（用于显示效果）
public enum HealthShieldDamageType
{
    Health,
    Shield
}

// 伤害事件参数
public class DamageEventArgs : EventArgs
{
    public Character Target { get; set; }
    public int DamageAmount { get; set; }
    public HealthShieldDamageType DamageType { get; set; }
}

// 反击技能信息
public class CounterSkillInfo
{
    public ActionSlot CounterSlot { get; set; }
    public Character Attacker { get; set; }
    public Character Target { get; set; }
    public int OriginalShieldDamage { get; set; }
    public int OriginalHealthDamage { get; set; }
}

// 伤害计算结果（包含所有乘区）
public class DamageCalculationResult
{
    public int FinalDamage { get; set; }              // 最终伤害
    public int BaseValue { get; set; }                // 基础点数
    public double SkillLevelMultiplier { get; set; }  // 攻防等级修正
    public float DamageMultiplier { get; set; }       // 一类增伤
    public float FinalDamageMultiplier { get; set; }  // 最终增伤
    public float AttackTypeResistance { get; set; }   // 攻击方式易损
    public float DamageTypeResistance { get; set; }   // 伤害类型易损
    public bool IsCrit { get; set; }                   // 是否暴击
    public float CritDamageMultiplier { get; set; }   // 暴击伤害乘区
    public int ShieldDamage { get; set; }             // 护盾伤害
    public int HealthDamage { get; set; }             // 血量伤害
}

public enum BattlePhase
{
    PlayerSelection,
    EnemySelection,
    Resolution,
    BattleEnd
}

public class BattleSystem
{
    // 伤害事件
    public event EventHandler<DamageEventArgs> OnDamage;
    
    public List<Character> Players { get; set; }
    public List<Character> Enemies { get; set; }
    public List<ActionSlot> PlayerSlots { get; set; }
    public List<ActionSlot> EnemySlots { get; set; }
    public int CurrentPlayerSlot { get; set; }
    public BattlePhase CurrentPhase { get; set; }
    public bool BattleEnded { get; set; }
    public string BattleMessage { get; set; }
    public List<string> BattleLog { get; set; }
    public Dictionary<ActionSlot, Character> _slotToCharacterMap { get; set; } // 行动槽到角色的映射
    public BattleStatistics Statistics { get; set; } = new BattleStatistics();

    public int MaxSlots { get; private set; } = 6; // 默认6个行动槽
    private int _resolutionStep;
    private bool _deflectionTriggeredThisRound;
    private bool _dodgeTriggeredThisRound = false;
    public Dictionary<Character, int> _characterShields = new Dictionary<Character, int>();
    private double _resolutionTimer;
    private double _roundTimer;
    private double _attackTimer;
    private double _coinTimer;
    private const double STEP_DELAY = 5.0; // 行动槽执行之间：停顿5000ms
    private const double ATTACK_DELAY = 3.0; // 拼点过程结束后-攻击技能执行伤害计算前：3000ms
    private const double COIN_DELAY = 1.5; // 每次拼点之间：1500ms
    private const double ROUND_DELAY = 7.5; // 回合切换时：7500ms
    private const double COIN_RETHROW_DELAY = 1.5; // 重投硬币之间（非最后一枚硬币）：1500ms
    private const double FINAL_COIN_DELAY = 3.0; // 最后一枚硬币投掷前：3000ms
    private const double SKILL_COMPLETE_DELAY = 2.0; // 技能完成后停留时间：2000ms
    private const double DODGE_RESULT_DELAY = 1.5; // 闪避结果展示时间：1500ms
    private enum ResolutionState
    {
        WaitingForStep,
        ResolvingStep,
        WaitingForAttack,
        RethrowingCoins,
        WaitingForSkillComplete,
        WaitingForDodgeResult,
        WaitingForRound
    }
    private ResolutionState _resolutionState;
    // 重投硬币相关变量
    private ActionSlot _currentAttackSlot;
    private Character _currentTarget;
    private int _currentCoinIndex;
    private int[] _originalCoins;
    private int _baseAttackValue;
    private int _coinValue;
    private int _attackLevel;
    private int[] _rerolledCoins; // 存储已重投的硬币结果
    private int _totalShieldDamage; // 累积的护盾伤害
    private int _totalHealthDamage; // 累积的体力伤害
    private string _attackerName; // 攻击者名称
    private string _attackSkillName; // 攻击技能名称
    private bool _attackEngagedInShowdown; // 标记攻击是否进行了拼点
    private int _targetDefenseLevel; // 目标的防御等级
    
    // 御甲鸣镝技能的次级目标信息
    private List<Character> _yujiaSecondaryTargets;
    private bool _yujiaHasNoSecondaryTargets;
    
    // 汲魂技能消耗的护盾总值
    private int _jihunConsumedShieldTotal;
    
    // 反击技能信息
    private CounterSkillInfo _counterSkillInfo;
    
    // Buff处理
    internal BuffHandler _buffHandler;
    public BuffHandler BuffHandler { get { return _buffHandler; } }
    
    // 目标系统相关字段
    private int _currentTargetSelectionOrder = 0;  // 当前目标选择序号（用于多对一冲突解决）
    private ActionSlot _manualSelectionSource;  // 手动选择目标时的源行动槽
    private bool _inManualSelectionMode = false;  // 是否处于手动选择目标模式
    private static Random _targetRandom = new Random();  // 目标选择用的随机数生成器
    
    // 执行顺序相关字段
    private List<ActionSlot> _executionOrder = new List<ActionSlot>();  // 行动槽执行顺序
    private int _currentResolutionIndex = 0;  // 当前执行到的行动槽索引
    
    // 公共属性用于可视化
    public List<ActionSlot> ExecutionOrder { get { return _executionOrder; } }
    public int CurrentResolutionStep { get { return _resolutionStep; } }
    
    public Character GetCharacterByActionSlot(ActionSlot slot)
    {
        if (_slotToCharacterMap != null && _slotToCharacterMap.ContainsKey(slot))
        {
            return _slotToCharacterMap[slot];
        }
        return null;
    }

    public BattleSystem()
    {

        List<Character> players = new List<Character> { new 示例角色1() };
        List<Character> enemies = new List<Character> { new 示例敌怪1() };
        InitializeBattle(players, enemies);
    }
    
    public BattleSystem(Character player, Character enemy)
    {
        
        List<Character> players = new List<Character> { player };
        List<Character> enemies = new List<Character> { enemy };
        InitializeBattle(players, enemies);
    }
    
    public BattleSystem(List<Character> players, List<Character> enemies)
    {
        InitializeBattle(players, enemies);
    }
    
    public BattleSystem(List<Character> players, List<Character> enemies, int maxSlots)
    {
        MaxSlots = maxSlots;
        InitializeBattle(players, enemies);
    }



    private void InitializeBattle(List<Character> players, List<Character> enemies)
    {
        // 设置己方角色的选择顺序
        for (int i = 0; i < players.Count; i++)
        {
            players[i].SelectionOrder = i;
        }
        Players = players;
        
        // 设置敌方角色的选择顺序
        for (int i = 0; i < enemies.Count; i++)
        {
            enemies[i].SelectionOrder = i;
        }
        Enemies = enemies;
        
        _slotToCharacterMap = new Dictionary<ActionSlot, Character>();
        _characterShields = new Dictionary<Character, int>();
        _buffHandler = new BuffHandler();
        _buffHandler.SetBattleSystem(this);
        InitializeBattleSlots();
    }
    
    public bool IsCounterSkill(ActionSlot slot)
    {
        if (slot == null)
            return false;
            
        // 判断是否是标准的反击技能
        if (slot.Type == ActionType.Counter)
            return true;
            
        // 判断是否是伪装成攻击技能的特殊反击技能
        if (slot.SkillName == "默守蓄锋" || slot.SkillName == "狼顾")
            return true;
            
        return false;
    }
    
    private void InitializeBattleSlots()
    {
        PlayerSlots = new List<ActionSlot>();
        EnemySlots = new List<ActionSlot>();
        
        for (int i = 0; i < MaxSlots; i++)
        {
            var playerSlot = new ActionSlot(i + 1);
            playerSlot.IsAlly = true;
            PlayerSlots.Add(playerSlot);
            
            var enemySlot = new ActionSlot(i + 1);
            enemySlot.IsAlly = false;
            EnemySlots.Add(enemySlot);
        }
        
        // 分配行动槽给角色
        AssignSlotsToCharacters();
        
        CurrentPlayerSlot = 0;
        CurrentPhase = BattlePhase.EnemySelection;
        BattleEnded = false;
        BattleMessage = "敌人正在选择技能...";
        BattleLog = new List<string>();
        _resolutionStep = 0;
        _deflectionTriggeredThisRound = false;
        _characterShields = new Dictionary<Character, int>();
        _resolutionTimer = 0;
        _roundTimer = 0;
        _attackTimer = 0;
        _coinTimer = 0;
        _resolutionState = ResolutionState.WaitingForStep;
        
        // 处理第一回合开始事件
        List<Character> allCharacters = new List<Character>();
        allCharacters.AddRange(Players);
        allCharacters.AddRange(Enemies);
        
        // 第一回合开始时，随机设置速度并排序角色
        RandomizeAndSortCharactersBySpeed();
        
        // 优先处理阵营Buff：检测所有魏国武将
        List<Character> allWeiCharacters = allCharacters.Where(c => c.Faction == Faction.魏).ToList();
        List<Character> playerWeiCharacters = Players.Where(c => c.Faction == Faction.魏).ToList();
        List<Character> enemyWeiCharacters = Enemies.Where(c => c.Faction == Faction.魏).ToList();
        
        // 处理己方阵营Buff（先不加镇国）
        ProcessFactionBuffsWithoutZhenGuo(playerWeiCharacters, isPlayer: true);
        
        // 处理敌方阵营Buff（先不加镇国）
        ProcessFactionBuffsWithoutZhenGuo(enemyWeiCharacters, isPlayer: false);
        
        foreach (var player in Players)
        {
            if (player.ShouldDie())
            {
                continue;
            }
            
            // 重置角色属性
            player.ResetAttributes();
            // 处理夏侯惇/曹仁/司马懿/曹丕/张辽/曹操的特殊效果
            if (player is 夏侯惇 || player is TurnBasedRPG.Characters.Allies.曹仁 || player is TurnBasedRPG.Characters.Allies.司马懿 || player is TurnBasedRPG.Characters.Allies.曹丕 || player is TurnBasedRPG.Characters.Allies.张辽 || player is TurnBasedRPG.Characters.Allies.曹操)
            {
                // 使用反射调用OnTurnStart方法
                var method = player.GetType().GetMethod("OnTurnStart");
                if (method != null)
                {
                    // 所有角色都需要3个参数
                    method.Invoke(player, new object[] { _buffHandler, allCharacters, this });
                }
            }
        }
        
        foreach (var enemy in Enemies)
        {
            if (enemy.ShouldDie())
            {
                continue;
            }
            
            // 重置角色属性
            enemy.ResetAttributes();
            // 处理夏侯惇/曹仁/司马懿/曹丕/张辽/曹操的特殊效果
            if (enemy is 夏侯惇 || enemy is TurnBasedRPG.Characters.Allies.曹仁 || enemy is TurnBasedRPG.Characters.Allies.司马懿 || enemy is TurnBasedRPG.Characters.Allies.曹丕 || enemy is TurnBasedRPG.Characters.Allies.张辽 || enemy is TurnBasedRPG.Characters.Allies.曹操)
            {
                // 使用反射调用OnTurnStart方法
                var method = enemy.GetType().GetMethod("OnTurnStart");
                if (method != null)
                {
                    // 所有角色都需要3个参数
                    method.Invoke(enemy, new object[] { _buffHandler, allCharacters, this });
                }
            }
        }
        
        // 第一回合开始前：先处理所有魏国武将的决断初始强度（应用曹操的决断特殊效果）
        ApplyFirstRoundCaoCaoJueDuanSpecialEffect(allCharacters, playerWeiCharacters, isPlayer: true);
        ApplyFirstRoundCaoCaoJueDuanSpecialEffect(allCharacters, enemyWeiCharacters, isPlayer: false);
        
        // 现在添加镇国，确保镇国强度与决断-曹丕一致
        AddZhenGuoToWeiCharacters(playerWeiCharacters, isPlayer: true);
        AddZhenGuoToWeiCharacters(enemyWeiCharacters, isPlayer: false);
        
        // 韬晦强度处理将在所有行动槽被选取后进行
        
        // 敌怪先选技能
        SelectEnemyActions();
    }
    
    private void ProcessFactionBuffs(List<Character> weiCharacters, bool isPlayer)
    {
        if (weiCharacters.Count == 0)
        {
            return;
        }
        
        // 0. 首先处理武道独尊（第一顺位）
        foreach (var chara in weiCharacters)
        {
            if (chara is TurnBasedRPG.Characters.Allies.张辽)
            {
                // 移除已有的武道独尊
                var existingWuDaoDuZun = _buffHandler.GetBuffs(chara).FirstOrDefault(b => b is 武道独尊);
                if (existingWuDaoDuZun != null)
                {
                    _buffHandler.RemoveBuff(chara, existingWuDaoDuZun);
                }
                
                // 添加新的武道独尊
                _buffHandler.AddBuff(chara, new 武道独尊());
            }
        }
        
        // 1. 处理固阵/魏武固阵（仅当有夏侯惇时）
        bool hasXiaHouDun = weiCharacters.Any(c => c is TurnBasedRPG.Characters.Allies.夏侯惇);
        if (hasXiaHouDun)
        {
            foreach (var chara in weiCharacters)
            {
                // 移除已有的固阵/魏武固阵
                var existingBuffs = _buffHandler.GetBuffs(chara).Where(b => b is 固阵 || b is 魏武固阵).ToList();
                foreach (var buff in existingBuffs)
                {
                    _buffHandler.RemoveBuff(chara, buff);
                }
                
                // 添加新的阵营buff
                if (weiCharacters.Count == 1)
                {
                    _buffHandler.AddBuff(chara, new 固阵());
                }
                else
                {
                    _buffHandler.AddBuff(chara, new 魏武固阵());
                }
            }
        }
        else
        {
            // 移除所有角色的固阵/魏武固阵
            foreach (var chara in weiCharacters)
            {
                var existingBuffs = _buffHandler.GetBuffs(chara).Where(b => b is 固阵 || b is 魏武固阵).ToList();
                foreach (var buff in existingBuffs)
                {
                    _buffHandler.RemoveBuff(chara, buff);
                }
            }
        }
        
        // 2. 处理同仇之盾（仅当有曹仁时）
        bool hasCaoRen = weiCharacters.Any(c => c is TurnBasedRPG.Characters.Allies.曹仁);
        if (hasCaoRen)
        {
            int tongchouStrength = Math.Max(0, weiCharacters.Count - 1);
            
            foreach (var chara in weiCharacters)
            {
                // 移除已有的同仇之盾
                var existingTongchou = _buffHandler.GetBuffs(chara).FirstOrDefault(b => b is 同仇之盾);
                if (existingTongchou != null)
                {
                    _buffHandler.RemoveBuff(chara, existingTongchou);
                }
                
                // 添加新的同仇之盾
                bool isCaoRenCharacter = chara is TurnBasedRPG.Characters.Allies.曹仁;
                _buffHandler.AddBuff(chara, new 同仇之盾(isCaoRenCharacter, strength: tongchouStrength));
            }
        }
        else
        {
            // 移除所有角色的同仇之盾
            foreach (var chara in weiCharacters)
            {
                var existingTongchou = _buffHandler.GetBuffs(chara).FirstOrDefault(b => b is 同仇之盾);
                if (existingTongchou != null)
                {
                    _buffHandler.RemoveBuff(chara, existingTongchou);
                }
            }
        }
        
        // 3. 处理镇国（仅当同队有曹丕时）
        var caoPi = weiCharacters.FirstOrDefault(c => c is TurnBasedRPG.Characters.Allies.曹丕);
        if (caoPi != null)
        {
            // 获取曹丕的决断-曹丕状态强度
            var jueDuanBuff = _buffHandler.GetBuffs(caoPi).FirstOrDefault(b => b is 决断_曹丕);
            int zhenGuoStrength = jueDuanBuff != null ? jueDuanBuff.Strength : 1;
            
            foreach (var chara in weiCharacters)
            {
                // 移除已有的镇国
                var existingZhenGuo = _buffHandler.GetBuffs(chara).FirstOrDefault(b => b is 镇国);
                if (existingZhenGuo != null)
                {
                    _buffHandler.RemoveBuff(chara, existingZhenGuo);
                }
                
                // 添加新的镇国
                _buffHandler.AddBuff(chara, new 镇国(null, zhenGuoStrength));
            }
        }
    }
    
    private void ApplyFirstRoundCaoCaoJueDuanSpecialEffect(List<Character> allCharacters, List<Character> weiCharacters, bool isPlayer)
    {
        if (weiCharacters.Count == 0)
        {
            return;
        }
        
        // 检查同队是否有曹操的决断-曹操buff
        bool hasCaoCaoJueDuan = false;
        foreach (var chara in weiCharacters)
        {
            var caoCaoJueDuan = _buffHandler.GetBuffs(chara).Find(b => b is 决断_曹操);
            if (caoCaoJueDuan != null)
            {
                hasCaoCaoJueDuan = true;
                break;
            }
        }
        
        if (!hasCaoCaoJueDuan)
        {
            return;
        }
        
        // 遍历所有同队魏国武将，调整决断状态的初始强度
        foreach (var chara in weiCharacters)
        {
            var jueDuanBuffs = _buffHandler.GetBuffs(chara).Where(b => 
                b is 决断_夏侯惇 || 
                b is 决断_曹仁 || 
                b is 决断_司马懿 || 
                b is 决断_曹丕 || 
                b is 决断_张辽 || 
                b is 决断_曹操).ToList();
            
            foreach (var jueDuanBuff in jueDuanBuffs)
            {
                // 应用曹操决断的特殊效果：强度+1，且最小2，最大4
                jueDuanBuff.Strength = Math.Clamp(1 + 1, 2, 4);
            }
        }
    }
    
    private void ProcessFactionBuffsWithoutZhenGuo(List<Character> weiCharacters, bool isPlayer)
    {
        if (weiCharacters.Count == 0)
        {
            return;
        }
        
        // 0. 首先处理武道独尊（第一顺位）
        foreach (var chara in weiCharacters)
        {
            if (chara is TurnBasedRPG.Characters.Allies.张辽)
            {
                // 移除已有的武道独尊
                var existingWuDaoDuZun = _buffHandler.GetBuffs(chara).FirstOrDefault(b => b is 武道独尊);
                if (existingWuDaoDuZun != null)
                {
                    _buffHandler.RemoveBuff(chara, existingWuDaoDuZun);
                }
                
                // 添加新的武道独尊
                _buffHandler.AddBuff(chara, new 武道独尊());
            }
        }
        
        // 1. 处理固阵/魏武固阵（仅当有夏侯惇时）
        bool hasXiaHouDun = weiCharacters.Any(c => c is TurnBasedRPG.Characters.Allies.夏侯惇);
        if (hasXiaHouDun)
        {
            foreach (var chara in weiCharacters)
            {
                // 移除已有的固阵/魏武固阵
                var existingBuffs = _buffHandler.GetBuffs(chara).Where(b => b is 固阵 || b is 魏武固阵).ToList();
                foreach (var buff in existingBuffs)
                {
                    _buffHandler.RemoveBuff(chara, buff);
                }
                
                // 添加新的阵营buff
                if (weiCharacters.Count == 1)
                {
                    _buffHandler.AddBuff(chara, new 固阵());
                }
                else
                {
                    _buffHandler.AddBuff(chara, new 魏武固阵());
                }
            }
        }
        else
        {
            // 移除所有角色的固阵/魏武固阵
            foreach (var chara in weiCharacters)
            {
                var existingBuffs = _buffHandler.GetBuffs(chara).Where(b => b is 固阵 || b is 魏武固阵).ToList();
                foreach (var buff in existingBuffs)
                {
                    _buffHandler.RemoveBuff(chara, buff);
                }
            }
        }
        
        // 2. 处理同仇之盾（仅当有曹仁时）
        bool hasCaoRen = weiCharacters.Any(c => c is TurnBasedRPG.Characters.Allies.曹仁);
        if (hasCaoRen)
        {
            int tongchouStrength = Math.Max(0, weiCharacters.Count - 1);
            
            foreach (var chara in weiCharacters)
            {
                // 移除已有的同仇之盾
                var existingTongchou = _buffHandler.GetBuffs(chara).FirstOrDefault(b => b is 同仇之盾);
                if (existingTongchou != null)
                {
                    _buffHandler.RemoveBuff(chara, existingTongchou);
                }
                
                // 添加新的同仇之盾
                bool isCaoRenCharacter = chara is TurnBasedRPG.Characters.Allies.曹仁;
                _buffHandler.AddBuff(chara, new 同仇之盾(isCaoRenCharacter, strength: tongchouStrength));
            }
        }
        else
        {
            // 移除所有角色的同仇之盾
            foreach (var chara in weiCharacters)
            {
                var existingTongchou = _buffHandler.GetBuffs(chara).FirstOrDefault(b => b is 同仇之盾);
                if (existingTongchou != null)
                {
                    _buffHandler.RemoveBuff(chara, existingTongchou);
                }
            }
        }
        
        // 注意：3. 处理镇国将在后面单独进行
    }
    
    private void AddZhenGuoToWeiCharacters(List<Character> weiCharacters, bool isPlayer)
    {
        if (weiCharacters.Count == 0)
        {
            return;
        }
        
        // 处理镇国（仅当同队有曹丕时）
        var caoPi = weiCharacters.FirstOrDefault(c => c is TurnBasedRPG.Characters.Allies.曹丕);
        if (caoPi != null)
        {
            // 获取曹丕的决断-曹丕状态强度
            var jueDuanBuff = _buffHandler.GetBuffs(caoPi).FirstOrDefault(b => b is 决断_曹丕);
            int zhenGuoStrength = jueDuanBuff != null ? jueDuanBuff.Strength : 1;
            
            foreach (var chara in weiCharacters)
            {
                // 移除已有的镇国
                var existingZhenGuo = _buffHandler.GetBuffs(chara).FirstOrDefault(b => b is 镇国);
                if (existingZhenGuo != null)
                {
                    _buffHandler.RemoveBuff(chara, existingZhenGuo);
                }
                
                // 添加新的镇国，强度与曹丕的决断强度一致
                _buffHandler.AddBuff(chara, new 镇国(null, zhenGuoStrength));
            }
        }
    }
    
    private void AssignSlotsToCharacters()
    {
        AssignSlotsToCharacters(initialize: true);
    }
    
    private void AssignSlotsToCharacters(bool initialize)
    {
        // 分配己方行动槽
        if (Players.Count > 0)
        {
            if (initialize)
            {
                // 初始化模式：计算每个角色的固定行动槽数量
                int slotsPerPlayer = MaxSlots / Players.Count;
                int remainingSlots = MaxSlots % Players.Count;
                
                int slotIndex = 0;
                for (int i = 0; i < Players.Count; i++)
                {
                    int slotsToAssign = slotsPerPlayer + (i < remainingSlots ? 1 : 0);
                    Players[i].FixedSlotCount = slotsToAssign;
                    
                    for (int j = 0; j < slotsToAssign && slotIndex < MaxSlots; j++)
                    {
                        _slotToCharacterMap[PlayerSlots[slotIndex]] = Players[i];
                        // 设置行动槽的速度值与角色一致
                        PlayerSlots[slotIndex].Speed = Players[i].Speed;
                        
                        // 设置行动槽的SkillName，确保和SelectedSkill一致
                        if (PlayerSlots[slotIndex].SelectedSkill.HasValue)
                        {
                            AttackSkill skillToUse = PlayerSlots[slotIndex].SelectedSkill.Value;
                            // 检查沉默状态
                            if (_buffHandler.CheckBuff<Silence>(Players[i]))
                            {
                                skillToUse = AttackSkill.Skill1;
                            }
                            
                            // 检查神威状态，如果是张辽且有神威状态，则替换为Skill2
                            if (Players[i] is TurnBasedRPG.Characters.Allies.张辽 zhangLiao && _buffHandler.CheckBuff<神威>(Players[i]))
                            {
                                skillToUse = AttackSkill.Skill2;
                            }
                            
                            BaseSkill skill = Players[i].GetSkillByActionType(ActionType.Attack, skillToUse);
                            if (skill != null)
                            {
                                PlayerSlots[slotIndex].SkillName = skill.Name;
                            }
                        }
                        
                        slotIndex++;
                    }
                }
            }
            else
            {
                // 重新分配模式：根据固定行动槽数量重新分配
                // 先移除所有己方行动槽的映射
                List<ActionSlot> slotsToRemove = new List<ActionSlot>();
                foreach (var kvp in _slotToCharacterMap)
                {
                    if (PlayerSlots.Contains(kvp.Key))
                    {
                        slotsToRemove.Add(kvp.Key);
                    }
                }
                foreach (var slot in slotsToRemove)
                {
                    _slotToCharacterMap.Remove(slot);
                }
                
                List<ActionSlot> tempSlots = new List<ActionSlot>(PlayerSlots);
                int slotIndex = 0;
                foreach (var player in Players)
                {
                    for (int j = 0; j < player.FixedSlotCount && slotIndex < MaxSlots; j++)
                    {
                        _slotToCharacterMap[tempSlots[slotIndex]] = player;
                        // 设置行动槽的速度值与角色一致
                        tempSlots[slotIndex].Speed = player.Speed;
                        
                        // 设置行动槽的SkillName，确保和SelectedSkill一致
                        if (tempSlots[slotIndex].SelectedSkill.HasValue)
                        {
                            AttackSkill skillToUse = tempSlots[slotIndex].SelectedSkill.Value;
                            // 检查沉默状态
                            if (_buffHandler.CheckBuff<Silence>(player))
                            {
                                skillToUse = AttackSkill.Skill1;
                            }
                            
                            // 检查神威状态，如果是张辽且有神威状态，则替换为Skill2
                            if (player is TurnBasedRPG.Characters.Allies.张辽 zhangLiao && _buffHandler.CheckBuff<神威>(player))
                            {
                                skillToUse = AttackSkill.Skill2;
                            }
                            
                            BaseSkill skill = player.GetSkillByActionType(ActionType.Attack, skillToUse);
                            if (skill != null)
                            {
                                tempSlots[slotIndex].SkillName = skill.Name;
                            }
                        }
                        
                        slotIndex++;
                    }
                }
            }
        }
        
        // 分配敌方行动槽
        if (Enemies.Count > 0)
        {
            if (initialize)
            {
                // 初始化模式：计算每个角色的固定行动槽数量
                int slotsPerEnemy = MaxSlots / Enemies.Count;
                int remainingSlots = MaxSlots % Enemies.Count;
                
                int slotIndex = 0;
                for (int i = 0; i < Enemies.Count; i++)
                {
                    int slotsToAssign = slotsPerEnemy + (i < remainingSlots ? 1 : 0);
                    Enemies[i].FixedSlotCount = slotsToAssign;
                    
                    for (int j = 0; j < slotsToAssign && slotIndex < MaxSlots; j++)
                    {
                        _slotToCharacterMap[EnemySlots[slotIndex]] = Enemies[i];
                        // 设置行动槽的速度值与角色一致
                        EnemySlots[slotIndex].Speed = Enemies[i].Speed;
                        
                        // 设置行动槽为攻击类型
                        EnemySlots[slotIndex].Type = ActionType.Attack;
                        
                        // 设置行动槽的SkillName和其他属性
                        AttackSkill skillToUse = EnemySlots[slotIndex].SelectedSkill ?? AttackSkill.Skill1;
                        // 检查沉默状态
                        if (_buffHandler.CheckBuff<Silence>(Enemies[i]))
                        {
                            skillToUse = AttackSkill.Skill1;
                        }
                        
                        // 检查神威状态，如果是张辽且有神威状态，则替换为Skill2
                        if (Enemies[i] is TurnBasedRPG.Characters.Allies.张辽 zhangLiao && _buffHandler.CheckBuff<神威>(Enemies[i]))
                        {
                            skillToUse = AttackSkill.Skill2;
                        }
                        
                        BaseSkill skill = Enemies[i].GetSkillByActionType(ActionType.Attack, skillToUse);
                        if (skill != null)
                        {
                            EnemySlots[slotIndex].SkillName = skill.Name;
                            EnemySlots[slotIndex].BaseValue = skill.BaseValue;
                            EnemySlots[slotIndex].CoinValue = skill.CoinValue;
                            EnemySlots[slotIndex].CoinCount = skill.CoinCount;
                            EnemySlots[slotIndex].DamageType = skill.DamageType;
                            EnemySlots[slotIndex].AttackType = skill.AttackType;
                            EnemySlots[slotIndex].ExtraEffects = skill.ExtraEffects;
                        }
                        
                        slotIndex++;
                    }
                }
            }
            else
            {
                // 重新分配模式：根据固定行动槽数量重新分配
                // 先移除所有敌方行动槽的映射
                List<ActionSlot> slotsToRemove = new List<ActionSlot>();
                foreach (var kvp in _slotToCharacterMap)
                {
                    if (EnemySlots.Contains(kvp.Key))
                    {
                        slotsToRemove.Add(kvp.Key);
                    }
                }
                foreach (var slot in slotsToRemove)
                {
                    _slotToCharacterMap.Remove(slot);
                }
                
                List<ActionSlot> tempSlots = new List<ActionSlot>(EnemySlots);
                int slotIndex = 0;
                foreach (var enemy in Enemies)
                {
                    for (int j = 0; j < enemy.FixedSlotCount && slotIndex < MaxSlots; j++)
                    {
                        _slotToCharacterMap[tempSlots[slotIndex]] = enemy;
                        // 设置行动槽的速度值与角色一致
                        tempSlots[slotIndex].Speed = enemy.Speed;
                        
                        // 设置行动槽为攻击类型
                        tempSlots[slotIndex].Type = ActionType.Attack;
                        
                        // 设置行动槽的SkillName和其他属性
                        AttackSkill skillToUse = tempSlots[slotIndex].SelectedSkill ?? AttackSkill.Skill1;
                        // 检查沉默状态
                        if (_buffHandler.CheckBuff<Silence>(enemy))
                        {
                            skillToUse = AttackSkill.Skill1;
                        }
                        
                        // 检查神威状态，如果是张辽且有神威状态，则替换为Skill2
                        if (enemy is TurnBasedRPG.Characters.Allies.张辽 zhangLiao && _buffHandler.CheckBuff<神威>(enemy))
                        {
                            skillToUse = AttackSkill.Skill2;
                        }
                        
                        BaseSkill skill = enemy.GetSkillByActionType(ActionType.Attack, skillToUse);
                        if (skill != null)
                        {
                            tempSlots[slotIndex].SkillName = skill.Name;
                            tempSlots[slotIndex].BaseValue = skill.BaseValue;
                            tempSlots[slotIndex].CoinValue = skill.CoinValue;
                            tempSlots[slotIndex].CoinCount = skill.CoinCount;
                            tempSlots[slotIndex].DamageType = skill.DamageType;
                            tempSlots[slotIndex].AttackType = skill.AttackType;
                            tempSlots[slotIndex].ExtraEffects = skill.ExtraEffects;
                        }
                        
                        slotIndex++;
                    }
                }
            }
        }
        
        // 设置司马懿和曹丕的系统引用
        List<Character> allCharacters = new List<Character>();
        allCharacters.AddRange(Players);
        allCharacters.AddRange(Enemies);
        
        foreach (var character in allCharacters)
        {
            if (character is TurnBasedRPG.Characters.Allies.司马懿)
            {
                var simaYi = character as TurnBasedRPG.Characters.Allies.司马懿;
                simaYi.SetSlotsAndSystem(_buffHandler);
            }
            if (character is TurnBasedRPG.Characters.Allies.曹丕)
            {
                var caoPi = character as TurnBasedRPG.Characters.Allies.曹丕;
                caoPi.SetBuffHandler(_buffHandler);
            }
        }
    }
    
    private void RandomizeAndSortCharactersBySpeed()
    {
        Random random = new Random();
        
        // 确保所有角色的最终速度范围正确计算
        List<Character> allCharacters = new List<Character>();
        allCharacters.AddRange(Players);
        allCharacters.AddRange(Enemies);
        
        foreach (var character in allCharacters)
        {
            character.UpdateBuffs(_buffHandler);
        }
        
        // 随机设置己方角色的速度（使用最终速度范围）
        foreach (var player in Players)
        {
            player.Speed = random.Next(player.FinalMinSpeed, player.FinalMaxSpeed + 1);
        }
        
        // 随机设置敌方角色的速度（使用最终速度范围）
        foreach (var enemy in Enemies)
        {
            enemy.Speed = random.Next(enemy.FinalMinSpeed, enemy.FinalMaxSpeed + 1);
        }
        
        // 排序己方角色：速度从高到低，同速度按选择顺序
        Players = Players
            .OrderByDescending(c => c.Speed)
            .ThenBy(c => c.SelectionOrder)
            .ToList();
        
        // 排序敌方角色：速度从高到低，同速度按选择顺序
        Enemies = Enemies
            .OrderByDescending(c => c.Speed)
            .ThenBy(c => c.SelectionOrder)
            .ToList();
        
        // 根据新的角色顺序重新分配行动槽
        AssignSlotsToCharacters(initialize: false);
        
    }

    public void SetPlayerSlotAction(ActionType actionType)
    {
        if (CurrentPhase != BattlePhase.PlayerSelection || CurrentPlayerSlot >= MaxSlots)
            return;
        
        // 获取当前行动槽
        ActionSlot currentSlot = PlayerSlots[CurrentPlayerSlot];
        
        // 获取当前行动槽对应的角色（使用TryGetValue避免KeyNotFoundException）
        Character? currentCharacter = null;
        if (_slotToCharacterMap.TryGetValue(currentSlot, out var foundCharacter))
        {
            currentCharacter = foundCharacter;
        }
        else
        {
            return;
        }
        
        // 计算每个角色当回合可用的行动槽数量
        int characterSlotCount = 0;
        foreach (var slot in PlayerSlots)
        {
            if (_slotToCharacterMap.TryGetValue(slot, out var slotCharacter) && slotCharacter == currentCharacter)
            {
                characterSlotCount++;
            }
        }
        
        // 计算守备技能限制：(可用行动槽数量/2.5)，四舍五入，最低不低于1
        int defensiveLimit = (int)Math.Round(characterSlotCount / 2.5f);
        defensiveLimit = Math.Max(1, defensiveLimit);
        
        int healLimit = 1;
        
        // 检查守备技能限制（反击技能不受限制）
        if ((actionType == ActionType.Defend || actionType == ActionType.Dodge) && 
            CountDefensiveSlotsForCharacter(PlayerSlots, currentCharacter) >= defensiveLimit)
        {
            BattleLog.Add($"每个角色每回合最多只能使用{defensiveLimit}个守备技能！");
            return;
        }
        
        // 检查治疗技能限制
        if (actionType == ActionType.Heal && 
            CountHealSlotsForCharacter(PlayerSlots, currentCharacter) >= healLimit)
        {
            BattleLog.Add($"每个角色每回合最多只能使用{healLimit}个治疗技能！");
            return;
        }
        
        // 处理沉默debuff对玩家技能选择的影响
        AttackSkill selectedSkill;
        if (PlayerSlots[CurrentPlayerSlot].IsAlternativeSkillSelected && PlayerSlots[CurrentPlayerSlot].NextSkill.HasValue)
        {
            selectedSkill = PlayerSlots[CurrentPlayerSlot].NextSkill.Value;
        }
        else
        {
            selectedSkill = PlayerSlots[CurrentPlayerSlot].SelectedSkill ?? AttackSkill.Skill1;
        }
        
        if (_buffHandler.CheckBuff<Silence>(currentCharacter))
        {
            // 带有沉默debuff时，只能使用技能1
            selectedSkill = AttackSkill.Skill1;
        }
        
        // 获取技能并计算技能值
        BaseSkill skill = currentCharacter.GetSkillByActionType(actionType, selectedSkill);
        if (skill != null)
        {
            // 更新角色buff效果，计算最终攻击等级和防御等级
            currentCharacter.UpdateBuffs(_buffHandler);
            
            // 处理持有护盾时的伤害减免效果
            int shieldValue = GetCharacterShield(currentCharacter);
            foreach (var buff in _buffHandler.GetBuffs(currentCharacter))
            {
                if (buff is 固阵 guzhen)
                {
                    guzhen.ApplyShieldDamageReduction(currentCharacter, shieldValue);
                }
                else if (buff is 魏武固阵 weiwuGuzhen)
                {
                    weiwuGuzhen.ApplyShieldDamageReduction(currentCharacter, shieldValue);
                }
                else if (buff is Ganglie ganglie)
                {
                    ganglie.ApplyShieldDamageReduction(currentCharacter, shieldValue);
                }
            }
            
            currentCharacter.CalculateSkillValues(skill);
            PlayerSlots[CurrentPlayerSlot].SetAction(actionType, skill);
            
            // 如果行动槽还没有目标，则自动选择目标
            if (PlayerSlots[CurrentPlayerSlot].TargetSlot == null)
            {
                var autoTarget = GetAutoTargetForPlayerSlot(PlayerSlots[CurrentPlayerSlot]);
                if (autoTarget != null)
                {
                    SetSlotTarget(PlayerSlots[CurrentPlayerSlot], autoTarget);
                }
            }
            
            // 记录丢弃的技能
            if (currentCharacter is 夏侯惇 || currentCharacter is TurnBasedRPG.Characters.Allies.曹仁 || 
                currentCharacter is TurnBasedRPG.Characters.Allies.司马懿 || currentCharacter is TurnBasedRPG.Characters.Allies.张辽)
            {
                // 确定未被选择的技能
                AttackSkill? discardedSkill = null;
                if (PlayerSlots[CurrentPlayerSlot].IsAlternativeSkillSelected && PlayerSlots[CurrentPlayerSlot].NextSkill.HasValue)
                {
                    // 选择了备选技能，丢弃当前技能
                    discardedSkill = PlayerSlots[CurrentPlayerSlot].SelectedSkill;
                }
                else if (PlayerSlots[CurrentPlayerSlot].SelectedSkill.HasValue && PlayerSlots[CurrentPlayerSlot].NextSkill.HasValue)
                {
                    // 选择了当前技能，丢弃备选技能
                    discardedSkill = PlayerSlots[CurrentPlayerSlot].NextSkill;
                }
                
                if (discardedSkill.HasValue)
                {
                    // 根据技能类型确定技能等级
                    int skillLevel = discardedSkill.Value switch
                    {
                        AttackSkill.Skill1 => 1,
                        AttackSkill.Skill2 => 2,
                        AttackSkill.Skill3 => 3,
                        _ => 1
                    };
                    if (currentCharacter is 夏侯惇 xiahoudun)
                    {
                        xiahoudun.RecordDiscardedSkill(CurrentPlayerSlot + 1, skillLevel);
                    }
                    else if (currentCharacter is TurnBasedRPG.Characters.Allies.曹仁 caoren)
                    {
                        caoren.RecordDiscardedSkill(CurrentPlayerSlot + 1, skillLevel);
                    }
                    else if (currentCharacter is TurnBasedRPG.Characters.Allies.司马懿 simaYi)
                    {
                        simaYi.RecordDiscardedSkill(CurrentPlayerSlot + 1, skillLevel);
                    }
                    else if (currentCharacter is TurnBasedRPG.Characters.Allies.张辽 zhangLiao)
                    {
                        zhangLiao.RecordDiscardedSkill(CurrentPlayerSlot + 1, skillLevel);
                    }
                }
            }
            
            // 从技能池序列中移除所选技能
            PlayerSlots[CurrentPlayerSlot].MoveToNextSkill();
        }
        
        CurrentPlayerSlot++;
        
        // 检查是否所有行动槽都已选择技能
        bool allSlotsFilled = true;
        foreach (var slot in PlayerSlots)
        {
            if (slot.Type == ActionType.None)
            {
                allSlotsFilled = false;
                break;
            }
        }
        
        if (allSlotsFilled)
        {
            // 计算执行顺序
            CalculateExecutionOrder();
            
            CurrentPhase = BattlePhase.Resolution;
            _resolutionStep = 0;
            _deflectionTriggeredThisRound = false;
            BattleMessage = "战斗解析开始...";
            BattleLog.Clear();
            
            // 在所有行动槽被手动选取后，为己方与敌方司马懿、曹丕调用ProcessSkillExtraEffects
            List<Character> allCharactersForSkillEffects = new List<Character>();
            allCharactersForSkillEffects.AddRange(Players);
            allCharactersForSkillEffects.AddRange(Enemies);
            
            foreach (var player in Players)
            {
                if (player is TurnBasedRPG.Characters.Allies.司马懿)
                {
                    var simaYi = player as TurnBasedRPG.Characters.Allies.司马懿;
                    simaYi.ProcessSkillExtraEffects(PlayerSlots, _slotToCharacterMap, allCharactersForSkillEffects, this);
                }
                if (player is TurnBasedRPG.Characters.Allies.曹丕)
                {
                    var caoPi = player as TurnBasedRPG.Characters.Allies.曹丕;
                    caoPi.ProcessSkillExtraEffects(PlayerSlots, _slotToCharacterMap, allCharactersForSkillEffects, this);
                }
            }
            
            foreach (var enemy in Enemies)
            {
                if (enemy is TurnBasedRPG.Characters.Allies.司马懿)
                {
                    var simaYi = enemy as TurnBasedRPG.Characters.Allies.司马懿;
                    simaYi.ProcessSkillExtraEffects(EnemySlots, _slotToCharacterMap, allCharactersForSkillEffects, this);
                }
                if (enemy is TurnBasedRPG.Characters.Allies.曹丕)
                {
                    var caoPi = enemy as TurnBasedRPG.Characters.Allies.曹丕;
                    caoPi.ProcessSkillExtraEffects(EnemySlots, _slotToCharacterMap, allCharactersForSkillEffects, this);
                }
            }
            
            // 先处理威震逍遥津技能（在装备的该回合第一对行动槽开始拼点前立刻使用）
            ProcessWeiZhenXiaoYaoJinAtStartOfRound();
            
            // 先处理偏转，再生成护盾（不清空已有的护盾，保持叠加）
            ProcessDeflectionAtStartOfRound();
            ProcessDefenseSlots();
        }
        else
        {
            // 确保CurrentPlayerSlot不超过MaxSlots
            if (CurrentPlayerSlot >= MaxSlots)
            {
                CurrentPlayerSlot = 0;
            }
            BattleMessage = $"为行动槽 {CurrentPlayerSlot + 1} 选择行动";
        }
    }
    
    private int CountDefensiveSlotsForCharacter(List<ActionSlot> slots, Character character)
    {
        int count = 0;
        foreach (var slot in slots)
        {
            if ((slot.Type == ActionType.Defend || slot.Type == ActionType.Dodge) && 
                !slot.IsDestroyed && 
                _slotToCharacterMap[slot] == character)
                count++;
        }
        return count;
    }
    
    private int CountHealSlotsForCharacter(List<ActionSlot> slots, Character character)
    {
        int count = 0;
        foreach (var slot in slots)
        {
            if (slot.Type == ActionType.Heal && 
                !slot.IsDestroyed && 
                _slotToCharacterMap[slot] == character)
                count++;
        }
        return count;
    }

    private int CountDefensiveSlots(List<ActionSlot> slots)
    {
        int count = 0;
        foreach (var slot in slots)
        {
            if (slot.Type == ActionType.Defend || slot.Type == ActionType.Dodge)
                count++;
        }
        return count;
    }

    private int CountHealSlots(List<ActionSlot> slots)
    {
        int count = 0;
        foreach (var slot in slots)
        {
            if (slot.Type == ActionType.Heal)
                count++;
        }
        return count;
    }

    private void SelectEnemyActions()
    {
        // 先为敌方行动槽分配目标
        AssignEnemyTargets();
        
        Random random = new Random();
        
        // 为每个敌人角色跟踪技能使用次数
        Dictionary<Character, int> defensiveCounts = new Dictionary<Character, int>();
        Dictionary<Character, int> healCounts = new Dictionary<Character, int>();
        
        foreach (var enemy in Enemies)
        {
            defensiveCounts[enemy] = 0;
            healCounts[enemy] = 0;
        }
        
        for (int i = 0; i < MaxSlots; i++)
        {
            // 获取当前行动槽对应的敌人角色
            Character currentEnemy = _slotToCharacterMap[EnemySlots[i]];
            
            ActionType action;
            bool validAction = false;
            
            // 计算敌怪的血量百分比
            float healthPercentage = (float)currentEnemy.CurrentHealth / currentEnemy.MaxHealth;
            
            // 尝试找到有效的行动
            for (int attempt = 0; attempt < 10; attempt++)
            {
                // 根据血量百分比调整行动选择权重
                int rand = random.Next(100);
                
                if (healthPercentage > 0.7) // 血量较高时，更倾向于攻击
                {
                    if (rand < 70) // 70% 概率选择攻击
                        action = ActionType.Attack;
                    else if (rand < 85) // 15% 概率选择防御
                        action = ActionType.Defend;
                    else if (rand < 95) // 10% 概率选择闪避
                        action = ActionType.Dodge;
                    else // 5% 概率选择治疗
                        action = ActionType.Heal;
                }
                else if (healthPercentage > 0.4) // 血量中等时，平衡选择
                {
                    if (rand < 50) // 50% 概率选择攻击
                        action = ActionType.Attack;
                    else if (rand < 70) // 20% 概率选择防御
                        action = ActionType.Defend;
                    else if (rand < 90) // 20% 概率选择闪避
                        action = ActionType.Dodge;
                    else // 10% 概率选择治疗
                        action = ActionType.Heal;
                }
                else // 血量较低时，更倾向于防御和治疗
                {
                    if (rand < 30) // 30% 概率选择攻击
                        action = ActionType.Attack;
                    else if (rand < 50) // 20% 概率选择防御
                        action = ActionType.Defend;
                    else if (rand < 70) // 20% 概率选择闪避
                        action = ActionType.Dodge;
                    else // 30% 概率选择治疗
                        action = ActionType.Heal;
                }
                
                // 计算每个敌人角色当回合可用的行动槽数量
                int characterSlotCount = 0;
                foreach (var slot in EnemySlots)
                {
                    if (_slotToCharacterMap[slot] == currentEnemy)
                    {
                        characterSlotCount++;
                    }
                }
                
                // 计算守备技能限制：(可用行动槽数量/2.5)，四舍五入，最低不低于1
                int defensiveLimit = (int)Math.Round(characterSlotCount / 2.5f);
                defensiveLimit = Math.Max(1, defensiveLimit);
                
                int healLimit = 1;
                
                // 检查守备技能限制
                if ((action == ActionType.Defend || action == ActionType.Dodge) && defensiveCounts[currentEnemy] >= defensiveLimit)
                    continue;
                
                // 检查治疗技能限制
                if (action == ActionType.Heal && healCounts[currentEnemy] >= healLimit)
                    continue;
                
                // 处理沉默debuff对敌人技能选择的影响
                AttackSkill selectedSkill = EnemySlots[i].SelectedSkill ?? AttackSkill.Skill1;
                if (_buffHandler.CheckBuff<Silence>(currentEnemy))
                {
                    // 带有沉默debuff时，只能使用技能1
                    selectedSkill = AttackSkill.Skill1;
                }
                
                // 找到有效行动
                // 获取技能并计算技能值
                BaseSkill skill = currentEnemy.GetSkillByActionType(action, selectedSkill);
                if (skill != null)
                {
                    // 更新角色buff效果，计算最终攻击等级和防御等级
                    currentEnemy.UpdateBuffs(_buffHandler);
                    
                    // 处理持有护盾时的伤害减免效果
                    int enemyShieldValue = GetCharacterShield(currentEnemy);
                    foreach (var buff in _buffHandler.GetBuffs(currentEnemy))
                    {
                        if (buff is 固阵 guzhen)
                        {
                            guzhen.ApplyShieldDamageReduction(currentEnemy, enemyShieldValue);
                        }
                        else if (buff is 魏武固阵 weiwuGuzhen)
                        {
                            weiwuGuzhen.ApplyShieldDamageReduction(currentEnemy, enemyShieldValue);
                        }
                        else if (buff is Ganglie ganglie)
                        {
                            ganglie.ApplyShieldDamageReduction(currentEnemy, enemyShieldValue);
                        }
                    }
                    
                    currentEnemy.CalculateSkillValues(skill);
                    EnemySlots[i].SetAction(action, skill);
                    
                    if (action == ActionType.Defend || action == ActionType.Dodge)
                        defensiveCounts[currentEnemy]++;
                    else if (action == ActionType.Heal)
                        healCounts[currentEnemy]++;
                    
                    validAction = true;
                    break;
                }
                // 如果技能为null，继续尝试其他行动类型
            }
            
            // 如果找不到有效行动，默认使用攻击
            if (!validAction)
            {
                // 获取技能并计算技能值
                BaseSkill skill = currentEnemy.GetSkillByActionType(ActionType.Attack, EnemySlots[i].SelectedSkill);
                if (skill != null)
                {
                    // 更新角色buff效果，计算最终攻击等级和防御等级
                currentEnemy.UpdateBuffs(_buffHandler);
                
                // 处理持有护盾时的伤害减免效果
                int enemyShieldValue2 = GetCharacterShield(currentEnemy);
                foreach (var buff in _buffHandler.GetBuffs(currentEnemy))
                {
                    if (buff is 固阵 guzhen)
                    {
                        guzhen.ApplyShieldDamageReduction(currentEnemy, enemyShieldValue2);
                    }
                    else if (buff is 魏武固阵 weiwuGuzhen)
                    {
                        weiwuGuzhen.ApplyShieldDamageReduction(currentEnemy, enemyShieldValue2);
                    }
                    else if (buff is Ganglie ganglie)
                    {
                        ganglie.ApplyShieldDamageReduction(currentEnemy, enemyShieldValue2);
                    }
                }
                
                currentEnemy.CalculateSkillValues(skill);
                EnemySlots[i].SetAction(ActionType.Attack, skill);
                }
            }
        }
        
        // 敌怪选择完技能后，玩家开始选择
        CurrentPhase = BattlePhase.PlayerSelection;
        CurrentPlayerSlot = 0;
        BattleMessage = "为行动槽 1 选择行动";
    }

    private void ProcessDeflectionAtStartOfRound()
    {
        // 在回合开始时先判定是否有符合条件的行动槽对触发偏转
        for (int i = 0; i < Math.Min(PlayerSlots.Count, EnemySlots.Count); i++)
        {
            ActionSlot playerSlot = PlayerSlots[i];
            ActionSlot enemySlot = EnemySlots[i];
            
            if (playerSlot.IsDestroyed || enemySlot.IsDestroyed)
            {
                continue;
            }
            
            if (CheckDeflection(playerSlot, enemySlot))
            {
                TriggerDeflection(playerSlot, enemySlot);
                // 每回合只触发一次偏转
                break;
            }
        }
    }
    
    private void ProcessWeiZhenXiaoYaoJinAtStartOfRound()
    {
        // 在装备的该回合第一对行动槽开始拼点前立刻使用威震逍遥津
        
        // 检查己方张辽
        foreach (var slot in PlayerSlots)
        {
            if (slot.Type == ActionType.Attack && !slot.IsDestroyed && !slot.IsCompleted)
            {
                Character currentCharacter = _slotToCharacterMap[slot];
                if (currentCharacter is TurnBasedRPG.Characters.Allies.张辽 zhangLiao)
                {
                    if (slot.GetSkillName() == "威震逍遥津" && !zhangLiao.HasUsedWeiZhenXiaoYaoJin())
                    {
                        // 标记已使用
                        zhangLiao.SetHasUsedWeiZhenXiaoYaoJin(true);
                        
                        // 创建技能
                        BaseSkill weiZhenSkill = currentCharacter.GetSkillByActionType(ActionType.Attack, AttackSkill.Skill3);
                        if (weiZhenSkill != null)
                        {
                            // 1. 清除张辽持有的所有减益状态
                            var allBuffs = _buffHandler.GetBuffs(currentCharacter);
                            var debuffsToRemove = allBuffs.Where(b => !b.IsBuff).ToList();
                            foreach (var buff in debuffsToRemove)
                            {
                                _buffHandler.RemoveBuff(currentCharacter, buff);
                            }
                            
                            // 更新角色buff效果，计算最终攻击等级和防御等级
                            currentCharacter.UpdateBuffs(_buffHandler);
                            
                            // 计算技能值
                            currentCharacter.CalculateSkillValues(weiZhenSkill);
                            
                            // 2. 为张辽施加2回合[神威]
                            var shenWeiBuff = new 神威(2);
                            _buffHandler.AddBuff(currentCharacter, shenWeiBuff);
                            
                            // 标记张辽处于神威状态
                            zhangLiao.SetInShenWeiState(true);
                            
                            // 将张辽的所有行动槽替换为破溃
                            ReplaceAllZhangLiaoSkill3WithKuiPo(zhangLiao, PlayerSlots);
                            
                            // 额外将其他技能也替换为破溃
                            foreach (var actionSlot in PlayerSlots)
                            {
                                if (_slotToCharacterMap.ContainsKey(actionSlot) && 
                                    _slotToCharacterMap[actionSlot] == zhangLiao && 
                                    actionSlot.Type == ActionType.Attack && 
                                    !actionSlot.IsDestroyed && 
                                    !actionSlot.IsCompleted)
                                {
                                    // 将行动槽替换为破溃
                                    actionSlot.SkillName = "破溃";
                                    actionSlot.SelectedSkill = AttackSkill.Skill2;
                                    BaseSkill kuiPoSkill = zhangLiao.GetSkillByActionType(ActionType.Attack, AttackSkill.Skill2);
                                    if (kuiPoSkill != null)
                                    {
                                        zhangLiao.CalculateSkillValues(kuiPoSkill);
                                        actionSlot.SetAction(ActionType.Attack, kuiPoSkill);
                                    }
                                }
                            }
                            
                            // 第一段伤害：物理+穿刺，根据基础值进行完整的伤害计算
                            foreach (var enemy in Enemies)
                            {
                                if (enemy.CurrentHealth > 0)
                                {
                                    var damageResult = CalculateFullDamage(currentCharacter, enemy, weiZhenSkill.BaseValue, slot, DamageType.Physical, AttackType.Pierce);
                                    int damage = damageResult.FinalDamage;
                                    ApplyDamage(damage, enemy, slot, isDirectDamage: true);
                                    BattleLog.Add($"威震逍遥津（第一段）对{enemy.Name}造成{damage}点物理伤害");
                                }
                            }
                            
                            // 第二段伤害：真实+穿刺，根据基础值进行完整的伤害计算
                            // 自身每损失1%生命，此伤害临时获得1%最终伤害提升
                            float healthLossPercent = 1.0f - ((float)currentCharacter.CurrentHealth / currentCharacter.MaxHealth);
                            float damageBonus = healthLossPercent * 1.0f;
                            currentCharacter.FinalDamageIncrease += damageBonus;
                            
                            // 对敌方所有单位造成真实伤害
                            foreach (var enemy in Enemies)
                            {
                                if (enemy.CurrentHealth > 0)
                                {
                                    var damageResult = CalculateFullDamage(currentCharacter, enemy, weiZhenSkill.BaseValue, slot, DamageType.True, AttackType.Pierce);
                                    int damage = damageResult.FinalDamage;
                                    ApplyDamage(damage, enemy, slot, isDirectDamage: true);
                                    BattleLog.Add($"威震逍遥津（第二段）对{enemy.Name}造成{damage}点真实伤害");
                                }
                            }
                            
                            // 恢复最终伤害加成
                            currentCharacter.FinalDamageIncrease -= damageBonus;
                            
                            // 将所有张辽的技能3替换为破溃
                            ReplaceAllZhangLiaoSkill3WithKuiPo(zhangLiao, PlayerSlots);
                        }
                        break;
                    }
                }
            }
        }
        
        // 检查敌方张辽
        foreach (var slot in EnemySlots)
        {
            if (slot.Type == ActionType.Attack && !slot.IsDestroyed && !slot.IsCompleted)
            {
                Character currentCharacter = _slotToCharacterMap[slot];
                if (currentCharacter is TurnBasedRPG.Characters.Allies.张辽 zhangLiao)
                {
                    if (slot.GetSkillName() == "威震逍遥津" && !zhangLiao.HasUsedWeiZhenXiaoYaoJin())
                    {
                        // 标记已使用
                        zhangLiao.SetHasUsedWeiZhenXiaoYaoJin(true);
                        
                        // 创建技能
                        BaseSkill weiZhenSkill = currentCharacter.GetSkillByActionType(ActionType.Attack, AttackSkill.Skill3);
                        if (weiZhenSkill != null)
                        {
                            // 1. 清除张辽持有的所有减益状态
                            var allBuffs = _buffHandler.GetBuffs(currentCharacter);
                            var debuffsToRemove = allBuffs.Where(b => !b.IsBuff).ToList();
                            foreach (var buff in debuffsToRemove)
                            {
                                _buffHandler.RemoveBuff(currentCharacter, buff);
                            }
                            
                            // 更新角色buff效果，计算最终攻击等级和防御等级
                            currentCharacter.UpdateBuffs(_buffHandler);
                            
                            // 计算技能值
                            currentCharacter.CalculateSkillValues(weiZhenSkill);
                            
                            // 2. 为张辽施加2回合[神威]
                            var shenWeiBuff = new 神威(2);
                            _buffHandler.AddBuff(currentCharacter, shenWeiBuff);
                            
                            // 标记张辽处于神威状态
                            zhangLiao.SetInShenWeiState(true);
                            
                            // 将张辽的所有行动槽替换为破溃
                            ReplaceAllZhangLiaoSkill3WithKuiPo(zhangLiao, EnemySlots);
                            
                            // 额外将其他技能也替换为破溃
                            foreach (var actionSlot in EnemySlots)
                            {
                                if (_slotToCharacterMap.ContainsKey(actionSlot) && 
                                    _slotToCharacterMap[actionSlot] == zhangLiao && 
                                    actionSlot.Type == ActionType.Attack && 
                                    !actionSlot.IsDestroyed && 
                                    !actionSlot.IsCompleted)
                                {
                                    // 将行动槽替换为破溃
                                    actionSlot.SkillName = "破溃";
                                    actionSlot.SelectedSkill = AttackSkill.Skill2;
                                    BaseSkill kuiPoSkill = zhangLiao.GetSkillByActionType(ActionType.Attack, AttackSkill.Skill2);
                                    if (kuiPoSkill != null)
                                    {
                                        zhangLiao.CalculateSkillValues(kuiPoSkill);
                                        actionSlot.SetAction(ActionType.Attack, kuiPoSkill);
                                    }
                                }
                            }
                            
                            // 第一段伤害：物理+穿刺，根据基础值进行完整的伤害计算
                            foreach (var player in Players)
                            {
                                if (player.CurrentHealth > 0)
                                {
                                    var damageResult = CalculateFullDamage(currentCharacter, player, weiZhenSkill.BaseValue, slot, DamageType.Physical, AttackType.Pierce);
                                    int damage = damageResult.FinalDamage;
                                    ApplyDamage(damage, player, slot, isDirectDamage: true);
                                    BattleLog.Add($"威震逍遥津（第一段）对{player.Name}造成{damage}点物理伤害");
                                }
                            }
                            
                            // 第二段伤害：真实+穿刺，根据基础值进行完整的伤害计算
                            // 自身每损失1%生命，此伤害临时获得0.5%最终伤害提升
                            float healthLossPercent = 1.0f - ((float)currentCharacter.CurrentHealth / currentCharacter.MaxHealth);
                            float damageBonus = healthLossPercent * 0.5f;
                            currentCharacter.FinalDamageIncrease += damageBonus;
                            
                            // 对敌方所有单位造成真实伤害
                            foreach (var player in Players)
                            {
                                if (player.CurrentHealth > 0)
                                {
                                    var damageResult = CalculateFullDamage(currentCharacter, player, weiZhenSkill.BaseValue, slot, DamageType.True, AttackType.Pierce);
                                    int damage = damageResult.FinalDamage;
                                    ApplyDamage(damage, player, slot, isDirectDamage: true);
                                    BattleLog.Add($"威震逍遥津（第二段）对{player.Name}造成{damage}点真实伤害");
                                }
                            }
                            
                            // 恢复最终伤害加成
                            currentCharacter.FinalDamageIncrease -= damageBonus;
                            
                            // 将所有张辽的技能3替换为破溃
                            ReplaceAllZhangLiaoSkill3WithKuiPo(zhangLiao, EnemySlots);
                        }
                        break;
                    }
                }
            }
        }
    }
    
    private DamageCalculationResult CalculateFullDamage(Character attacker, Character target, int baseValue, ActionSlot slot, DamageType damageType, AttackType attackType)
    {
        DamageCalculationResult result = new DamageCalculationResult();
        result.BaseValue = baseValue;
        
        // 保存原有的伤害类型
        DamageType originalDamageType = slot.DamageType;
        AttackType originalAttackType = slot.AttackType;
        
        // 临时修改行动槽的伤害类型
        slot.DamageType = damageType;
        slot.AttackType = attackType;
        
        // 确定技能采用的攻防等级
        int skillLevel = attacker.FinalAttackLevel;
        
        // 检查是否受到魏武固阵状态影响，使用防御等级进行计算
        bool hasWeiWuGuZhen = false;
        var buffs = _buffHandler.GetBuffs(attacker);
        foreach (var buff in buffs)
        {
            if (buff.Name == "魏武固阵")
            {
                hasWeiWuGuZhen = true;
                break;
            }
        }
        // 如果攻击者有武道独尊，忽略魏武固阵
        if (hasWeiWuGuZhen && !attacker.HasWuDaoDuZun)
        {
            // 使用防御等级进行计算
            skillLevel = attacker.FinalDefenseLevel;
        }
        
        // 确定对方技能采用的攻防等级
        int targetSkillLevel = target.FinalDefenseLevel;
        bool targetHasWeiWuGuZhen = false;
        var targetBuffs = _buffHandler.GetBuffs(target);
        foreach (var buff in targetBuffs)
        {
            if (buff.Name == "魏武固阵")
            {
                targetHasWeiWuGuZhen = true;
                break;
            }
        }
        // 如果目标有武道独尊，忽略魏武固阵
        if (targetHasWeiWuGuZhen && !target.HasWuDaoDuZun)
        {
            // 使用防御等级进行计算
            targetSkillLevel = target.FinalDefenseLevel;
        }
        
        // 根据攻击对抗类型和技能类型计算skillLevelMultiplier（攻防等级修正乘区）
        double skillLevelMultiplier;
        double multiplierRate = 0.03; // 默认倍率：3%
        
        // 无论是否对抗防御技能，都使用目标的防御等级进行计算
        int targetLevelForCalculation = targetSkillLevel;
        int levelDifference = skillLevel - targetLevelForCalculation;
        skillLevelMultiplier = 1.0 + ((double)levelDifference * multiplierRate);
        
        // skillLevelMultiplier的计算结果不低于0.2
        skillLevelMultiplier = Math.Max(0.2, skillLevelMultiplier);
        result.SkillLevelMultiplier = skillLevelMultiplier;
        
        // 一类增伤乘区damageMultiplier：(1+攻击者伤害提升-目标伤害减免)，最低0.2
        float damageMultiplier = (1 + attacker.DamageIncrease - target.DamageReduction);
        damageMultiplier = Math.Max(0.2f, damageMultiplier);
        result.DamageMultiplier = damageMultiplier;
        
        // 获取伤害种类抗性
        float damageTypeResistance = 1.0f;
        switch (damageType)
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
        result.DamageTypeResistance = damageTypeResistance;
        
        // 获取攻击方式抗性
        float attackTypeResistance = 1.0f;
        switch (attackType)
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
        result.AttackTypeResistance = attackTypeResistance;
        
        // 确保抗性值不低于0.1
        damageTypeResistance = Math.Max(0.1f, damageTypeResistance);
        attackTypeResistance = Math.Max(0.1f, attackTypeResistance);
        
        // 最终伤害乘区finalDamageMultiplier：(1+攻击者最终伤害提升-目标最终伤害减免)
        float finalDamageMultiplier = (1 + attacker.FinalDamageIncrease - target.FinalDamageReduction);
        
        // 武道独尊效果：若张辽的攻击等级高于目标，则在攻击时临时获得（攻击等级差*0.75）%伤害增加，上限为75%
        if (attacker.HasWuDaoDuZun)
        {
            int wuDaoLevelDifference = attacker.FinalAttackLevel - target.FinalDefenseLevel;
            if (wuDaoLevelDifference > 0)
            {
                float damageBonus = Math.Min(wuDaoLevelDifference * 0.0075f, 0.75f);
                finalDamageMultiplier += damageBonus;
            }
        }
        
        // finalDamageMultiplier的计算结果不低于0.2
        finalDamageMultiplier = Math.Max(0.2f, finalDamageMultiplier);
        result.FinalDamageMultiplier = finalDamageMultiplier;
        
        // 计算暴击
        float critChance = attacker.CritRate + attacker.FinalCritRate - target.CritResistance - target.FinalCritResistance;
        critChance = Math.Max(0f, critChance);
        
        // 这里只是简单判断是否暴击（实际应该用随机数）
        // 为了简化，这里先假设不暴击，实际在调用处会处理
        result.IsCrit = false;
        result.CritDamageMultiplier = 1.0f;
        
        // 计算最终伤害（不计算暴击）
        int damage = (int)(baseValue * skillLevelMultiplier * damageMultiplier * finalDamageMultiplier * attackTypeResistance * damageTypeResistance);
        result.FinalDamage = damage;
        
        // 恢复原有的伤害类型
        slot.DamageType = originalDamageType;
        slot.AttackType = originalAttackType;
        
        return result;
    }
    
    private void ReplaceAllZhangLiaoSkill3WithKuiPo(Character zhangLiao, List<ActionSlot> slots)
    {
        foreach (var slot in slots)
        {
            if (_slotToCharacterMap.ContainsKey(slot) && _slotToCharacterMap[slot] == zhangLiao)
            {
                if (slot.Type == ActionType.Attack && !slot.IsDestroyed)
                {
                    // 检查当前技能是否为威震逍遥津
                    if (slot.GetSkillName() == "威震逍遥津" || 
                        (slot.SelectedSkill.HasValue && slot.SelectedSkill.Value == AttackSkill.Skill3))
                    {
                        // 用技能2替换
                        slot.SkillName = "破溃";
                        slot.SelectedSkill = AttackSkill.Skill2;
                        BaseSkill kuiPoSkill = zhangLiao.GetSkillByActionType(ActionType.Attack, AttackSkill.Skill2);
                        if (kuiPoSkill != null)
                        {
                            zhangLiao.CalculateSkillValues(kuiPoSkill);
                            slot.SetAction(ActionType.Attack, kuiPoSkill);
                        }
                    }
                }
            }
        }
    }

    private void ProcessDefenseSlots()
    {
        // 处理防御技能
        foreach (var slot in PlayerSlots)
        {
            if (slot.Type == ActionType.Defend && !slot.IsDestroyed && !slot.IsCompleted)
            {
                // 获取当前行动槽对应的角色
                Character currentCharacter = _slotToCharacterMap[slot];
                
                // 如果角色已死亡，跳过
                if (currentCharacter.ShouldDie())
                {
                    slot.IsDestroyed = true;
                    continue;
                }
                
                // 处理夏侯惇的防御技能特殊效果
                if (currentCharacter is 夏侯惇)
                {
                    // 处理防御技能的特殊效果：回合开始时对自己施加持续回合数为1的刚烈buff
                    ((夏侯惇)currentCharacter).HandleDefendSkillEffect(_buffHandler);
                }
                
                // 在处理防御时投掷硬币
                slot.FlipCoins(currentCharacter.Morale, currentCharacter, _buffHandler);
                int finalValue = slot.BaseValue + slot.GetCurrentCoinValue();
                int baseShieldValue = (int)(finalValue * currentCharacter.ShieldEffectiveness); // 使用角色的ShieldEffectiveness属性来计算护盾值
                // 应用护盾修正
                int shieldValue = (int)(baseShieldValue * (1 + currentCharacter.ShieldAdjustment));
                
                int totalShieldForCaoCao = shieldValue;
                Character? targetForShield = null;
                
                // 如果是曹操的临危授命，先寻找护盾最少的魏国武将
                if (currentCharacter is TurnBasedRPG.Characters.Allies.曹操 && slot.GetSkillName() == "临危授命")
                {
                    List<Character> allies = currentCharacter.IsAlly ? Players : Enemies;
                    List<Character> validTargets = new List<Character>();
                    
                    foreach (var ally in allies)
                    {
                        if (ally.CurrentHealth > 0 && ally.Faction == Faction.魏 && ally != currentCharacter)
                        {
                            validTargets.Add(ally);
                        }
                    }
                    
                    if (validTargets.Count > 0)
                    {
                        // 找到护盾最少的目标
                        targetForShield = validTargets[0];
                        int minShield = GetCharacterShield(targetForShield);
                        foreach (var target in validTargets)
                        {
                            int targetShield = GetCharacterShield(target);
                            if (targetShield < minShield)
                            {
                                minShield = targetShield;
                                targetForShield = target;
                            }
                        }
                    }
                    else
                    {
                        // 没有满足条件的目标，为自己提升50%护盾
                        totalShieldForCaoCao = (int)(shieldValue * 1.5f);
                    }
                }
                
                // 应用最终护盾值
                if (!_characterShields.ContainsKey(currentCharacter))
                {
                    _characterShields[currentCharacter] = 0;
                }
                _characterShields[currentCharacter] += totalShieldForCaoCao;
                slot.IsCompleted = true;
                BattleLog.Add($"{currentCharacter.Name} 使用防御生成 {totalShieldForCaoCao} 点护盾");
                
                // 如果是曹操的临危授命，处理后续效果
                if (currentCharacter is TurnBasedRPG.Characters.Allies.曹操 && slot.GetSkillName() == "临危授命")
                {
                    var caoCaoSkill = new TurnBasedRPG.Systems.SkillManagement.CaoCaoSkill(this);
                    caoCaoSkill.HandleLinweiShoumingPostDefend(currentCharacter, totalShieldForCaoCao, targetForShield, _buffHandler);
                }
            }
        }
        
        foreach (var slot in EnemySlots)
        {
            if (slot.Type == ActionType.Defend && !slot.IsDestroyed && !slot.IsCompleted)
            {
                // 获取当前行动槽对应的角色
                Character currentCharacter = _slotToCharacterMap[slot];
                
                // 如果角色已死亡，跳过
                if (currentCharacter.ShouldDie())
                {
                    slot.IsDestroyed = true;
                    continue;
                }
                
                // 处理夏侯惇的防御技能特殊效果
                if (currentCharacter is 夏侯惇)
                {
                    // 处理防御技能的特殊效果：回合开始时对自己施加持续回合数为1的刚烈buff
                    ((夏侯惇)currentCharacter).HandleDefendSkillEffect(_buffHandler);
                }
                
                // 在处理防御时投掷硬币
                slot.FlipCoins(0, currentCharacter, _buffHandler); // 敌怪不使用士气
                int finalValue = slot.BaseValue + slot.GetCurrentCoinValue();
                int baseShieldValue = (int)(finalValue * currentCharacter.ShieldEffectiveness); // 使用角色的ShieldEffectiveness属性来计算护盾值
                // 应用护盾修正
                int shieldValue = (int)(baseShieldValue * (1 + currentCharacter.ShieldAdjustment));
                
                int totalShieldForCaoCao = shieldValue;
                Character? targetForShield = null;
                
                // 如果是曹操的临危授命，先寻找护盾最少的魏国武将
                if (currentCharacter is TurnBasedRPG.Characters.Allies.曹操 && slot.GetSkillName() == "临危授命")
                {
                    List<Character> allies = currentCharacter.IsAlly ? Players : Enemies;
                    List<Character> validTargets = new List<Character>();
                    
                    foreach (var ally in allies)
                    {
                        if (ally.CurrentHealth > 0 && ally.Faction == Faction.魏 && ally != currentCharacter)
                        {
                            validTargets.Add(ally);
                        }
                    }
                    
                    if (validTargets.Count > 0)
                    {
                        // 找到护盾最少的目标
                        targetForShield = validTargets[0];
                        int minShield = GetCharacterShield(targetForShield);
                        foreach (var target in validTargets)
                        {
                            int targetShield = GetCharacterShield(target);
                            if (targetShield < minShield)
                            {
                                minShield = targetShield;
                                targetForShield = target;
                            }
                        }
                    }
                    else
                    {
                        // 没有满足条件的目标，为自己提升50%护盾
                        totalShieldForCaoCao = (int)(shieldValue * 1.5f);
                    }
                }
                
                // 应用最终护盾值
                if (!_characterShields.ContainsKey(currentCharacter))
                {
                    _characterShields[currentCharacter] = 0;
                }
                _characterShields[currentCharacter] += totalShieldForCaoCao;
                slot.IsCompleted = true;
                BattleLog.Add($"{currentCharacter.Name} 使用防御生成 {totalShieldForCaoCao} 点护盾");
                
                // 如果是曹操的临危授命，处理后续效果
                if (currentCharacter is TurnBasedRPG.Characters.Allies.曹操 && slot.GetSkillName() == "临危授命")
                {
                    var caoCaoSkill = new TurnBasedRPG.Systems.SkillManagement.CaoCaoSkill(this);
                    caoCaoSkill.HandleLinweiShoumingPostDefend(currentCharacter, totalShieldForCaoCao, targetForShield, _buffHandler);
                }
            }
        }
        
        // 闪避技能与攻击技能的交互将在ResolveSlotPair方法中处理，确保只在轮到对应行动槽时才进行对抗
    }

    public void ResolveNextStep()
    {
        if (CurrentPhase != BattlePhase.Resolution || _resolutionStep >= MaxSlots)
        {
            return;
        }
        
        ResolveSlotPair(_resolutionStep);
        
        if (CheckDeathCondition())
        {
            ForceBattleEnd();
            return;
        }
        
        _resolutionStep++;
        
        // 移除这部分逻辑，让UpdateResolution方法来处理回合切换
        // if (_resolutionStep >= MAX_SLOTS)
        // {
        //     CheckBattleEnd();
        //     if (!BattleEnded)
        //     {
        //         ResetBattleForNextRound();
        //     }
        // }
    }

    private bool CheckDeathCondition()
    {
        // 检查是否所有玩家角色死亡
        bool allPlayersDead = true;
        foreach (var player in Players)
        {
            if (!player.ShouldDie())
            {
                allPlayersDead = false;
                break;
            }
        }
        
        // 检查是否所有敌人角色死亡
        bool allEnemiesDead = true;
        foreach (var enemy in Enemies)
        {
            if (!enemy.ShouldDie())
            {
                allEnemiesDead = false;
                break;
            }
        }
        
        return allPlayersDead || allEnemiesDead;
    }
    
    // 检查某个角色是否死亡
    private bool IsCharacterDead(Character character)
    {
        return character.ShouldDie();
    }
    
    // 检查某个行动槽的目标角色是否死亡
    private bool IsTargetDeadForSlot(ActionSlot playerSlot, ActionSlot enemySlot, int slotIndex)
    {
        Character playerCharacter = _slotToCharacterMap[playerSlot];
        Character enemyCharacter = _slotToCharacterMap[enemySlot];
        
        // 如果是玩家攻击，检查敌人是否死亡
        if (playerSlot.Type == ActionType.Attack && !playerSlot.IsDestroyed && !playerSlot.IsCompleted)
        {
            if (IsCharacterDead(enemyCharacter))
            {
                return true;
            }
        }
        
        // 如果是敌人攻击，检查玩家是否死亡
        if (enemySlot.Type == ActionType.Attack && !enemySlot.IsDestroyed && !enemySlot.IsCompleted)
        {
            if (IsCharacterDead(playerCharacter))
            {
                return true;
            }
        }
        
        return false;
    }

    private void ForceBattleEnd()
    {
        // 检查是否有玩家角色死亡
        foreach (var player in Players)
        {
            if (player.ShouldDie())
            {
                player.OnDeath();
                BattleLog.Add($"{player.Name} has fallen! Battle ends immediately!");
            }
        }
        
        // 检查是否有敌人角色死亡
        foreach (var enemy in Enemies)
        {
            if (enemy.ShouldDie())
            {
                enemy.OnDeath();
                BattleLog.Add($"{enemy.Name} has been defeated! Battle ends immediately!");
            }
        }
        
        CheckBattleEnd();
    }

    private void ResolveSlotPair(int slotIndex)
    {
        ActionSlot playerSlot = PlayerSlots[slotIndex];
        ActionSlot enemySlot = EnemySlots[slotIndex];
        
        // 获取当前行动槽对应的角色
        Character playerCharacter = _slotToCharacterMap[playerSlot];
        Character enemyCharacter = _slotToCharacterMap[enemySlot];
        
        // 检查角色本身是否死亡
        if (IsCharacterDead(playerCharacter) && IsCharacterDead(enemyCharacter))
        {
            BattleLog.Add("Both characters are dead");
            // 标记行动槽为已完成
            playerSlot.IsDestroyed = true;
            playerSlot.IsCompleted = true;
            enemySlot.IsDestroyed = true;
            enemySlot.IsCompleted = true;
            return;
        }
        
        // 如果玩家角色死亡，标记其行动槽为已摧毁
        if (IsCharacterDead(playerCharacter))
        {
            playerSlot.IsDestroyed = true;
            playerSlot.IsCompleted = true;
        }
        
        // 如果敌方角色死亡，标记其行动槽为已摧毁
        if (IsCharacterDead(enemyCharacter))
        {
            enemySlot.IsDestroyed = true;
            enemySlot.IsCompleted = true;
        }
        
        // 为攻击技能寻找合适的目标
        Character effectivePlayerTarget = enemyCharacter;
        Character effectiveEnemyTarget = playerCharacter;
        
        // 如果玩家的攻击目标已死亡，寻找新的随机敌方目标
        if (playerSlot.Type == ActionType.Attack && !playerSlot.IsDestroyed && !playerSlot.IsCompleted && IsCharacterDead(enemyCharacter))
        {
            // 寻找存活的敌方目标
            List<Character> aliveEnemies = Enemies.Where(e => !e.ShouldDie()).ToList();
            if (aliveEnemies.Count > 0)
            {
                Random random = new Random();
                effectivePlayerTarget = aliveEnemies[random.Next(aliveEnemies.Count)];
            }
        }
        
        // 如果敌方的攻击目标已死亡，寻找新的随机玩家目标
        if (enemySlot.Type == ActionType.Attack && !enemySlot.IsDestroyed && !enemySlot.IsCompleted && IsCharacterDead(playerCharacter))
        {
            // 寻找存活的玩家目标
            List<Character> alivePlayers = Players.Where(p => !p.ShouldDie()).ToList();
            if (alivePlayers.Count > 0)
            {
                Random random = new Random();
                effectiveEnemyTarget = alivePlayers[random.Next(alivePlayers.Count)];
            }
        }
        
        // 处理夏侯惇的技能效果
        if (playerCharacter is 夏侯惇 && !IsCharacterDead(playerCharacter))
        {
            // 处理攻击技能2的特殊效果：回合开始时使同队所有魏国武将获得2级忍耐，持续2回合
            if (playerSlot.Type == ActionType.Attack && playerSlot.GetSkillName() == "拔矢啖睛")
            {
                List<Character> allCharacters = Players.Concat(Enemies).ToList();
                ((夏侯惇)playerCharacter).HandleAttackSkill2Effect(_buffHandler, allCharacters);
            }
            // 处理攻击技能3的特殊效果：回合开始时使自身获得4级忍耐与4级防御等级提升，持续1回合
            if (playerSlot.Type == ActionType.Attack && playerSlot.GetSkillName() == "铁壁战吼")
            {
                ((夏侯惇)playerCharacter).HandleAttackSkill3Effect(_buffHandler);
            }
            // 处理防御技能的特殊效果
            if (playerSlot.Type == ActionType.Defend && playerSlot.GetSkillName() == "刚烈之魂")
            {
                if (playerCharacter is 夏侯惇)
                {
                    ((夏侯惇)playerCharacter).HandleDefendSkillEffect(_buffHandler);
                }
            }
            
            // 处理敌人防御技能的特殊效果
            if (enemySlot.Type == ActionType.Defend && enemySlot.GetSkillName() == "刚烈之魂" && !IsCharacterDead(enemyCharacter))
            {
                if (enemyCharacter is 夏侯惇)
                {
                    ((夏侯惇)enemyCharacter).HandleDefendSkillEffect(_buffHandler);
                }
            }
        }
        
        if (playerSlot.IsDestroyed && enemySlot.IsDestroyed)
        {
            BattleLog.Add("Both slots are destroyed");
            return;
        }
        
        // 获取当前行动槽对应的角色
        Character currentPlayer = _slotToCharacterMap[playerSlot];
        Character currentEnemy = _slotToCharacterMap[enemySlot];
        
        // 处理闪避技能与攻击技能的交互（使用新的闪避逻辑）
        // 检查玩家攻击技能是否需要与敌人闪避技能对抗
        if (playerSlot.Type == ActionType.Attack && !playerSlot.IsDestroyed && !playerSlot.IsCompleted && !IsCharacterDead(currentPlayer))
        {
            // 检查所有敌方闪避技能
            foreach (var enemyDodgeSlot in EnemySlots)
            {
                Character dodgeTarget = _slotToCharacterMap[enemyDodgeSlot];
                if (enemyDodgeSlot.Type == ActionType.Dodge && !enemyDodgeSlot.IsDestroyed && !enemyDodgeSlot.IsCompleted && !IsCharacterDead(dodgeTarget))
                {
                    // 使用新的闪避检查逻辑
                    if (ShouldDodgeEngageWithAttack(enemyDodgeSlot, playerSlot))
                    {
                        ResolveAttackVsDodge(playerSlot, enemyDodgeSlot, dodgeTarget);
                        if (playerSlot.IsDestroyed || playerSlot.IsCompleted)
                        {
                            return;
                        }
                    }
                }
            }
        }
        
        // 检查敌人攻击技能是否需要与玩家闪避技能对抗
        if (enemySlot.Type == ActionType.Attack && !enemySlot.IsDestroyed && !enemySlot.IsCompleted && !IsCharacterDead(currentEnemy))
        {
            // 检查所有我方闪避技能
            foreach (var playerDodgeSlot in PlayerSlots)
            {
                Character dodgeTarget = _slotToCharacterMap[playerDodgeSlot];
                if (playerDodgeSlot.Type == ActionType.Dodge && !playerDodgeSlot.IsDestroyed && !playerDodgeSlot.IsCompleted && !IsCharacterDead(dodgeTarget))
                {
                    // 使用新的闪避检查逻辑
                    if (ShouldDodgeEngageWithAttack(playerDodgeSlot, enemySlot))
                    {
                        ResolveAttackVsDodge(enemySlot, playerDodgeSlot, dodgeTarget);
                        if (enemySlot.IsDestroyed || enemySlot.IsCompleted)
                        {
                            return;
                        }
                    }
                }
            }
        }
        
        if (playerSlot.IsDestroyed || IsCharacterDead(currentPlayer))
        {
            if (!IsCharacterDead(currentEnemy) && !IsCharacterDead(effectiveEnemyTarget))
            {
                ResolveSingleAction(enemySlot, currentEnemy, effectiveEnemyTarget);
            }
            return;
        }
        
        if (enemySlot.IsDestroyed || IsCharacterDead(currentEnemy))
        {
            if (!IsCharacterDead(currentPlayer) && !IsCharacterDead(effectivePlayerTarget))
            {
                ResolveSingleAction(playerSlot, currentPlayer, effectivePlayerTarget);
            }
            return;
        }
        
        ResolveActionPair(playerSlot, enemySlot, currentPlayer, currentEnemy);
    }

    private bool CheckDeflection(ActionSlot slot1, ActionSlot slot2)
    {
        // 确保只有序号相同的行动槽才能触发偏转
        int playerSlotIndex = PlayerSlots.IndexOf(slot1);
        int enemySlotIndex = EnemySlots.IndexOf(slot2);
        if (playerSlotIndex == -1 || enemySlotIndex == -1 || playerSlotIndex != enemySlotIndex)
        {
            return false;
        }
        
        // 仅在一方使用闪避技能而另一方使用防御或闪避技能时才触发偏转
        bool slot1IsDodge = slot1.Type == ActionType.Dodge;
        bool slot2IsDodge = slot2.Type == ActionType.Dodge;
        bool slot1IsDefensive = IsDefensiveAction(slot1.Type);
        bool slot2IsDefensive = IsDefensiveAction(slot2.Type);
        
        return !_deflectionTriggeredThisRound && 
               slot1IsDefensive && 
               slot2IsDefensive &&
               (slot1IsDodge || slot2IsDodge); // 必须至少有一方使用闪避技能
    }

    private bool IsDefensiveAction(ActionType type)
    {
        return type == ActionType.Defend || type == ActionType.Dodge;
    }

    private void TriggerDeflection(ActionSlot slot1, ActionSlot slot2)
    {
        slot1.IsDestroyed = true;
        slot2.IsDestroyed = true;
        _deflectionTriggeredThisRound = true;
        // 找到slot1和slot2在各自行动槽列表中的索引
        int playerSlotIndex = PlayerSlots.IndexOf(slot1);
        int enemySlotIndex = EnemySlots.IndexOf(slot2);
        
        // 确定slot1和slot2的角色和技能类型
        string playerRole = "英雄";
        string enemyRole = "敌人";
        string playerSkillName = slot1.GetTypeName();
        string enemySkillName = slot2.GetTypeName();
        
        // 找到第x对行动槽
        int pairIndex = Math.Max(playerSlotIndex, enemySlotIndex) + 1;
        
        BattleLog.Add($"{playerRole}与{enemyRole}的第 {pairIndex} 对行动槽触发了偏转,双方的{playerSkillName}技能与{enemySkillName}技能失效");
    }

    private void ResolveActionPair(ActionSlot playerSlot, ActionSlot enemySlot, Character currentPlayer, Character currentEnemy)
    {
        if (playerSlot.Type == ActionType.Attack)
        {
            if (enemySlot.Type == ActionType.Attack)
            {
                ResolveAttackVsAttack(playerSlot, enemySlot, currentPlayer, currentEnemy);
            }
            else if (enemySlot.Type == ActionType.Dodge)
            {
                ResolveAttackVsDodge(playerSlot, enemySlot, currentEnemy);
            }
            else if (enemySlot.Type == ActionType.Heal)
            {
                ResolveAttackVsHeal(playerSlot, enemySlot, currentPlayer, currentEnemy);
            }
            else if (enemySlot.Type == ActionType.Defend)
            {
                // 攻击对抗防御：防御技能已经在回合开始时生成护盾，直接执行攻击技能单方面攻击
                ResolveSingleAction(playerSlot, currentPlayer, currentEnemy);
            }
        }
        else if (playerSlot.Type == ActionType.Dodge)
        {
            if (enemySlot.Type == ActionType.Attack)
            {
                ResolveAttackVsDodge(enemySlot, playerSlot, currentPlayer);
            }
            else if (enemySlot.Type == ActionType.Defend || enemySlot.Type == ActionType.Dodge)
            {
                // 检查偏转条件
                if (CheckDeflection(playerSlot, enemySlot))
                {
                    TriggerDeflection(playerSlot, enemySlot);
                }
            }
        }
        else if (playerSlot.Type == ActionType.Defend)
        {
            if (enemySlot.Type == ActionType.Attack)
            {
                // 防御对抗攻击：防御技能已经在回合开始时生成护盾，直接执行敌人的攻击技能单方面攻击
                ResolveSingleAction(enemySlot, currentEnemy, currentPlayer);
            }
            else if (enemySlot.Type == ActionType.Defend || enemySlot.Type == ActionType.Dodge)
            {
                // 防御对抗防御或闪避，总是触发偏转
                if (CheckDeflection(playerSlot, enemySlot))
                {
                    TriggerDeflection(playerSlot, enemySlot);
                }
                else
                {
                    // 如果本回合已经触发过偏转，至少标记敌人的闪避行动槽为已销毁
                    enemySlot.IsDestroyed = true;
                }
            }
        }
        else if (playerSlot.Type == ActionType.Heal)
        {
            ResolveSingleAction(playerSlot, currentPlayer, currentPlayer);
            
            if (enemySlot.Type == ActionType.Attack)
            {
                ResolveSingleAction(enemySlot, currentEnemy, currentPlayer);
            }
            else if (enemySlot.Type == ActionType.Heal)
            {
                ResolveSingleAction(enemySlot, currentEnemy, currentEnemy);
            }
        }
        else if (playerSlot.Type == ActionType.Counter)
        {
            // 反击技能vs反击/防御/闪避/治疗：无任何效果
            if (enemySlot.Type == ActionType.Counter || enemySlot.Type == ActionType.Defend || enemySlot.Type == ActionType.Dodge || enemySlot.Type == ActionType.Heal)
            {
                // 无任何效果，标记反击技能为已完成
                playerSlot.IsCompleted = true;
                return;
            }
            // 反击技能vs攻击技能：不进行拼点，先受到伤害，然后再进行反击
            else if (enemySlot.Type == ActionType.Attack)
            {
                // 保存当前的伤害值
                int originalShieldDamage = _totalShieldDamage;
                int originalHealthDamage = _totalHealthDamage;
                
                // 先处理敌人的攻击技能
                ResolveSingleAction(enemySlot, currentEnemy, currentPlayer);
                
                // 检查是否受到了伤害
                // 注意：由于ResolveSingleAction会设置状态，这里的检查可能不会立即执行
                // 但我们仍然需要标记反击技能的状态
                playerSlot.IsCompleted = false; // 不要立即标记为已完成，等待攻击处理完成后再决定
                
                // 记录反击技能的信息，以便在攻击处理完成后触发
                _counterSkillInfo = new CounterSkillInfo
                {
                    CounterSlot = playerSlot,
                    Attacker = currentPlayer,
                    Target = currentEnemy,
                    OriginalShieldDamage = originalShieldDamage,
                    OriginalHealthDamage = originalHealthDamage
                };
            }
        }
        else if (enemySlot.Type == ActionType.Counter)
        {
            // 反击技能vs反击/防御/闪避/治疗：无任何效果
            if (playerSlot.Type == ActionType.Counter || playerSlot.Type == ActionType.Defend || playerSlot.Type == ActionType.Dodge || playerSlot.Type == ActionType.Heal)
            {
                // 无任何效果，标记反击技能为已完成
                enemySlot.IsCompleted = true;
                return;
            }
            // 反击技能vs攻击技能：不进行拼点，先受到伤害，然后再进行反击
            else if (playerSlot.Type == ActionType.Attack)
            {
                // 保存当前的伤害值
                int originalShieldDamage = _totalShieldDamage;
                int originalHealthDamage = _totalHealthDamage;
                
                // 先处理玩家的攻击技能
                ResolveSingleAction(playerSlot, currentPlayer, currentEnemy);
                
                // 检查是否受到了伤害
                // 注意：由于ResolveSingleAction会设置状态，这里的检查可能不会立即执行
                // 但我们仍然需要标记反击技能的状态
                enemySlot.IsCompleted = false; // 不要立即标记为已完成，等待攻击处理完成后再决定
                
                // 记录反击技能的信息，以便在攻击处理完成后触发
                _counterSkillInfo = new CounterSkillInfo
                {
                    CounterSlot = enemySlot,
                    Attacker = currentEnemy,
                    Target = currentPlayer,
                    OriginalShieldDamage = originalShieldDamage,
                    OriginalHealthDamage = originalHealthDamage
                };
            }
        }
    }

    private void ResolveAttackVsAttack(ActionSlot playerSlot, ActionSlot enemySlot, Character currentPlayer, Character currentEnemy)
    {
        int playerWins = 0;
        int enemyWins = 0;
        int totalRounds = 0;
        int maxRounds = 100; // 防止无限循环
        
        while (playerSlot.HasRemainingCoins() && enemySlot.HasRemainingCoins() && totalRounds < maxRounds)
        {
            // 在拼点时投掷硬币
            playerSlot.FlipCoins(currentPlayer.Morale, currentPlayer, _buffHandler);
            enemySlot.FlipCoins(currentEnemy.Morale, currentEnemy, _buffHandler); // 敌方也使用士气
            
            int playerValue = playerSlot.BaseValue + playerSlot.GetCurrentCoinValue() + playerSlot.CompetingPower;
            int enemyValue = enemySlot.BaseValue + enemySlot.GetCurrentCoinValue() + enemySlot.CompetingPower;
            
            totalRounds++;
            
            if (playerValue > enemyValue)
            {
                playerWins++;
                enemySlot.RemoveLastCoin();
                // 移除不符合要求的BattleLog输出
            }
            else if (enemyValue > playerValue)
            {
                enemyWins++;
                playerSlot.RemoveLastCoin();
                // 移除不符合要求的BattleLog输出
            }
            else
            {
                // 移除不符合要求的BattleLog输出
            }
            
            // 短暂延迟，让玩家看到硬币状态
            // 移除Thread.Sleep，避免阻塞主线程
        }
        
        if (totalRounds >= maxRounds)
        {
        }
        

        
        if (playerWins > enemyWins)
            {
                // 保存攻击相关变量
                _currentAttackSlot = playerSlot;
                // 使用slot.TargetSlot获取目标，如果没有则使用默认的currentEnemy
                _currentTarget = playerSlot.TargetSlot != null ? _slotToCharacterMap[playerSlot.TargetSlot] : currentEnemy;
                _currentCoinIndex = 0;
                _originalCoins = playerSlot.Coins != null ? (int[])playerSlot.Coins.Clone() : new int[0];
                _rerolledCoins = new int[_originalCoins.Length]; // 初始化重投硬币数组
                _baseAttackValue = playerSlot.BaseValue; // 使用包含攻击等级修正的基础点数
                _coinValue = playerSlot.CoinValue;
                _attackLevel = currentPlayer.FinalAttackLevel;
                _totalShieldDamage = 0; // 初始化累积护盾伤害
                _totalHealthDamage = 0; // 初始化累积体力伤害
                _attackerName = currentPlayer.Name; // 设置攻击者名称
                _attackSkillName = playerSlot.GetSkillName(); // 获取攻击技能名称
                _attackEngagedInShowdown = true; // 进行拼点
                _targetDefenseLevel = currentEnemy.FinalDefenseLevel; // 设置目标的防御等级
                
                // 标记敌怪的攻击槽为已销毁
                enemySlot.IsDestroyed = true;
                
                // 设置状态为等待攻击
                _resolutionState = ResolutionState.WaitingForAttack;
                
                // 攻击拼点胜利，玩家提高士气，敌方降低士气
                int moraleGain = 2 + Math.Max(0, totalRounds - 1); // 至少2点，每多一次拼点+1
                currentPlayer.AdjustMorale(moraleGain);
                currentEnemy.AdjustMorale(-2); // 敌方固定扣除2点士气值
                // 移除不符合要求的BattleLog输出
            }
            else if (enemyWins > playerWins)
            {
                // 保存攻击相关变量
                _currentAttackSlot = enemySlot;
                // 使用slot.TargetSlot获取目标，如果没有则使用默认的currentPlayer
                _currentTarget = enemySlot.TargetSlot != null ? _slotToCharacterMap[enemySlot.TargetSlot] : currentPlayer;
                _currentCoinIndex = 0;
                _originalCoins = enemySlot.Coins != null ? (int[])enemySlot.Coins.Clone() : new int[0];
                _rerolledCoins = new int[_originalCoins.Length]; // 初始化重投硬币数组
                _baseAttackValue = enemySlot.BaseValue; // 使用包含攻击等级修正的基础点数
                _coinValue = enemySlot.CoinValue;
                _attackLevel = currentEnemy.FinalAttackLevel;
                _totalShieldDamage = 0; // 初始化累积护盾伤害
                _totalHealthDamage = 0; // 初始化累积体力伤害
                _attackerName = currentEnemy.Name; // 设置攻击者名称
                _attackSkillName = enemySlot.GetSkillName(); // 获取攻击技能名称
                _attackEngagedInShowdown = true; // 进行拼点
                _targetDefenseLevel = currentPlayer.FinalDefenseLevel; // 设置目标的防御等级
                
                // 标记玩家的攻击槽为已销毁
                playerSlot.IsDestroyed = true;
                
                // 设置状态为等待攻击
                _resolutionState = ResolutionState.WaitingForAttack;
                
                // 攻击拼点失败，玩家降低士气，敌方提高士气
                int moraleGain = 2 + Math.Max(0, totalRounds - 1); // 至少2点，每多一次拼点+1
                currentEnemy.AdjustMorale(moraleGain);
                currentPlayer.AdjustMorale(-2); // 固定扣除2点士气值
                // 移除不符合要求的BattleLog输出
            }
        else
        {
            // 平局情况，双方攻击槽都被销毁
            playerSlot.IsDestroyed = true;
            enemySlot.IsDestroyed = true;
            // 不设置任何攻击相关变量，保持状态为ResolvingStep
        }
    }

    private void ResolveAttackVsDodge(ActionSlot attackSlot, ActionSlot dodgeSlot, Character target)
    {
        // 获取攻击方和闪避方的角色
        Character attacker = null;
        Character dodger = target;
        
        // 根据行动槽类型确定攻击者
        if (attackSlot.Type == ActionType.Attack)
        {
            // 查找攻击行动槽对应的角色
            foreach (var entry in _slotToCharacterMap)
            {
                if (entry.Key == attackSlot)
                {
                    attacker = entry.Value;
                    break;
                }
            }
        }
        
        // 攻击技能每次投掷硬币时，闪避技能投掷全部硬币并比较双方最终点数
        if (attacker != null)
        {
            attackSlot.FlipCoins(attacker.Morale);
        }
        else
        {
            attackSlot.FlipCoins(0);
        }
        
        if (dodger != null)
        {
            dodgeSlot.FlipCoins(dodger.Morale);
        }
        else
        {
            dodgeSlot.FlipCoins(0);
        }
        
        int attackValue = attackSlot.BaseValue + attackSlot.GetCurrentCoinValue() + attackSlot.CompetingPower;
        int dodgeValue = dodgeSlot.BaseValue + dodgeSlot.GetCurrentCoinValue() + dodgeSlot.CompetingPower;
        

        
        if (attackValue >= dodgeValue)
        {
            // 攻击技能当前总点数大于等于闪避技能总点数，按当前攻击技能总点数造成伤害，并摧毁对方闪避技能
            // 保存攻击相关变量
            _currentAttackSlot = attackSlot;
            _currentTarget = target;
            _currentCoinIndex = 0;
            _originalCoins = attackSlot.Coins != null ? (int[])attackSlot.Coins.Clone() : new int[0];
            _rerolledCoins = new int[_originalCoins.Length]; // 初始化重投硬币数组
            _baseAttackValue = attackSlot.BaseValue; // 使用包含攻击等级修正的基础点数
            _coinValue = attackSlot.CoinValue;
            _attackLevel = attacker != null ? attacker.FinalAttackLevel : 1;
            _totalShieldDamage = 0; // 初始化累积护盾伤害
            _totalHealthDamage = 0; // 初始化累积体力伤害
            _attackerName = attacker?.Name ?? "攻击者"; // 设置攻击者名称
            _attackSkillName = attackSlot.GetSkillName(); // 获取攻击技能名称
            _attackEngagedInShowdown = true; // 进行拼点
            _targetDefenseLevel = target.FinalDefenseLevel; // 设置目标的防御等级
            
            // 标记闪避槽为已销毁
            dodgeSlot.IsDestroyed = true;
            
            // 设置状态为等待攻击
            _resolutionState = ResolutionState.WaitingForAttack;
            
            // 闪避失败，降低士气
            if (dodger != null)
            {
                dodger.AdjustMorale(-3);
            }
        }
        else
        {
            // 攻击技能当前总点数小于闪避技能总点数，不造成伤害
            // 不标记闪避槽为已销毁，让它继续与后续的攻击行动槽拼点
            BattleLog.Add($"{dodger?.Name ?? "目标"}成功闪避了攻击");
            
            // 闪避成功，提高士气
            if (dodger != null)
            {
                int moraleGain = 5; // 固定增加5点士气值
                dodger.AdjustMorale(moraleGain);
                // 移除士气变化的输出，只显示用户要求的内容
            }
            
            // 切换到闪避结果等待状态，增加1.5秒展示时间
            _resolutionState = ResolutionState.WaitingForDodgeResult;
        }
    }

    private void ResolveAttackVsHeal(ActionSlot attackSlot, ActionSlot healSlot, Character attacker, Character defender)
    {
        // 在拼点时投掷硬币
        attackSlot.FlipCoins(attacker.Morale);
        healSlot.FlipCoins(defender.Morale);
        
        int attackValue = attackSlot.BaseValue + attackSlot.GetCurrentCoinValue() + attackSlot.CompetingPower;
        int healValue = healSlot.BaseValue + healSlot.GetCurrentCoinValue() + healSlot.CompetingPower;
        
        // 保存攻击相关变量
        _currentAttackSlot = attackSlot;
        _currentTarget = defender;
        _currentCoinIndex = 0;
        _originalCoins = attackSlot.Coins != null ? (int[])attackSlot.Coins.Clone() : new int[0];
        _rerolledCoins = new int[_originalCoins.Length]; // 初始化重投硬币数组
        _baseAttackValue = attackSlot.BaseValue; // 使用包含攻击等级修正的基础点数
        _coinValue = attackSlot.CoinValue;
        _attackLevel = attacker.FinalAttackLevel;
        _totalShieldDamage = 0; // 初始化累积护盾伤害
        _totalHealthDamage = 0; // 初始化累积体力伤害
        _attackerName = attacker.Name; // 设置攻击者名称
        _attackSkillName = attackSlot.GetSkillName(); // 获取攻击技能名称
        _attackEngagedInShowdown = false; // 未进行拼点
        _targetDefenseLevel = defender.FinalDefenseLevel; // 设置目标的防御等级
        
        // 设置状态为等待攻击
        _resolutionState = ResolutionState.WaitingForAttack;
        
        defender.CurrentHealth += healValue;
        if (defender.CurrentHealth > defender.MaxHealth)
            defender.CurrentHealth = defender.MaxHealth;
        // 移除不符合要求的BattleLog输出
        
        // 记录治疗统计
        if (healSlot != null && _slotToCharacterMap.ContainsKey(healSlot))
        {
            Character healer = _slotToCharacterMap[healSlot];
            string skillName = healSlot.GetSkillName();
            Statistics.RecordHealing(healer, skillName, healValue);
        }
        
        healSlot.IsDestroyed = true;
    }

    private void ResolveSingleAction(ActionSlot slot, Character actor, Character target)
    {
        // 优先使用slot.TargetSlot获取目标
        Character effectiveTarget = target;
        if (slot.TargetSlot != null && _slotToCharacterMap.ContainsKey(slot.TargetSlot))
        {
            effectiveTarget = _slotToCharacterMap[slot.TargetSlot];
        }
        
        // 检查行动槽是否已被摧毁
        if (slot.IsDestroyed)
        {
            return;
        }
        
        // 处理技能使用前的效果
        if (actor is 夏侯惇)
        {
            // 直接使用slot.Skill对象，而不是创建新的skill对象
            if (slot.Skill != null && slot.Skill.GetType().Name == "横斩")
            {
                // 检查是否有沉默状态
                if (_buffHandler.CheckBuff<Silence>(actor))
                {
                    // 动态调用ApplySilenceEnhancement方法
                    var method = slot.Skill.GetType().GetMethod("ApplySilenceEnhancement");
                    if (method != null)
                    {
                        method.Invoke(slot.Skill, null);
                        // 同步属性到ActionSlot
                        slot.CompetingPower = slot.Skill.CompetingPower;
                        slot.CoinValue = slot.Skill.CoinValue;
                    }
                }
            }
            else if (slot.Skill != null && slot.Skill.GetType().Name == "铁壁战吼")
            {
                // 检查护盾值是否不低于最大生命的33%
                if (_characterShields.ContainsKey(actor) && _characterShields[actor] >= actor.MaxHealth * 0.33f)
                {
                    // 动态调用ApplyShieldEnhancement方法
                    var method = slot.Skill.GetType().GetMethod("ApplyShieldEnhancement");
                    if (method != null)
                    {
                        method.Invoke(slot.Skill, null);
                        // 同步属性到ActionSlot
                        slot.CompetingPower = slot.Skill.CompetingPower;
                        slot.BaseValue = slot.Skill.BaseValue;
                    }
                }
            }
        }
        else if (actor is TurnBasedRPG.Characters.Allies.曹仁)
        {
            BaseSkill skill = actor.GetSkillByActionType(slot.Type, slot.SelectedSkill);
            if (skill.GetType().Name == "御甲鸣镝")
            {
                // 检查护盾值是否不低于最大生命的33%
                if (_characterShields.ContainsKey(actor) && _characterShields[actor] >= actor.MaxHealth * 0.33f)
                {
                    // 动态调用ApplyShieldEnhancement方法
                    var method = skill.GetType().GetMethod("ApplyShieldEnhancement");
                    if (method != null)
                    {
                        method.Invoke(skill, null);
                        // 同步属性到ActionSlot
                        slot.CompetingPower = skill.CompetingPower;
                    }
                }
                
                // 随机选取主要目标外至多两个敌方单位作为次级目标
                _yujiaSecondaryTargets = new List<Character>();
                List<Character> enemies = new List<Character>();
                if (effectiveTarget.IsAlly)
                {
                    // 如果目标是己方，那么敌方是Enemies
                    enemies.AddRange(Enemies);
                }
                else
                {
                    // 如果目标是敌方，那么敌方是Players
                    enemies.AddRange(Players);
                }
                
                // 排除主要目标，只选择存活的敌方单位
                foreach (var enemy in enemies)
                {
                    if (enemy != effectiveTarget && !enemy.ShouldDie() && _yujiaSecondaryTargets.Count < 2)
                    {
                        _yujiaSecondaryTargets.Add(enemy);
                    }
                }
                
                // 设置HasNoSecondaryTargets标志
                _yujiaHasNoSecondaryTargets = _yujiaSecondaryTargets.Count == 0;
            }
            else if (skill.GetType().Name == "镇岳反攻")
            {
                // 检查护盾值是否高于25%最大生命
                if (_characterShields.ContainsKey(actor) && _characterShields[actor] > actor.MaxHealth * 0.25f)
                {
                    // 动态调用ApplyShieldEnhancement方法
                    var method = skill.GetType().GetMethod("ApplyShieldEnhancement");
                    if (method != null)
                    {
                        method.Invoke(skill, null);
                        // 同步属性到ActionSlot
                        slot.CompetingPower = skill.CompetingPower;
                    }
                }
            }
        }
        else if (actor is TurnBasedRPG.Characters.Allies.司马懿)
        {
            BaseSkill skill = actor.GetSkillByActionType(slot.Type, slot.SelectedSkill);
            if (skill.GetType().Name == "汲魂")
            {
                // [使用前]消耗全队魏国武将当前持有的护盾值的10%，对于当前护盾值不低于50%最大生命的武将提升消耗量至20%
                int totalShieldConsumed = 0;
                
                // 给同队所有魏国武将消耗护盾
                List<Character> allies = new List<Character>();
                if (actor.IsAlly)
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
                        int currentShield = GetCharacterShield(ally);
                        bool hasKey = _characterShields.ContainsKey(ally);
                        if (currentShield > 0)
                        {
                            // 确定消耗比例
                            float consumeRate = 0.1f; // 默认10%
                            if (currentShield >= ally.MaxHealth * 0.5f)
                            {
                                consumeRate = 0.2f; // 护盾不低于50%最大生命时，提升到20%
                            }
                            
                            int shieldToConsume = (int)(currentShield * consumeRate);
                            
                            // 关键修复：如果字典没有这个角色的键，但当前护盾>0，说明是GetCharacterShield返回的0是不对的，
                            // 但实际上角色应该有护盾。让我们先确保角色在字典中。
                            if (!_characterShields.ContainsKey(ally))
                            {
                                _characterShields[ally] = currentShield;
                            }
                            
                            if (shieldToConsume > 0)
                            {
                                _characterShields[ally] -= shieldToConsume;
                                totalShieldConsumed += shieldToConsume;
                                
                                // 触发曹仁的默守叠加强度机制
                                // 我们需要模拟一次护盾受到伤害的情况，让HandleCaoRenShieldHit被调用
                                // 创建一个临时的行动槽来模拟
                                ActionSlot tempSlot = new ActionSlot(0);
                                _slotToCharacterMap[tempSlot] = actor; // 攻击者是司马懿
                                
                                // 使用CaoRenSkill类处理
                                var caoRenSkill = new TurnBasedRPG.Systems.SkillManagement.CaoRenSkill(this);
                                // 找到所有曹仁
                                List<Character> allCaoRens = new List<Character>();
                                foreach (var entry in _slotToCharacterMap)
                                {
                                    if (entry.Value is TurnBasedRPG.Characters.Allies.曹仁)
                                    {
                                        allCaoRens.Add(entry.Value);
                                    }
                                }
                                // 处理每个曹仁
                                foreach (var caoRen in allCaoRens)
                                {
                                    caoRenSkill.HandleCaoRenShieldHit(caoRen, actor, _buffHandler, GetAllCharacters());
                                }
                                
                                // 移除临时行动槽
                                _slotToCharacterMap.Remove(tempSlot);
                            }
                        }
                    }
                }
                
                // 消耗的护盾总值每达到自身最大生命的5%，使此技能的拼点威力+1
                int maxHealth5Percent = (int)(actor.MaxHealth * 0.05f);
                if (maxHealth5Percent > 0)
                {
                    int bonusBaseValue = totalShieldConsumed / maxHealth5Percent;
                    if (bonusBaseValue > 0)
                    {
                        slot.CompetingPower += bonusBaseValue;
                    }
                }
                
                // 记录消耗的护盾总值，以便在命中时使用
                _jihunConsumedShieldTotal = totalShieldConsumed;
            }
        }
        else if (actor is TurnBasedRPG.Characters.Allies.张辽 zhangLiao)
        {
            // 处理霜戟和破溃的使用前效果
            if (slot.SkillName == "霜戟")
            {
                // [使用前]若自身的攻击等级不低于目标的防御等级，则使本技能的拼点威力提升4
                if (actor.FinalAttackLevel >= target.FinalDefenseLevel)
                {
                    slot.CompetingPower += 4;
                }
            }
            else if (slot.SkillName == "破溃")
            {
                // [使用前]若自身的攻击等级不低于目标的防御等级，则使本技能的拼点威力提升5
                if (actor.FinalAttackLevel >= effectiveTarget.FinalDefenseLevel)
                {
                    slot.CompetingPower += 5;
                }
            }
            
            // 处理神威状态的拼点威力加成
            if (zhangLiao.IsInShenWeiState())
            {
                int shenWeiBonus = 2 + (int)(actor.FinalAttackLevel / 20.0f);
                slot.CompetingPower += shenWeiBonus;
            }
        }
        
        // 在执行行动时投掷硬币
        slot.FlipCoins(actor.Morale);
        
        int actionValue = slot.BaseValue + slot.GetCurrentCoinValue();
        
        // 延迟1000ms后再完成血量变动
        // 移除Thread.Sleep，避免阻塞主线程
        
        switch (slot.Type)
        {
            case ActionType.Attack:
                // 检查目标是否有特殊战斗操作（如自动闪避）
                ActionSlot specialActionSlot = effectiveTarget.HandleSpecialBattleAction(slot, _dodgeTriggeredThisRound);
                if (specialActionSlot != null)
                {
                    _dodgeTriggeredThisRound = true;
                    
                    // 投掷硬币
                    specialActionSlot.FlipCoins(effectiveTarget.Morale);
                    
                    // 解析攻击 vs 特殊行动
                    ResolveAttackVsDodge(slot, specialActionSlot, effectiveTarget);
                    return;
                }
                
                // 保存攻击相关变量
                _currentAttackSlot = slot;
                _currentTarget = effectiveTarget;
                _currentCoinIndex = 0;
                _originalCoins = slot.Coins != null ? (int[])slot.Coins.Clone() : new int[0];
                _rerolledCoins = new int[_originalCoins.Length]; // 初始化重投硬币数组
                _baseAttackValue = slot.BaseValue; // 使用包含攻击等级修正的基础点数
                _coinValue = slot.CoinValue;
                _attackLevel = actor.FinalAttackLevel;
                _totalShieldDamage = 0; // 初始化累积护盾伤害
                _totalHealthDamage = 0; // 初始化累积体力伤害
                _attackerName = actor.Name; // 设置攻击者名称
                _attackSkillName = slot.GetSkillName(); // 获取攻击技能名称
                _attackEngagedInShowdown = false; // 未进行拼点
                _targetDefenseLevel = effectiveTarget.FinalDefenseLevel; // 设置目标的防御等级
                
                // 设置状态为等待攻击
                _resolutionState = ResolutionState.WaitingForAttack;
                // 不要立即标记为已销毁，在RethrowingCoins状态中完成伤害结算后再标记
                break;
            case ActionType.Heal:
                actor.CurrentHealth += actionValue;
                if (actor.CurrentHealth > actor.MaxHealth)
                    actor.CurrentHealth = actor.MaxHealth;
                // 找到行动槽在列表中的索引
                int healSlotIndex = PlayerSlots.IndexOf(slot) != -1 ? PlayerSlots.IndexOf(slot) : EnemySlots.IndexOf(slot);
                BattleLog.Add($"{actor.Name}的第 {healSlotIndex + 1} 治疗行动槽治疗 {actionValue} 点生命");
                
                // 记录治疗统计
                if (slot != null && _slotToCharacterMap.ContainsKey(slot))
                {
                    Character healer = _slotToCharacterMap[slot];
                    string skillName = slot.GetSkillName();
                    Statistics.RecordHealing(healer, skillName, actionValue);
                }
                
                slot.IsCompleted = true; // 标记为已完成，保留技能图标和核心信息
                
                // 切换到技能完成等待状态，增加2秒停留时间
                _currentAttackSlot = slot;
                _resolutionState = ResolutionState.WaitingForSkillComplete;
                break;
            case ActionType.Defend:
                // 防御技能已经在ProcessDefenseSlots方法中处理过，这里不再处理
                // 直接标记为已完成并切换到技能完成等待状态
                slot.IsCompleted = true; // 标记为已完成，保留技能图标和核心信息
                
                // 切换到技能完成等待状态，增加2秒停留时间
                _currentAttackSlot = slot;
                _resolutionState = ResolutionState.WaitingForSkillComplete;
                break;
            case ActionType.Counter:
                // 反击技能：当受到来自目标的攻击后，对该目标进行攻击
                // 处理方式类似于攻击技能
                // 保存攻击相关变量
                _currentAttackSlot = slot;
                _currentTarget = effectiveTarget;
                _currentCoinIndex = 0;
                _originalCoins = slot.Coins != null ? (int[])slot.Coins.Clone() : new int[0];
                _rerolledCoins = new int[_originalCoins.Length]; // 初始化重投硬币数组
                _baseAttackValue = slot.BaseValue; // 使用包含攻击等级修正的基础点数
                _coinValue = slot.CoinValue;
                _attackLevel = actor.FinalAttackLevel;
                _totalShieldDamage = 0; // 初始化累积护盾伤害
                _totalHealthDamage = 0; // 初始化累积体力伤害
                _attackerName = actor.Name; // 设置攻击者名称
                _attackSkillName = slot.GetSkillName(); // 获取攻击技能名称
                _attackEngagedInShowdown = false; // 未进行拼点
                _targetDefenseLevel = effectiveTarget.FinalDefenseLevel; // 设置目标的防御等级
                
                // 设置状态为等待攻击
                _resolutionState = ResolutionState.WaitingForAttack;
                // 不要立即标记为已销毁，在RethrowingCoins状态中完成伤害结算后再标记
                break;
        }
        
        // 移除这行代码，只在RethrowingCoins状态中标记攻击槽为已销毁
        // slot.IsDestroyed = true;
    }

    public void ApplyDamage(int damage, Character target, ActionSlot slot, bool isDirectDamage = true, bool isLastCoinHit = false)
    {
        ApplyDamage(damage, target, slot, null, isDirectDamage, isLastCoinHit);
    }

    public void ApplyDamage(int damage, Character target, ActionSlot slot, DamageCalculationResult? damageResult, bool isDirectDamage = true, bool isLastCoinHit = false)
    {
        string targetTeamInfo = target?.IsAlly == true ? "-我方" : "-敌方";
        
        // 获取攻击者信息
        Character attacker = null;
        string skillName = "未知技能";
        string attackerTeamInfo = "";
        if (slot != null && _slotToCharacterMap.ContainsKey(slot))
        {
            attacker = _slotToCharacterMap[slot];
            skillName = slot.GetSkillName();
            attackerTeamInfo = attacker?.IsAlly == true ? "-我方" : "-敌方";
        }
        
        // 检查目标是否有神威状态，如果有则免疫伤害
        if (_buffHandler.CheckBuff<神威>(target))
        {
            return;
        }
        
        // 获取结算前的护盾和血量
        int shieldBefore = GetCharacterShield(target);
        int healthBefore = target.CurrentHealth;
        
        // 如果目标是张辽且还未使用威震逍遥津，保存受伤前的生命值
        bool isZhangLiao = target is TurnBasedRPG.Characters.Allies.张辽;
        int healthBeforeDamage = 0;
        if (isZhangLiao)
        {
            healthBeforeDamage = target.CurrentHealth;
        }
        
        // 如果有damageResult，输出新格式的日志
        if (damageResult != null)
        {
            string critText = damageResult.IsCrit ? "是" : "否";
            string logMessage = $"[ApplyDamage]{attacker?.Name}{attackerTeamInfo}对{target?.Name}{targetTeamInfo}使用了{skillName}，最终点数：{damageResult.BaseValue} 攻防等级修正：{damageResult.SkillLevelMultiplier:F2} 一类增伤乘区：{damageResult.DamageMultiplier:F2} 最终增伤乘区：{damageResult.FinalDamageMultiplier:F2} 攻击方式易损：{damageResult.AttackTypeResistance:F2} 伤害类型易损：{damageResult.DamageTypeResistance:F2} 是否暴击：{critText} 暴击伤害乘区：{damageResult.CritDamageMultiplier:F2} 最终伤害：{damageResult.FinalDamage}（{damageResult.ShieldDamage}护盾，{damageResult.HealthDamage}血量）";
            Game1.Log(logMessage);
        }
        
        int shieldDamage = 0;
        int healthDamage = 0;
        
        // 使用角色特定的护盾值
        int currentShield = GetCharacterShield(target);
        if (currentShield > 0)
        {
            shieldDamage = Math.Min(currentShield, damage);
            int beforeShield = _characterShields[target];
            _characterShields[target] -= shieldDamage;
            string targetTeamInfoForLog = target?.IsAlly == true ? "-我方" : "-敌方";
            damage -= shieldDamage;
            
            // 触发护盾伤害事件
            if (shieldDamage > 0)
            {
                OnDamage?.Invoke(this, new DamageEventArgs
                {
                    Target = target,
                    DamageAmount = shieldDamage,
                    DamageType = HealthShieldDamageType.Shield
                });
            }
        }
        
        if (damage > 0)
        {
            healthDamage = damage;
            target.CurrentHealth -= healthDamage;
            if (target.CurrentHealth < 0)
                target.CurrentHealth = 0;
            
            // 触发血量伤害事件
            if (healthDamage > 0)
            {
                OnDamage?.Invoke(this, new DamageEventArgs
                {
                    Target = target,
                    DamageAmount = healthDamage,
                    DamageType = HealthShieldDamageType.Health
                });
            }
        }
        
        // 获取结算后的护盾和血量
        int shieldAfter = GetCharacterShield(target);
        int healthAfter = target.CurrentHealth;
        
        // 如果有damageResult，设置护盾伤害和血量伤害
        if (damageResult != null)
        {
            damageResult.ShieldDamage = shieldDamage;
            damageResult.HealthDamage = healthDamage;
        }
        
        // 处理刚烈buff的伤害反弹效果（只有在是直接伤害时才处理）
        if (target is 夏侯惇 && isDirectDamage)
        {
            // 检查目标是否有护盾
            int shieldValue = _characterShields.ContainsKey(target) ? _characterShields[target] : 0;
            if (shieldValue > 0 && _buffHandler.CheckBuff<Ganglie>(target))
                    {
                        // 反弹150%的伤害给攻击者
                        int totalDamageReflected = shieldDamage + healthDamage;
                        if (totalDamageReflected > 0)
                        {
                            if (attacker != null && attacker != target)
                            {
                                int reflectedDamage = (int)(totalDamageReflected * 1.5f);
                        // 反弹真实伤害，直接调用ApplyDamage并标记为间接伤害
                        ApplyDamage(reflectedDamage, attacker, slot, isDirectDamage: false, isLastCoinHit: isLastCoinHit);
                        BattleLog.Add($"夏侯惇反弹了{reflectedDamage}点伤害！");
                        
                        // 记录反弹伤害统计
                        if (target != null)
                        {
                            Statistics.RecordDamage(target, "刚烈", 0, reflectedDamage);
                        }
                    }
                }
            }
        }
        
        // 处理曹仁的默守状态：当魏国武将的护盾被命中时（只有在是直接伤害时才处理）
        if (shieldDamage > 0 && isDirectDamage)
        {
            string targetTeamInfoCaoRen = target?.IsAlly == true ? "-我方" : "-敌方";
            // 使用CaoRenSkill类处理
            var caoRenSkill = new TurnBasedRPG.Systems.SkillManagement.CaoRenSkill(this);
            // 找到所有曹仁
            List<Character> allCaoRens = new List<Character>();
            foreach (var entry in _slotToCharacterMap)
            {
                if (entry.Value is TurnBasedRPG.Characters.Allies.曹仁)
                {
                    allCaoRens.Add(entry.Value);
                }
            }
            // 处理每个曹仁
            foreach (var caoRen in allCaoRens)
            {
                caoRenSkill.HandleCaoRenShieldHit(caoRen, attacker, _buffHandler, GetAllCharacters());
            }
            
            // 处理曹丕的御极守成状态：当曹丕的护盾被命中时（只有在是直接伤害时才处理）
            List<Character> allCaoPis = new List<Character>();
            foreach (var entry in _slotToCharacterMap)
            {
                if (entry.Value is TurnBasedRPG.Characters.Allies.曹丕)
                {
                    allCaoPis.Add(entry.Value);
                }
            }
            // 处理每个曹丕
            foreach (var caoPi in allCaoPis)
            {
                ((TurnBasedRPG.Characters.Allies.曹丕)caoPi).OnShieldDamage(this);
            }
        }
        
        // 检查护盾是否被击破
        bool isShieldBroken = false;
        int previousShield = 0;
        if (_characterShields.ContainsKey(target))
        {
            previousShield = _characterShields[target] + shieldDamage;
            if (previousShield > 0 && _characterShields[target] <= 0)
            {
                isShieldBroken = true;
            }
        }
        
        // 处理仁心效果
        // 当同队魏国武将的护盾被击破或当前持有的护盾值不高于最大生命的7.5%时，触发曹操的仁心
        int currentShieldAfter = GetCharacterShield(target);
        float maxHealth75Percent = target.MaxHealth * 0.075f;
        bool shouldTriggerRenxin = (isShieldBroken || (currentShieldAfter <= maxHealth75Percent && shieldDamage > 0));
        
        if (shouldTriggerRenxin)
        {
            // 找到所有同队的曹操
            List<Character> allCaoCaos = new List<Character>();
            foreach (var entry in _slotToCharacterMap)
            {
                if (entry.Value is TurnBasedRPG.Characters.Allies.曹操 && entry.Value.IsAlly == target.IsAlly)
                {
                    allCaoCaos.Add(entry.Value);
                }
            }
            // 处理每个曹操
            foreach (var caoCao in allCaoCaos)
            {
                // 使用CaoCaoSkill类处理
                var caoCaoSkill = new TurnBasedRPG.Systems.SkillManagement.CaoCaoSkill(this);
                // 调用HandleRenxinEffect方法
                caoCaoSkill.HandleRenxinEffect(caoCao, target, _buffHandler, this);
            }
        }
        
        // 处理狼顾的触发逻辑
        // 使用SimaYiSkill类处理
        var simaYiSkill = new TurnBasedRPG.Systems.SkillManagement.SimaYiSkill(this);
        // 获取攻击者
        Character langguAttacker = null;
        if (_slotToCharacterMap != null && _slotToCharacterMap.ContainsKey(slot))
        {
            langguAttacker = _slotToCharacterMap[slot];
        }
        simaYiSkill.HandleLangguTrigger(target, slot, isDirectDamage, shieldDamage, healthDamage, isShieldBroken, isLastCoinHit, _buffHandler, GetAllCharacters(), langguAttacker);
        
        // 累积伤害，不在每次投掷时添加日志
        _totalShieldDamage += shieldDamage;
        _totalHealthDamage += healthDamage;
        
        // 记录伤害统计
        if (slot != null && _slotToCharacterMap.ContainsKey(slot))
        {
            Statistics.RecordDamage(attacker, skillName, shieldDamage, healthDamage);
        }
        
        // 检查张辽是否受到致死伤害，且还未使用威震逍遥津
        if (isZhangLiao && target.CurrentHealth <= 0 && !((TurnBasedRPG.Characters.Allies.张辽)target).HasUsedWeiZhenXiaoYaoJin())
        {
            // 1. 使张辽的血量回退至该次伤害前，判定为未死亡
            target.CurrentHealth = healthBeforeDamage;
            
            // 标记因致死伤害触发
            ((TurnBasedRPG.Characters.Allies.张辽)target).SetDiedFromLethalDamage(true);
            ((TurnBasedRPG.Characters.Allies.张辽)target).SetHealthBeforeLethalDamage(healthBeforeDamage);
            
            // 找到张辽的行动槽中装备威震逍遥津的那个
            ActionSlot zhangLiaoSlot = null;
            List<ActionSlot> slotsToCheck = target.IsAlly ? PlayerSlots : EnemySlots;
            foreach (var s in slotsToCheck)
            {
                if (_slotToCharacterMap.ContainsKey(s) && _slotToCharacterMap[s] == target && 
                    s.GetSkillName() == "威震逍遥津" && !s.IsDestroyed && !s.IsCompleted)
                {
                    zhangLiaoSlot = s;
                    break;
                }
            }
            
            if (zhangLiaoSlot != null)
            {
                // 立即使用威震逍遥津
                // 标记已使用
                ((TurnBasedRPG.Characters.Allies.张辽)target).SetHasUsedWeiZhenXiaoYaoJin(true);
                
                // 创建技能
                BaseSkill weiZhenSkill = target.GetSkillByActionType(ActionType.Attack, AttackSkill.Skill3);
                if (weiZhenSkill != null)
                {
                    // 1. 清除张辽持有的所有减益状态
                    var allBuffs = _buffHandler.GetBuffs(target);
                    var debuffsToRemove = allBuffs.Where(b => !b.IsBuff).ToList();
                    foreach (var buff in debuffsToRemove)
                    {
                        _buffHandler.RemoveBuff(target, buff);
                    }
                    
                    // 更新角色buff效果，计算最终攻击等级和防御等级
                    target.UpdateBuffs(_buffHandler);
                    
                    // 计算技能值
                    target.CalculateSkillValues(weiZhenSkill);
                    
                    // 2. 为张辽施加2回合[神威]
                    var shenWeiBuff = new 神威(2);
                    _buffHandler.AddBuff(target, shenWeiBuff);
                    
                    // 标记张辽处于神威状态
                    ((TurnBasedRPG.Characters.Allies.张辽)target).SetInShenWeiState(true);
                    
                    // 将张辽的所有行动槽替换为破溃
                    ReplaceAllZhangLiaoSkill3WithKuiPo((TurnBasedRPG.Characters.Allies.张辽)target, slotsToCheck);
                    
                    // 额外将其他技能也替换为破溃
                    foreach (var actionSlot in slotsToCheck)
                    {
                        if (_slotToCharacterMap.ContainsKey(actionSlot) && 
                            _slotToCharacterMap[actionSlot] == target && 
                            actionSlot.Type == ActionType.Attack && 
                            !actionSlot.IsDestroyed && 
                            !actionSlot.IsCompleted)
                        {
                            // 将行动槽替换为破溃
                            actionSlot.SkillName = "破溃";
                            actionSlot.SelectedSkill = AttackSkill.Skill2;
                            BaseSkill kuiPoSkill = target.GetSkillByActionType(ActionType.Attack, AttackSkill.Skill2);
                            if (kuiPoSkill != null)
                            {
                                target.CalculateSkillValues(kuiPoSkill);
                                actionSlot.SetAction(ActionType.Attack, kuiPoSkill);
                            }
                        }
                    }
                    
                    // 找到敌方角色列表
                    List<Character> enemies = target.IsAlly ? Enemies : Players;
                    
                    // 3. 对敌方全体造成两次伤害
                    // 第一段伤害：物理+穿刺，根据基础值进行完整的伤害计算
                    foreach (var enemy in enemies)
                    {
                        if (enemy.CurrentHealth > 0)
                        {
                            var damageResult1 = CalculateFullDamage(target, enemy, weiZhenSkill.BaseValue, zhangLiaoSlot, DamageType.Physical, AttackType.Pierce);
                            int damageVal = damageResult1.FinalDamage;
                            ApplyDamage(damageVal, enemy, zhangLiaoSlot, damageResult1, isDirectDamage: true);
                            BattleLog.Add($"威震逍遥津（第一段）对{enemy.Name}造成{damageVal}点物理伤害");
                        }
                    }
                    
                    // 第二段伤害：真实+穿刺，根据基础值进行完整的伤害计算
                    // 自身每损失1%生命，此伤害临时获得1%最终伤害提升
                    float healthLossPercent = 1.0f - ((float)target.CurrentHealth / target.MaxHealth);
                    float damageBonus = healthLossPercent * 1.0f;
                    target.FinalDamageIncrease += damageBonus;
                    
                    // 对敌方所有单位造成真实伤害
                    foreach (var enemy in enemies)
                    {
                        if (enemy.CurrentHealth > 0)
                        {
                            var damageResult2 = CalculateFullDamage(target, enemy, weiZhenSkill.BaseValue, zhangLiaoSlot, DamageType.True, AttackType.Pierce);
                            int damageVal = damageResult2.FinalDamage;
                            ApplyDamage(damageVal, enemy, zhangLiaoSlot, damageResult2, isDirectDamage: true);
                            BattleLog.Add($"威震逍遥津（第二段）对{enemy.Name}造成{damageVal}点真实伤害");
                        }
                    }
                    
                    // 恢复最终伤害加成
                    target.FinalDamageIncrease -= damageBonus;
                    
                    // 将所有张辽的技能3替换为破溃
                    ReplaceAllZhangLiaoSkill3WithKuiPo((TurnBasedRPG.Characters.Allies.张辽)target, slotsToCheck);
                }
            }
        }
    }
    
    public int GetCharacterShield(Character character)
    {
        if (_characterShields.ContainsKey(character))
        {
            int shield = _characterShields[character];
            return shield;
        }
        return 0;
    }
    
    public List<Character> GetAllCharacters()
    {
        List<Character> allCharacters = new List<Character>();
        allCharacters.AddRange(Players);
        allCharacters.AddRange(Enemies);
        return allCharacters;
    }
    


    public void TriggerCaoPiCounter(Character caoPi, Character target, bool isWeiWuHongLiu = false)
    {
        string skillName = isWeiWuHongLiu ? "魏武洪流" : "制衡";
        string teamInfo = caoPi.IsAlly ? "-我方" : "-敌方";
        string targetTeamInfo = target.IsAlly ? "-我方" : "-敌方";
        
        // 使用CaoPiSkill类处理
        var caoPiSkill = new TurnBasedRPG.Systems.SkillManagement.CaoPiSkill(this);
        caoPiSkill.TriggerCaoPiCounter(caoPi, target, isWeiWuHongLiu, _buffHandler, GetAllCharacters(), this);
    }



    private void CheckBattleEnd()
    {
        // 检查是否所有玩家角色都已死亡
        bool allPlayersDead = true;
        foreach (var player in Players)
        {
            if (!player.ShouldDie())
            {
                allPlayersDead = false;
                break;
            }
        }
        
        if (allPlayersDead)
        {
            // 输出当前回合的贡献统计
            Statistics.OutputStatistics();
            
            BattleEnded = true;
            CurrentPhase = BattlePhase.BattleEnd;
            BattleMessage = "所有我方角色都倒下了... 游戏结束！";
            return;
        }
        
        // 检查是否所有敌人角色都已死亡
        bool allEnemiesDead = true;
        foreach (var enemy in Enemies)
        {
            if (!enemy.ShouldDie())
            {
                allEnemiesDead = false;
                break;
            }
        }
        
        if (allEnemiesDead)
        {
            // 输出当前回合的贡献统计
            Statistics.OutputStatistics();
            
            BattleEnded = true;
            CurrentPhase = BattlePhase.BattleEnd;
            BattleMessage = "所有敌人都被击败了！我方获胜！";
        }
    }

    private void ResetBattleForNextRound()
    {
        // 回合切换时清空战斗日志
        BattleLog.Clear();
        
        // 创建所有角色列表
        List<Character> allCharacters = new List<Character>();
        allCharacters.AddRange(Players);
        allCharacters.AddRange(Enemies);
        
        // 处理回合结束事件
        foreach (var player in Players)
        {
            if (player is 夏侯惇 || player.GetType().Name == "夏侯惇2" || 
                player is TurnBasedRPG.Characters.Allies.曹仁 || player.GetType().Name == "曹仁2" || 
                player is TurnBasedRPG.Characters.Allies.司马懿 || player is TurnBasedRPG.Characters.Allies.张辽 || player is TurnBasedRPG.Characters.Allies.曹丕 || player is TurnBasedRPG.Characters.Allies.曹操)
            {
                // 使用反射调用OnTurnEnd方法
                var method = player.GetType().GetMethod("OnTurnEnd");
                if (method != null)
                {
                    var parameters = method.GetParameters();
                    if (parameters.Length == 2)
                    {
                        method.Invoke(player, new object[] { _buffHandler, allCharacters });
                    }
                    else
                    {
                        method.Invoke(player, new object[] { _buffHandler });
                    }
                }
            }
        }
        
        foreach (var enemy in Enemies)
        {
            if (enemy is 夏侯惇 || enemy is TurnBasedRPG.Characters.Allies.曹仁 || 
                enemy is TurnBasedRPG.Characters.Allies.司马懿 || enemy is TurnBasedRPG.Characters.Allies.张辽 || enemy is TurnBasedRPG.Characters.Allies.曹丕 || enemy is TurnBasedRPG.Characters.Allies.曹操)
            {
                // 使用反射调用OnTurnEnd方法
                var method = enemy.GetType().GetMethod("OnTurnEnd");
                if (method != null)
                {
                    var parameters = method.GetParameters();
                    if (parameters.Length == 2)
                    {
                        method.Invoke(enemy, new object[] { _buffHandler, allCharacters });
                    }
                    else
                    {
                        method.Invoke(enemy, new object[] { _buffHandler });
                    }
                }
            }
        }
        
        // 处理角色的回合开始事件（allCharacters已经在上面定义过了）
        
        // 在处理角色的回合开始效果之前，先清空护盾
        _characterShields.Clear();
        
        // 每回合开始时，随机设置速度并排序角色
        RandomizeAndSortCharactersBySpeed();
        
        foreach (var player in Players)
        {
            // 减少buff回合数
            _buffHandler.DecrementTurns(player);
            // 重置角色属性
            player.ResetAttributes();
            // 处理夏侯惇/曹仁/司马懿/曹丕/张辽/曹操的特殊效果
            if (player is 夏侯惇 || player is TurnBasedRPG.Characters.Allies.曹仁 || 
                player is TurnBasedRPG.Characters.Allies.司马懿 || player is TurnBasedRPG.Characters.Allies.曹丕 || player is TurnBasedRPG.Characters.Allies.张辽 || player is TurnBasedRPG.Characters.Allies.曹操)
            {
                // 使用反射调用OnTurnStart方法
                var method = player.GetType().GetMethod("OnTurnStart");
                if (method != null)
                {
                    // 所有角色都需要3个参数
                    method.Invoke(player, new object[] { _buffHandler, allCharacters, this });
                }
            }
            
            // 先更新所有buff的效果（包括护盾修正）
            player.UpdateBuffs(_buffHandler);
        }
        
        foreach (var enemy in Enemies)
        {
            // 减少buff回合数
            _buffHandler.DecrementTurns(enemy);
            // 重置角色属性
            enemy.ResetAttributes();
            // 处理夏侯惇/曹仁/司马懿/曹丕/张辽/曹操的特殊效果
            if (enemy is 夏侯惇 || enemy is TurnBasedRPG.Characters.Allies.曹仁 || 
                enemy is TurnBasedRPG.Characters.Allies.司马懿 || enemy is TurnBasedRPG.Characters.Allies.曹丕 || enemy is TurnBasedRPG.Characters.Allies.张辽 || enemy is TurnBasedRPG.Characters.Allies.曹操)
            {
                // 使用反射调用OnTurnStart方法
                var method = enemy.GetType().GetMethod("OnTurnStart");
                if (method != null)
                {
                    // 所有角色都需要3个参数
                    method.Invoke(enemy, new object[] { _buffHandler, allCharacters, this });
                }
            }
            
            // 先更新所有buff的效果（包括护盾修正）
            enemy.UpdateBuffs(_buffHandler);
        }
        
        foreach (var slot in PlayerSlots)
        {
            slot.Reset();
        }
        foreach (var slot in EnemySlots)
        {
            slot.Reset();
        }
        
        // 重新分配行动槽给角色，并设置速度值和技能名称
        AssignSlotsToCharacters(initialize: false);
        
        // 输出统计信息
        Statistics.OutputStatistics();
        
        // 重置回合相关变量
        _deflectionTriggeredThisRound = false;
        _dodgeTriggeredThisRound = false;
        _resolutionStep = 0;
        _resolutionTimer = 0;
        BattleLog.Clear();
        
        // 进入下回合，重置本回合统计数据
        Statistics.NextRound();
        
        // 韬晦强度处理将在所有行动槽被选取后进行
        
        // 敌怪先选技能
        CurrentPhase = BattlePhase.EnemySelection;
        BattleMessage = "敌人正在选择技能...";
        SelectEnemyActions();
    }

    public void UndoLastPlayerAction()
    {
        if (CurrentPhase != BattlePhase.PlayerSelection || CurrentPlayerSlot <= 0)
            return;
        
        CurrentPlayerSlot--;
        ActionSlot slot = PlayerSlots[CurrentPlayerSlot];
        slot.Type = ActionType.None;
        slot.BaseValue = 0;
        slot.CoinValue = 0;
        slot.CoinCount = 0;
        slot.Coins = new int[0];
        slot.CurrentCoinIndex = 0;
        
        // 恢复技能序列
        slot.ResetSkillSequence();
        
        // 重新为该行动槽设置随机抽取的攻击技能名称
        // 首先获取该行动槽所属的角色
        Character character = null;
        if (_slotToCharacterMap.ContainsKey(slot))
        {
            character = _slotToCharacterMap[slot];
        }
        
        // 如果找到了角色，根据SelectedSkill设置SkillName
        if (character != null && slot.SelectedSkill.HasValue)
        {
            BaseSkill skill = character.GetSkillByActionType(ActionType.Attack, slot.SelectedSkill);
            if (skill != null)
            {
                slot.SkillName = skill.Name;
            }
        }
        
        BattleMessage = $"为行动槽 {CurrentPlayerSlot + 1} 选择行动";
    }

    public void Restart()
    {
        InitializeBattle(Players, Enemies);
    }
    
    public void AddShield(Character character, int amount, string effectName = "", Character? attacker = null)
    {
        string teamInfo = character.IsAlly ? "-我方" : "-敌方";
        
        if (!_characterShields.ContainsKey(character))
        {
            _characterShields[character] = 0;
        }
        
        // 应用护盾修正（包括围城状态的影响）
        float shieldMultiplier = 1 + character.ShieldAdjustment;
        int adjustedAmount = (int)(amount * shieldMultiplier);
        
        int beforeShield = _characterShields[character];
        _characterShields[character] += adjustedAmount;
        
        // 输出新格式的AddShield日志
        string attackerTeamInfo = attacker?.IsAlly == true ? "-我方" : "-敌方";
        Game1.Log($"[AddShield]{character.Name}{teamInfo} 获得护盾，护盾基础值{amount}，护盾效果乘区{shieldMultiplier:F2}，实际获得护盾{adjustedAmount}");
        
        BattleLog.Add($"{character.Name}获得{adjustedAmount}点护盾");
        
        // 记录护盾统计
        string skillName = string.IsNullOrEmpty(effectName) ? "护盾" : effectName;
        Statistics.RecordShield(character, skillName, adjustedAmount);
    }
    
    // 批量添加护盾的方法，用于同时为多个目标添加护盾
    public void AddShieldToMultiple(Character attacker, List<Character> targets, int amount, string effectName = "")
    {
        if (targets == null || targets.Count == 0)
            return;
        
        // 输出施加护盾的日志
        string attackerTeamInfo = attacker.IsAlly ? "-我方" : "-敌方";
        List<string> targetNames = new List<string>();
        foreach (var target in targets)
        {
            string targetTeamInfo = target.IsAlly ? "-我方" : "-敌方";
            targetNames.Add($"{target.Name}{targetTeamInfo}");
        }
        string targetNamesStr = string.Join("/", targetNames);
        Game1.Log($"[AddShield]{attacker.Name}{attackerTeamInfo} 为 {targetNamesStr} 施加护盾");
        
        // 逐个为目标添加护盾
        foreach (var target in targets)
        {
            AddShield(target, amount, effectName, attacker);
        }
    }
    
    // 获取角色的所有buff
    public List<BaseBuff> GetBuffs(Character character)
    {
        return _buffHandler.GetBuffs(character);
    }
    
    // 为角色添加buff
    public void AddBuff(Character character, BaseBuff buff)
    {
        _buffHandler.AddBuff(character, buff);
    }
    
    // 生命移除方法：直接扣除目标的生命值或护盾值，不经过伤害计算的各种乘区
    public void HealthRemoval(int damage, Character target)
    {
        string targetTeamInfo = target?.IsAlly == true ? "-我方" : "-敌方";
        
        // 检查目标是否有神威状态，如果有则免疫伤害
        if (_buffHandler.CheckBuff<神威>(target))
        {
            return;
        }
        
        // 获取结算前的护盾和血量
        int shieldBefore = GetCharacterShield(target);
        int healthBefore = target.CurrentHealth;
        
        int shieldDamage = 0;
        int healthDamage = 0;
        
        // 先扣除护盾
        int currentShield = GetCharacterShield(target);
        if (currentShield > 0)
        {
            shieldDamage = Math.Min(currentShield, damage);
            int beforeShield = _characterShields[target];
            _characterShields[target] -= shieldDamage;
            string targetTeamInfoForLog = target?.IsAlly == true ? "-我方" : "-敌方";
            damage -= shieldDamage;
        }
        
        // 再扣除生命值
        if (damage > 0)
        {
            healthDamage = damage;
            target.CurrentHealth -= healthDamage;
            if (target.CurrentHealth < 0)
                target.CurrentHealth = 0;
        }
        
        // 获取结算后的护盾和血量
        int shieldAfter = GetCharacterShield(target);
        int healthAfter = target.CurrentHealth;
        
        // 触发伤害事件
        if (shieldDamage > 0)
        {
            OnDamage?.Invoke(this, new DamageEventArgs
            {
                Target = target,
                DamageAmount = shieldDamage,
                DamageType = HealthShieldDamageType.Shield
            });
        }
        
        if (healthDamage > 0)
        {
            OnDamage?.Invoke(this, new DamageEventArgs
            {
                Target = target,
                DamageAmount = healthDamage,
                DamageType = HealthShieldDamageType.Health
            });
        }
    }
    
    public void UpdateResolution(double deltaTime)
    {
        if (CurrentPhase == BattlePhase.Resolution)
        {
            try
            {
                switch (_resolutionState)
                {
                    case ResolutionState.WaitingForStep:
                        if (_resolutionStep < _executionOrder.Count)
                        {
                            _resolutionTimer += deltaTime;
                            if (_resolutionTimer >= STEP_DELAY)
                            {
                                // 使用新的执行顺序
                                ActionSlot currentSlot = _executionOrder[_resolutionStep];
                                
                                // 检查该行动槽是否已经被处理过（比如在配对处理中）
                                if (currentSlot.IsDestroyed || currentSlot.IsCompleted)
                                {
                                    _resolutionStep++;
                                    _resolutionTimer = 0;
                                    break;
                                }
                                
                                // 获取行动槽对应的角色
                                Character slotCharacter = _slotToCharacterMap[currentSlot];
                                
                                // 检查目标是否有效
                                Character targetCharacter = null;
                                if (currentSlot.TargetSlot != null && _slotToCharacterMap.ContainsKey(currentSlot.TargetSlot))
                                {
                                    targetCharacter = _slotToCharacterMap[currentSlot.TargetSlot];
                                }
                                
                                // 检查角色是否死亡
                                if (IsCharacterDead(slotCharacter))
                                {
                                    currentSlot.IsDestroyed = true;
                                    currentSlot.IsCompleted = true;
                                    _resolutionStep++;
                                    _resolutionTimer = 0;
                                    break;
                                }
                                
                                // 检查是否有配对关系
                                bool hasPair = false;
                                if (currentSlot.TargetSlot != null && !currentSlot.IsUnilateralAttack && 
                                    currentSlot.TargetSlot.TargetSlot == currentSlot && 
                                    !currentSlot.TargetSlot.IsDestroyed && !currentSlot.TargetSlot.IsCompleted)
                                {
                                    // 有配对关系，一起处理
                                    ActionSlot pairSlot = currentSlot.TargetSlot;
                                    Character pairCharacter = _slotToCharacterMap[pairSlot];
                                    
                                    // 确定哪个是玩家哪个是敌方
                                    ActionSlot playerSlot = currentSlot.IsAlly ? currentSlot : pairSlot;
                                    ActionSlot enemySlot = currentSlot.IsAlly ? pairSlot : currentSlot;
                                    
                                    // 获取配对在列表中的索引
                                    int pairIndex = _executionOrder.IndexOf(pairSlot);
                                    if (pairIndex > _resolutionStep)
                                    {
                                        // 配对的行动槽还没轮到，先跳过当前，等轮到配对时一起处理
                                        _resolutionStep++;
                                        _resolutionTimer = 0;
                                        break;
                                    }
                                    
                                    // 使用新的非对位配对处理方法
                                    ResolveArbitrarySlotPair(currentSlot, pairSlot);
                                    
                                    hasPair = true;
                                }
                                
                                if (!hasPair)
                                {
                                    // 没有配对关系，单独处理
                                    
                                    // 确定目标
                                    Character effectiveTarget = targetCharacter;
                                    if (effectiveTarget == null || IsCharacterDead(effectiveTarget))
                                    {
                                        // 目标无效，寻找新目标
                                        if (currentSlot.Type == ActionType.Attack)
                                        {
                                            List<Character> aliveTargets = currentSlot.IsAlly 
                                                ? Enemies.Where(e => !e.ShouldDie()).ToList()
                                                : Players.Where(p => !p.ShouldDie()).ToList();
                                            if (aliveTargets.Count > 0)
                                            {
                                                Random random = new Random();
                                                effectiveTarget = aliveTargets[random.Next(aliveTargets.Count)];
                                            }
                                        }
                                        else if (currentSlot.Type == ActionType.Heal)
                                        {
                                            effectiveTarget = slotCharacter;
                                        }
                                    }
                                    
                                    if (effectiveTarget != null)
                                    {
                                        ResolveSingleAction(currentSlot, slotCharacter, effectiveTarget);
                                    }
                                    else
                                    {
                                        currentSlot.IsCompleted = true;
                                    }
                                }
                                
                                _resolutionTimer = 0;
                                
                                if (CheckDeathCondition())
                                {
                                    ForceBattleEnd();
                                    return;
                                }
                                
                                // 只有在没有设置其他状态时才增加步骤
                                if (_resolutionState == ResolutionState.WaitingForStep)
                                {
                                    _resolutionStep++;
                                }
                            }
                        }
                        else
                        {
                            _roundTimer += deltaTime;
                            if (_roundTimer >= ROUND_DELAY)
                            {
                                _roundTimer = 0;
                                CheckBattleEnd();
                                if (!BattleEnded)
                                {
                                    ResetBattleForNextRound();
                                }
                                else
                                {
                                    BattleMessage = BattleEnded ? "战斗结束！" : "回合结束！";
                                }
                            }
                        }
                        break;
                    case ResolutionState.ResolvingStep:
                        // 行动槽解析完成，检查是否需要处理攻击
                        // 如果所有硬币都已处理，或者没有攻击需要处理，切换到WaitingForStep状态
                        _resolutionState = ResolutionState.WaitingForStep;
                        break;
                    case ResolutionState.WaitingForAttack:
                        _attackTimer += deltaTime;
                        if (_attackTimer >= ATTACK_DELAY)
                        {
                            _attackTimer = 0;
                            // 执行攻击逻辑
                            _resolutionState = ResolutionState.RethrowingCoins;
                        }
                        break;
                    case ResolutionState.RethrowingCoins:
                        // 重投硬币逻辑
                        // 首先检查是否所有硬币都已投掷完成
                        if (_currentAttackSlot != null && _currentTarget != null && _originalCoins != null && _rerolledCoins != null && _currentCoinIndex >= _originalCoins.Length)
                        {
                            // 所有硬币都已投掷，直接添加攻击日志并标记攻击槽为已销毁
                            
                            // 处理镇岳反攻技能攻击后的效果（只在所有硬币处理完毕后触发一次）
                            if (_attackSkillName == "镇岳反攻")
                            {
                                // 找到攻击者
                                Character attacker = null;
                                foreach (var entry in _slotToCharacterMap)
                                {
                                    if (entry.Key == _currentAttackSlot)
                                    {
                                        attacker = entry.Value;
                                        break;
                                    }
                                }
                                
                                if (attacker != null)
                                {
                                    int attackerShield = GetCharacterShield(attacker);
                                    
                                    // 计算额外伤害：自身剩余护盾值15%的真实伤害
                                    float extraDamageMultiplier = 0.15f;
                                    
                                    // 检查目标是否有护盾
                                    int targetShield = GetCharacterShield(_currentTarget);
                                    if (targetShield <= 0)
                                    {
                                        extraDamageMultiplier = 0.20f; // 若目标未持有护盾，则真实伤害倍率提升至20%
                                    }
                                    
                                    int extraDamage = (int)(attackerShield * extraDamageMultiplier);
                                    
                                    if (extraDamage > 0)
                                    {
                                        // 使用ApplyDamage方法应用伤害，先扣除护盾再扣除血量
                                        ApplyDamage(extraDamage, _currentTarget, _currentAttackSlot, isDirectDamage: true);
                                        BattleLog.Add($"镇岳反攻对{_currentTarget.Name}造成{extraDamage}点真实伤害");
                                    }
                                }
                            }
                            
                            // 处理曹操技能攻击后的效果
                            if (_attackSkillName == "煮酒论英" || _attackSkillName == "青釭开天" || _attackSkillName == "屯田固本" || _attackSkillName == "天下归心")
                            {
                                // 找到攻击者
                                Character attacker = null;
                                foreach (var entry in _slotToCharacterMap)
                                {
                                    if (entry.Key == _currentAttackSlot)
                                    {
                                        attacker = entry.Value;
                                        break;
                                    }
                                }
                                
                                if (attacker != null && attacker is TurnBasedRPG.Characters.Allies.曹操)
                                {
                                    var caoCaoSkill = new TurnBasedRPG.Systems.SkillManagement.CaoCaoSkill(this);
                                    int totalDamage = _totalShieldDamage + _totalHealthDamage;
                                    
                                    if (_attackSkillName == "煮酒论英")
                                    {
                                        caoCaoSkill.HandleZhujiulunyingPostAttack(attacker, _currentTarget, totalDamage, _buffHandler);
                                    }
                                    else if (_attackSkillName == "青釭开天")
                                    {
                                        caoCaoSkill.ProcessQinggangPostAttackEffects(attacker, new List<Character> { _currentTarget }, _buffHandler, GetAllCharacters(), this);
                                    }
                                    else if (_attackSkillName == "屯田固本")
                                    {
                                        caoCaoSkill.HandleTuntianGubenPostAttack(attacker, _currentTarget, totalDamage, _buffHandler);
                                    }
                                    else if (_attackSkillName == "天下归心")
                                    {
                                        // 天下归心需要消耗值，但暂时用总伤害代替
                                        caoCaoSkill.HandleTianxiaguixinOnHit(attacker, totalDamage, _buffHandler);
                                    }
                                }
                            }
                            
                            // 构建日志消息
                            string logMessage = $"{_attackerName}的{_attackSkillName}";
                            if (_attackEngagedInShowdown)
                            {
                                logMessage += "拼点胜利";
                            }
                            
                            // 无论伤害是否为0，都添加伤害统计信息
                            if (_totalShieldDamage > 0 && _totalHealthDamage > 0)
                            {
                                logMessage += $",共造成{_totalShieldDamage}点护盾伤害,{_totalHealthDamage}点体力伤害";
                            }
                            else if (_totalShieldDamage > 0)
                            {
                                logMessage += $",共造成{_totalShieldDamage}点护盾伤害";
                            }
                            else if (_totalHealthDamage > 0)
                            {
                                logMessage += $",共造成{_totalHealthDamage}点体力伤害";
                            }
                            else
                            {
                                // 即使伤害为0，也显示造成0点伤害
                                logMessage += $",共造成0点伤害";
                            }
                            
                            BattleLog.Add(logMessage);
                            
                            // 标记攻击槽为已完成（而非销毁），保留技能图标和核心信息
                            if (_currentAttackSlot != null)
                            {
                                _currentAttackSlot.IsCompleted = true;
                                
                                // 处理夏侯惇的特殊技能效果
                                Character attacker = null;
                                foreach (var entry in _slotToCharacterMap)
                                {
                                    if (entry.Key == _currentAttackSlot)
                                    {
                                        attacker = entry.Value;
                                        break;
                                    }
                                }
                                
                                if (attacker is 夏侯惇)
                                {
                                    // 处理攻击技能3的特殊效果：攻击结束后对自身施加1回合沉默
                                    if (_currentAttackSlot.GetSkillName() == "铁壁战吼")
                                    {
                                        ((夏侯惇)attacker).HandleAttackSkill3SilenceEffect(_buffHandler);
                                    }
                                }
                                

                            }
                            // 切换到技能完成等待状态，增加2秒停留时间
                            _resolutionState = ResolutionState.WaitingForSkillComplete;
                        }
                        else
                        {
                            // 还有硬币需要投掷，继续重投逻辑
                            _coinTimer += deltaTime;
                            
                            // 检查是否需要等待
                            if (_currentCoinIndex > 0)
                            {
                                double delay = (_currentCoinIndex == _originalCoins?.Length - 1) ? FINAL_COIN_DELAY : COIN_RETHROW_DELAY;
                                if (_coinTimer < delay)
                                    break;
                            }
                            
                            // 重置硬币计时器
                            _coinTimer = 0;
                            
                            if (_currentAttackSlot != null && _currentTarget != null && _originalCoins != null && _rerolledCoins != null && _currentCoinIndex < _originalCoins.Length)
                            {
                                // 找到攻击者角色
                                Character attacker = null;
                                foreach (var entry in _slotToCharacterMap)
                                {
                                    if (entry.Key == _currentAttackSlot)
                                    {
                                        attacker = entry.Value;
                                        break;
                                    }
                                }
                                
                                // 检查是否有神威状态
                                bool hasShenWei = false;
                                if (attacker != null)
                                {
                                    hasShenWei = _buffHandler.CheckBuff<TurnBasedRPG.Buffs.Buff.神威>(attacker);
                                }
                                
                                // 重投当前硬币
                                int coinResult;
                                if (hasShenWei)
                                {
                                    // 有神威状态，硬币必定为正面
                                    coinResult = 1;
                                }
                                else
                                {
                                    Random random = new Random();
                                    coinResult = random.Next(2) == 0 ? -1 : 1; // -1 为反面，1 为正面
                                }
                                
                                // 保存当前重投的硬币结果
                                _rerolledCoins[_currentCoinIndex] = coinResult;
                                
                                // 更新硬币数组
                                int[] newCoins = new int[_currentCoinIndex + 1];
                                for (int i = 0; i <= _currentCoinIndex; i++)
                                {
                                    newCoins[i] = _rerolledCoins[i];
                                }
                                _currentAttackSlot.Coins = newCoins;
                                
                                // 如果是盾击技能，添加硬币投掷日志
                                if (_attackSkillName == "盾击")
                                {
                                    string coinResultStr = coinResult == 1 ? "正面" : "反面";
                                }
                                
                                // 计算当前总的硬币点数（仅使用1级角色情况下的技能点数，忽略升级导致的攻击等级修正）
                                int headsCount = 0;
                                for (int i = 0; i < newCoins.Length; i++)
                                {
                                    if (newCoins[i] == 1)
                                        headsCount++;
                                }
                                int finalValue = _baseAttackValue + (headsCount * _coinValue);
                                
                                // 确定技能采用的攻防等级
                                int skillLevel = _attackLevel;
                                // 检查是否受到魏武固阵状态影响，使用防御等级进行计算
                                if (_currentAttackSlot != null)
                                {
                                    // 获取攻击者
                                    Character guzhenAttacker = null;
                                    foreach (var entry in _slotToCharacterMap)
                                    {
                                        if (entry.Key == _currentAttackSlot)
                                        {
                                            guzhenAttacker = entry.Value;
                                            break;
                                        }
                                    }
                                    
                                    if (guzhenAttacker != null)
                                    {
                                        // 检查是否有魏武固阵状态
                                        var buffs = _buffHandler.GetBuffs(guzhenAttacker);
                                        bool has魏武固阵 = buffs.Any(buff => buff.Name == "魏武固阵");
                                        // 如果攻击者有武道独尊，忽略魏武固阵
                                        if (has魏武固阵 && !guzhenAttacker.HasWuDaoDuZun)
                                        {
                                            // 使用防御等级进行计算
                                            skillLevel = guzhenAttacker.FinalDefenseLevel;
                                        }
                                    }
                                }
                                
                                // 确定对方技能采用的攻防等级
                                int targetSkillLevel = _targetDefenseLevel;
                                if (_currentTarget != null)
                                {
                                    // 检查对方是否有魏武固阵状态
                                    var buffs = _buffHandler.GetBuffs(_currentTarget);
                                    bool has魏武固阵 = buffs.Any(buff => buff.Name == "魏武固阵");
                                    // 如果目标有武道独尊，忽略魏武固阵
                                    if (has魏武固阵 && !_currentTarget.HasWuDaoDuZun)
                                    {
                                        // 使用防御等级进行计算
                                        targetSkillLevel = _currentTarget.FinalDefenseLevel;
                                    }
                                }
                                
                                // 根据攻击对抗类型和技能类型计算skillLevelMultiplier（攻防等级修正乘区）
                                double skillLevelMultiplier;
                                double multiplierRate = 0.03; // 默认倍率：3%
                            
                                // 检查是否是反击技能或默守蓄锋（使用4.5%倍率）
                                bool isCounterSkill = _currentAttackSlot != null && 
                                    (_currentAttackSlot.Type == ActionType.Counter || 
                                     _attackSkillName == "默守蓄锋");
                                if (isCounterSkill)
                                {
                                    multiplierRate = 0.045; // 反击技能和默守蓄锋4.5%
                                }
                                
                                // 无论是否对抗防御技能，都使用目标的防御等级进行计算
                                int targetLevelForCalculation = _currentTarget.FinalDefenseLevel;
                                int levelDifference = skillLevel - targetLevelForCalculation;
                                skillLevelMultiplier = 1.0 + ((double)levelDifference * multiplierRate);
                                
                                // skillLevelMultiplier的计算结果不低于0.2
                                skillLevelMultiplier = Math.Max(0.2, skillLevelMultiplier);
                                
                                // 获取攻击者
                                Character damageAttacker = null;
                                if (_currentAttackSlot != null && _slotToCharacterMap.ContainsKey(_currentAttackSlot))
                                {
                                    damageAttacker = _slotToCharacterMap[_currentAttackSlot];
                                }
                                
                                // 更新攻击者和目标的buff，确保FinalAttackLevel和FinalDefenseLevel是最新的
                                if (damageAttacker != null)
                                {
                                    damageAttacker.UpdateBuffs(_buffHandler);
                                }
                                if (_currentTarget != null)
                                {
                                    _currentTarget.UpdateBuffs(_buffHandler);
                                }
                                
                                // 更新技能的攻防等级，因为buff刚刚被更新了
                                int updatedSkillLevel = _attackLevel;
                                if (_currentAttackSlot != null)
                                {
                                    // 获取攻击者
                                    Character guzhenAttacker = null;
                                    foreach (var entry in _slotToCharacterMap)
                                    {
                                        if (entry.Key == _currentAttackSlot)
                                        {
                                            guzhenAttacker = entry.Value;
                                            break;
                                        }
                                    }
                                    
                                    if (guzhenAttacker != null)
                                    {
                                        // 检查是否有魏武固阵状态
                                        var buffs = _buffHandler.GetBuffs(guzhenAttacker);
                                        bool has魏武固阵 = buffs.Any(buff => buff.Name == "魏武固阵");
                                        // 如果攻击者有武道独尊，忽略魏武固阵
                                        if (has魏武固阵 && !guzhenAttacker.HasWuDaoDuZun)
                                        {
                                            // 使用防御等级进行计算
                                            updatedSkillLevel = guzhenAttacker.FinalDefenseLevel;
                                        }
                                        else
                                        {
                                            updatedSkillLevel = guzhenAttacker.FinalAttackLevel;
                                        }
                                    }
                                }
                                
                                // 更新对方技能的攻防等级
                                int updatedTargetSkillLevel = _targetDefenseLevel;
                                if (_currentTarget != null)
                                {
                                    // 检查对方是否有魏武固阵状态
                                    var buffs = _buffHandler.GetBuffs(_currentTarget);
                                    bool has魏武固阵 = buffs.Any(buff => buff.Name == "魏武固阵");
                                    // 如果目标有武道独尊，忽略魏武固阵
                                    if (has魏武固阵 && !_currentTarget.HasWuDaoDuZun)
                                    {
                                        // 使用防御等级进行计算
                                        updatedTargetSkillLevel = _currentTarget.FinalDefenseLevel;
                                    }
                                    else
                                    {
                                        updatedTargetSkillLevel = _currentTarget.FinalDefenseLevel;
                                    }
                                }
                                
                                // 更新攻防等级修正乘区skillLevelMultiplier
                                double updatedSkillLevelMultiplier;
                                double updatedMultiplierRate = 0.03; // 默认倍率：3%
                            
                                // 检查是否是反击技能或默守蓄锋（使用4.5%倍率）
                                bool updatedIsCounterSkill = _currentAttackSlot != null && 
                                    (_currentAttackSlot.Type == ActionType.Counter || 
                                     _attackSkillName == "默守蓄锋");
                                if (updatedIsCounterSkill)
                                {
                                    updatedMultiplierRate = 0.045; // 反击技能和默守蓄锋4.5%
                                }
                                
                                // 无论是否对抗防御技能，都使用目标的防御等级进行计算
                                int updatedTargetLevelForCalculation = _currentTarget.FinalDefenseLevel;
                                int updatedLevelDifference = updatedSkillLevel - updatedTargetLevelForCalculation;
                                updatedSkillLevelMultiplier = 1.0 + ((double)updatedLevelDifference * updatedMultiplierRate);
                                
                                // skillLevelMultiplier的计算结果不低于0.2
                                updatedSkillLevelMultiplier = Math.Max(0.2, updatedSkillLevelMultiplier);
                                
                                // 更新skillLevelMultiplier
                                skillLevelMultiplier = updatedSkillLevelMultiplier;
                                
                                // 一类增伤乘区damageMultiplier：(1+攻击者伤害提升-目标伤害减免)，最低0.2
                                float damageMultiplier = (1 + (damageAttacker?.DamageIncrease ?? 0f) - _currentTarget.DamageReduction);
                                
                                // 夏侯惇决断-夏侯惇真实伤害增伤：来源为夏侯惇、来源拥有【决断-夏侯惇】状态强度、伤害类型为真实伤害
                                Character jueDuanAttacker = null;
                                if (_currentAttackSlot != null && _slotToCharacterMap.ContainsKey(_currentAttackSlot))
                                {
                                    jueDuanAttacker = _slotToCharacterMap[_currentAttackSlot];
                                }
                                if (jueDuanAttacker != null && jueDuanAttacker is 夏侯惇 && _currentAttackSlot.DamageType == DamageType.True)
                                {
                                    var buffs = _buffHandler.GetBuffs(jueDuanAttacker);
                                    var jueDuanBuff = buffs.Find(b => b is 决断_夏侯惇);
                                    if (jueDuanBuff != null)
                                    {
                                        damageMultiplier += jueDuanBuff.Strength * 0.1f;
                                    }
                                }
                                
                                // 横斩技能沉默增强效果：所有硬币造成的伤害变为真实伤害，且临时获得20%伤害提升
                                if (_attackSkillName == "横斩")
                                {
                                    // 获取技能对象
                                    BaseSkill? skill = _currentAttackSlot.Skill;
                                    if (skill != null && skill.GetType().Name == "横斩")
                                    {
                                        // 检查HasSilenceEnhancement属性
                                        var hasSilenceEnhancementProp = skill.GetType().GetProperty("HasSilenceEnhancement");
                                        if (hasSilenceEnhancementProp != null)
                                        {
                                            bool hasSilenceEnhancement = (bool)hasSilenceEnhancementProp.GetValue(skill);
                                            if (hasSilenceEnhancement)
                                            {
                                                // 临时修改伤害类型为真实伤害
                                                _currentAttackSlot.DamageType = DamageType.True;
                                                // 临时获得20%伤害提升
                                                damageMultiplier += 0.2f;
                                            }
                                        }
                                    }
                                }
                                
                                // 拔矢啖睛技能最后一枚硬币效果：若目标的防御等级低于自身，则本硬币造成真实伤害且临时获得（自身忍耐效果强度x10）%伤害提升
                                if (_attackSkillName == "拔矢啖睛" && _currentCoinIndex == _originalCoins.Length - 1)
                                {
                                    // 检查目标的防御等级是否低于自身
                                    if (damageAttacker != null && _currentTarget != null && damageAttacker.FinalDefenseLevel > _currentTarget.FinalDefenseLevel)
                                    {
                                        // 临时修改伤害类型为真实伤害
                                        _currentAttackSlot.DamageType = DamageType.True;
                                        
                                        // 计算自身忍耐效果强度
                                        int enduranceStrength = 0;
                                        var buffs = _buffHandler.GetBuffs(damageAttacker);
                                        foreach (var buff in buffs)
                                        {
                                            if (buff is Endurance enduranceBuff)
                                            {
                                                enduranceStrength += enduranceBuff.Strength;
                                            }
                                        }
                                        
                                        // 临时获得（自身忍耐效果强度x10）%伤害提升
                                        if (enduranceStrength > 0)
                                        {
                                            damageMultiplier += enduranceStrength * 0.1f;
                                        }
                                    }
                                }
                                
                                damageMultiplier = Math.Max(0.2f, damageMultiplier);
                                
                                // 获取伤害种类抗性
                                float damageTypeResistance = 1.0f;
                                switch (_currentAttackSlot.DamageType)
                                {
                                    case DamageType.Physical:
                                        damageTypeResistance = _currentTarget.PhysicalVulnerability;
                                        break;
                                    case DamageType.Magic:
                                        damageTypeResistance = _currentTarget.MagicVulnerability;
                                        break;
                                    case DamageType.True:
                                        damageTypeResistance = _currentTarget.TrueVulnerability;
                                        break;
                                }
                                
                                // 获取攻击方式抗性
                                float attackTypeResistance = 1.0f;
                                switch (_currentAttackSlot.AttackType)
                                {
                                    case AttackType.Slash:
                                        attackTypeResistance = _currentTarget.SlashVulnerability;
                                        break;
                                    case AttackType.Blunt:
                                        attackTypeResistance = _currentTarget.BluntVulnerability;
                                        break;
                                    case AttackType.Pierce:
                                        attackTypeResistance = _currentTarget.PierceVulnerability;
                                        break;
                                    case AttackType.Spell:
                                        attackTypeResistance = _currentTarget.SpellVulnerability;
                                        break;
                                }
                                
                                // 确保抗性值不低于0.1
                                damageTypeResistance = Math.Max(0.1f, damageTypeResistance);
                                attackTypeResistance = Math.Max(0.1f, attackTypeResistance);
                                
                                // 最终伤害乘区finalDamageMultiplier：(1+攻击者最终伤害提升-目标最终伤害减免)
                                float finalDamageMultiplier = (1 + (damageAttacker?.FinalDamageIncrease ?? 0f) - _currentTarget.FinalDamageReduction);
                                
                                // 武道独尊效果：若张辽的攻击等级高于目标，则在攻击时临时获得（攻击等级差*0.75）%伤害增加，上限为75%
                                if (damageAttacker != null && damageAttacker.HasWuDaoDuZun)
                                {
                                    int wuDaoLevelDifference = damageAttacker.FinalAttackLevel - _currentTarget.FinalDefenseLevel;
                                    if (wuDaoLevelDifference > 0)
                                    {
                                        float damageBonus = Math.Min(wuDaoLevelDifference * 0.0075f, 0.75f);
                                        finalDamageMultiplier += damageBonus;
                                    }
                                }
                                
                                // 处理御甲鸣镝技能的30%最终伤害提升（如果没有次级目标）
                                if (_attackSkillName == "御甲鸣镝")
                                {
                                    if (_yujiaHasNoSecondaryTargets)
                                    {
                                        finalDamageMultiplier *= 1.3f; // 30%最终伤害提升
                                    }
                                }
                                
                                // finalDamageMultiplier的计算结果不低于0.2
                                finalDamageMultiplier = Math.Max(0.2f, finalDamageMultiplier);
                                
                                // 获取攻击者
                                Character critAttacker = null;
                                if (_currentAttackSlot != null && _slotToCharacterMap.ContainsKey(_currentAttackSlot))
                                {
                                    critAttacker = _slotToCharacterMap[_currentAttackSlot];
                                }
                                
                                // 暴击判定
                                bool isCriticalHit = false;
                                float critDamageMultiplier = 1.0f;
                                if (critAttacker != null)
                                {
                                    // 计算暴击概率
                                    float skillCritRate = _currentAttackSlot.Skill?.CritRate ?? 0f;
                                    float targetCritResistance = _currentTarget.CritResistance;
                                    float firstStepCritRate = Math.Max(0f, skillCritRate - targetCritResistance);
                                    float finalCritRateStep = (_currentAttackSlot.Skill?.FinalCritRate ?? 0f) - _currentTarget.FinalCritResistance;
                                    float totalCritRate = Math.Max(0f, firstStepCritRate + finalCritRateStep);
                                    totalCritRate = Math.Min(totalCritRate, 1.0f); // 超出100%视为100%
                                    
                                    // 按概率判定是否暴击
                                    Random critRandom = new Random();
                                    double randomValue = critRandom.NextDouble();
                                    if (randomValue < totalCritRate)
                                    {
                                        isCriticalHit = true;
                                    }
                                    
                                    // 计算暴击伤害乘区
                                    if (isCriticalHit)
                                    {
                                        float skillCritDamage = _currentAttackSlot.Skill?.CritDamage ?? 0f;
                                        float targetCritDamageResistance = _currentTarget.CritDamageResistance;
                                        critDamageMultiplier = 1 + (skillCritDamage - targetCritDamageResistance);
                                        critDamageMultiplier = Math.Max(1.0f, critDamageMultiplier); // 不低于1
                                    }
                                }
                                
                                // 计算最终伤害
                                int damage = (int)(finalValue * skillLevelMultiplier * damageMultiplier * finalDamageMultiplier * attackTypeResistance * damageTypeResistance * critDamageMultiplier);
                                
                                // 创建DamageCalculationResult对象
                                DamageCalculationResult damageResult = new DamageCalculationResult
                                {
                                    BaseValue = finalValue,
                                    SkillLevelMultiplier = skillLevelMultiplier,
                                    DamageMultiplier = damageMultiplier,
                                    FinalDamageMultiplier = finalDamageMultiplier,
                                    AttackTypeResistance = attackTypeResistance,
                                    DamageTypeResistance = damageTypeResistance,
                                    IsCrit = isCriticalHit,
                                    CritDamageMultiplier = critDamageMultiplier,
                                    FinalDamage = damage
                                };
                                
                                // 记录伤害计算前的护盾和血量值（用于盾击技能日志）
                                int shieldBefore = 0;
                                int healthBefore = 0;
                                if (_attackSkillName == "盾击")
                                {
                                    shieldBefore = GetCharacterShield(_currentTarget);
                                    healthBefore = _currentTarget.CurrentHealth;
                                }
                                
                                // 检查是否是第一枚和最后一枚硬币
                                bool isFirstCoinHit = (_currentCoinIndex == 0);
                                bool isLastCoinHit = (_currentCoinIndex == _originalCoins.Length - 1);
                                
                                // 设置行动槽的 IsFirstCoin 和 IsLastCoin 标志
                                _currentAttackSlot.IsFirstCoin = isFirstCoinHit;
                                _currentAttackSlot.IsLastCoin = isLastCoinHit;
                                
                                // 应用当前硬币的伤害
                                string targetTeamInfo = _currentTarget.IsAlly ? "-我方" : "-敌方";
                                string attackerTeamInfo = damageAttacker?.IsAlly == true ? "-我方" : "-敌方";
                                ApplyDamage(damage, _currentTarget, _currentAttackSlot, damageResult, isDirectDamage: true, isLastCoinHit: isLastCoinHit);
                                
                                // 处理曹丕的技能效果
                                if (damageAttacker is TurnBasedRPG.Characters.Allies.曹丕)
                                {
                                    var caoPiSkill = new TurnBasedRPG.Systems.SkillManagement.CaoPiSkill(this);
                                    caoPiSkill.HandleCaoPiSkillEffects(damageAttacker, _currentAttackSlot, _currentTarget, _buffHandler, GetAllCharacters(), this);
                                }
                                
                                // 处理张辽的技能效果
                                if (damageAttacker is TurnBasedRPG.Characters.Allies.张辽)
                                {
                                    var zhangLiaoSkill = new TurnBasedRPG.Systems.SkillManagement.ZhangLiaoSkill(this);
                                    zhangLiaoSkill.HandleZhangLiaoSkillEffects(damageAttacker, _currentAttackSlot, _currentTarget, _buffHandler, GetAllCharacters());
                                    
                                    // 检查是否有神威状态，且是最后一枚硬币命中
                                    bool hasShenWeiMorale = _buffHandler.CheckBuff<TurnBasedRPG.Buffs.Buff.神威>(damageAttacker);
                                    if (hasShenWeiMorale && isLastCoinHit)
                                    {
                                        // 额外扣除目标1点士气值
                                        _currentTarget.AdjustMorale(-1);
                                        BattleLog.Add($"{damageAttacker.Name}的神威最后一枚硬币命中，{_currentTarget.Name}士气-1");
                                    }
                                }
                                
                                // 检查攻击者是否是魏国武将，调用曹丕的 OnWeiSkillHit
                                if (damageAttacker != null && damageAttacker.Faction == Faction.魏)
                                {
                                    // 找到所有曹丕（包括友方和敌方）
                                    List<Character> allCaoPis = GetAllCharacters().FindAll(c => c is TurnBasedRPG.Characters.Allies.曹丕);
                                    foreach (var caoPi in allCaoPis)
                                    {
                                        var caoPiObj = caoPi as TurnBasedRPG.Characters.Allies.曹丕;
                                        if (caoPiObj != null && caoPiObj.IsAlly == damageAttacker.IsAlly)
                                        {
                                            caoPiObj.OnWeiSkillHit(_currentTarget, this, GetAllCharacters(), damageAttacker, isCounterSkill);
                                        }
                                    }
                                }
                                
                                // 处理横斩技能命中时的效果：使目标获得2级[脆弱]，持续1回合
                                if (_attackSkillName == "横斩")
                                {
                                    _buffHandler.AddBuff(_currentTarget, new 脆弱(1, 2));
                                    BattleLog.Add($"横斩使{_currentTarget.Name}获得2级脆弱，持续1回合");
                                }
                                
                                // 处理御甲鸣镝技能对次级目标的50%伤害
                                if (_attackSkillName == "御甲鸣镝")
                                {
                                    if (_yujiaSecondaryTargets != null && _yujiaSecondaryTargets.Count > 0)
                                    {
                                        // 对每个次级目标造成50%伤害
                                        int secondaryDamage = (int)(damage * 0.5f);
                                        foreach (var secondaryTarget in _yujiaSecondaryTargets)
                                        {
                                            if (secondaryTarget != null && !secondaryTarget.ShouldDie())
                                            {
                                                string secondaryTargetTeamInfo = secondaryTarget.IsAlly ? "-我方" : "-敌方";
                                                ApplyDamage(secondaryDamage, secondaryTarget, _currentAttackSlot, isDirectDamage: false, isLastCoinHit: isLastCoinHit);
                                                BattleLog.Add($"{_attackSkillName}对次级目标{secondaryTarget.Name}{secondaryTargetTeamInfo}造成{secondaryDamage}点伤害");
                                            }
                                        }
                                    }
                                }
                                
                                // 如果是盾击技能，添加伤害结算日志和护盾结算
                                if (_attackSkillName == "盾击")
                                {
                                    int shieldAfter = GetCharacterShield(_currentTarget);
                                    int healthAfter = _currentTarget.CurrentHealth;
                                    int shieldDamageTaken = shieldBefore - shieldAfter;
                                    int healthDamageTaken = healthBefore - healthAfter;
                                    int totalDamageTaken = shieldDamageTaken + healthDamageTaken;
                                    BattleLog.Add($"{_attackSkillName}共造成{shieldDamageTaken}点护盾伤害,{healthDamageTaken}点体力伤害");
                                    
                                    // 处理盾击技能每一枚硬币造成伤害时的效果
                                    if (shieldDamageTaken > 0 || healthDamageTaken > 0)
                                    {
                                        // 找到攻击者
                                        Character dunjiAttacker = null;
                                        foreach (var entry in _slotToCharacterMap)
                                        {
                                            if (entry.Key == _currentAttackSlot)
                                            {
                                                dunjiAttacker = entry.Value;
                                                break;
                                            }
                                        }
                                        
                                        if (dunjiAttacker != null)
                                        {
                                            // 使自身与同队其余魏国武将获得技能最终点数100%或生命上限7.5%的护盾（取较小值）
                                            int shieldFromSkill = finalValue;
                                            int shieldFromMaxHealth = (int)(dunjiAttacker.MaxHealth * 0.075f);
                                            int shieldAmount = Math.Min(shieldFromSkill, shieldFromMaxHealth);
                                            
                                            // 给攻击者自己加护盾
                                            if (shieldAmount > 0)
                                            {
                                                AddShield(dunjiAttacker, shieldAmount, "盾击效果");
                                            }
                                            
                                            // 给同队其余魏国武将加护盾
                                            List<Character> allies = new List<Character>();
                                            if (dunjiAttacker.IsAlly)
                                            {
                                                allies.AddRange(Players);
                                            }
                                            else
                                            {
                                                allies.AddRange(Enemies);
                                            }
                                            
                                            foreach (var ally in allies)
                                            {
                                                if (ally != dunjiAttacker && ally.Faction == Faction.魏)
                                                {
                                                    if (shieldAmount > 0)
                                                    {
                                                        AddShield(ally, shieldAmount, "盾击效果");
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                
                                // 处理铁壁战吼技能最后一枚硬币命中时的效果
                                if (_attackSkillName == "铁壁战吼" && _currentCoinIndex == _originalCoins.Length - 1)
                                {
                                    // 计算额外伤害（25%的真实伤害）
                                    int additionalDamage = (int)(damage * 0.25f);
                                    
                                    // 找到敌方随机目标
                                    List<Character> enemies = new List<Character>();
                                    if (_currentTarget.IsAlly)
                                    {
                                        // 如果目标是己方，那么敌方是Enemies
                                        enemies.AddRange(Enemies);
                                    }
                                    else
                                    {
                                        // 如果目标是敌方，那么敌方是Players
                                        enemies.AddRange(Players);
                                    }
                                    
                                    // 排除当前目标
                                    enemies.Remove(_currentTarget);
                                    
                                    if (enemies.Count > 0)
                                    {
                                        // 随机选择一个敌方目标
                                        Random randomEnemySelector = new Random();
                                        Character randomEnemy = enemies[randomEnemySelector.Next(enemies.Count)];
                                        
                                        // 对随机目标造成额外伤害
                                        if (additionalDamage > 0)
                                        {
                                            // 使用 ApplyDamage 方法处理真实伤害
                                            ApplyDamage(additionalDamage, randomEnemy, _currentAttackSlot, isDirectDamage: true);
                                            BattleLog.Add($"铁壁战吼对{randomEnemy.Name}造成{additionalDamage}点真实伤害");
                                        }
                                        
                                        // 使自身获得等同于该次额外伤害200%的护盾值
                                        int shieldAmount = (int)(additionalDamage * 2.0f);
                                        if (shieldAmount > 0)
                                        {
                                            // 找到攻击者
                                            Character tiebiAttacker = null;
                                            foreach (var entry in _slotToCharacterMap)
                                            {
                                                if (entry.Key == _currentAttackSlot)
                                                {
                                                    tiebiAttacker = entry.Value;
                                                    break;
                                                }
                                            }
                                            
                                            if (tiebiAttacker != null)
                                            {
                                                AddShield(tiebiAttacker, shieldAmount);
                                            }
                                        }
                                        
                                        // 使主要目标与附加伤害命中的目标获得3级[虚弱]，持续2回合
                                        _buffHandler.AddBuff(_currentTarget, new 虚弱(2, 3));
                                        if (randomEnemy != null)
                                        {
                                            _buffHandler.AddBuff(randomEnemy, new 虚弱(2, 3));
                                        }
                                        BattleLog.Add($"铁壁战吼使{_currentTarget.Name}和{randomEnemy?.Name ?? "随机目标"}获得3级虚弱，持续2回合");
                                    }
                                }
                                
                                // 处理汲魂技能命中时的效果
                                if (_attackSkillName == "汲魂" && _currentCoinIndex == _originalCoins.Length - 1)
                                {
                                    // 找到攻击者
                                    Character jihunAttacker = null;
                                    foreach (var entry in _slotToCharacterMap)
                                    {
                                        if (entry.Key == _currentAttackSlot)
                                        {
                                            jihunAttacker = entry.Value;
                                            break;
                                        }
                                    }
                                    
                                    if (jihunAttacker != null)
                                    {
                                        // [命中时]额外造成相当于消耗的护盾总值40%的真实伤害
                                        int extraTrueDamage = (int)(_jihunConsumedShieldTotal * 0.4f);
                                        if (extraTrueDamage > 0)
                                        {
                                            // 对当前目标造成额外真实伤害
                                            ApplyDamage(extraTrueDamage, _currentTarget, _currentAttackSlot, isDirectDamage: false);
                                            BattleLog.Add($"汲魂额外造成{extraTrueDamage}点真实伤害");
                                            
                                            // 为自身添加相当于此额外伤害值150%的护盾
                                            int shieldToAdd = (int)(extraTrueDamage * 1.5f);
                                            AddShield(jihunAttacker, shieldToAdd);
                                            BattleLog.Add($"汲魂为自身添加{shieldToAdd}点护盾");
                                        }
                                    }
                                }
                                
                                // 处理御甲鸣镝技能的效果
                                if (_attackSkillName == "御甲鸣镝")
                                {
                                    // 找到攻击者
                                    Character yujiaAttacker = null;
                                    foreach (var entry in _slotToCharacterMap)
                                    {
                                        if (entry.Key == _currentAttackSlot)
                                        {
                                            yujiaAttacker = entry.Value;
                                            break;
                                        }
                                    }
                                    
                                    if (yujiaAttacker != null)
                                    {
                                        // 每一枚硬币命中时，曹仁与同队魏国武将获得等同于5%最大生命的护盾
                                        int shieldAmount = (int)(yujiaAttacker.MaxHealth * 0.05f);
                                        if (shieldAmount > 0)
                                        {
                                            // 给攻击者自己加护盾
                                            AddShield(yujiaAttacker, shieldAmount);
                                            
                                            // 给同队魏国武将加护盾
                                            List<Character> allies = new List<Character>();
                                            if (yujiaAttacker.IsAlly)
                                            {
                                                allies.AddRange(Players);
                                            }
                                            else
                                            {
                                                allies.AddRange(Enemies);
                                            }
                                            
                                            foreach (var ally in allies)
                                            {
                                                if (ally != yujiaAttacker && ally.Faction == Faction.魏)
                                                {
                                                    AddShield(ally, shieldAmount);
                                                }
                                            }
                                        }
                                    }
                                }
                                
                                // 处理御甲鸣镝技能攻击后效果：使主要目标与所有次级目标获得[围城]，持续2回合
                                if (_attackSkillName == "御甲鸣镝" && _currentCoinIndex == _originalCoins.Length - 1)
                                {
                                    // 给主要目标添加围城状态
                                    _buffHandler.AddBuff(_currentTarget, new 围城(2, 1));
                                    BattleLog.Add($"御甲鸣镝使{_currentTarget.Name}获得围城，持续2回合");
                                    
                                    // 给次级目标添加围城状态
                                    if (_yujiaSecondaryTargets != null && _yujiaSecondaryTargets.Count > 0)
                                    {
                                        foreach (var secondaryTarget in _yujiaSecondaryTargets)
                                        {
                                            if (secondaryTarget != null && !secondaryTarget.ShouldDie())
                                            {
                                                _buffHandler.AddBuff(secondaryTarget, new 围城(2, 1));
                                                BattleLog.Add($"御甲鸣镝使{secondaryTarget.Name}获得围城，持续2回合");
                                            }
                                        }
                                    }
                                }
                                
                                // 处理镇岳反攻技能命中时的效果：复制目标的减益状态
                                if (_attackSkillName == "镇岳反攻")
                                {
                                    // 使用 DuplicateDebuffFromTargetToTarget 函数复制减益状态
                                    DuplicateDebuffFromTargetToTarget(_currentTarget, 2);
                                }
                                
                                // 处理默守蓄锋技能命中时的效果
                                if (_attackSkillName == "默守蓄锋")
                                {
                                    // 找到攻击者
                                    Character moshouAttacker = null;
                                    foreach (var entry in _slotToCharacterMap)
                                    {
                                        if (entry.Key == _currentAttackSlot)
                                        {
                                            moshouAttacker = entry.Value;
                                            break;
                                        }
                                    }
                                    
                                    if (moshouAttacker != null)
                                    {
                                        // 自身和同队其余魏国武将获得等同于10%最大生命的护盾
                                        int shieldAmount = (int)(moshouAttacker.MaxHealth * 0.10f);
                                        
                                        if (shieldAmount > 0)
                                        {
                                            // 给攻击者自己加护盾
                                            AddShield(moshouAttacker, shieldAmount, "默守蓄锋");
                                            // 记录护盾统计
                                            Statistics.RecordShield(moshouAttacker, "默守蓄锋", shieldAmount);
                                            
                                            // 给同队魏国武将加护盾
                                            List<Character> allies = new List<Character>();
                                            if (moshouAttacker.IsAlly)
                                            {
                                                allies.AddRange(Players);
                                            }
                                            else
                                            {
                                                allies.AddRange(Enemies);
                                            }
                                            
                                            foreach (var ally in allies)
                                            {
                                                if (ally != moshouAttacker && ally.Faction == Faction.魏)
                                                {
                                                    AddShield(ally, shieldAmount, "默守蓄锋");
                                                    // 记录护盾统计
                                                    Statistics.RecordShield(moshouAttacker, "默守蓄锋", shieldAmount);
                                                }
                                            }
                                        }
                                    }
                                }
                                
                                // 处理窃国者侯技能命中时效果：转移目标的增益状态
                                if (_attackSkillName == "窃国者侯")
                                {
                                    // 找到攻击者（司马懿）
                                    Character simaYi = null;
                                    foreach (var entry in _slotToCharacterMap)
                                    {
                                        if (entry.Key == _currentAttackSlot)
                                        {
                                            simaYi = entry.Value;
                                            break;
                                        }
                                    }
                                    
                                    if (simaYi != null)
                                    {
                                        // 处理主要目标
                                        TransferBuffFromTargetToAttacker(_currentTarget, simaYi);
                                        
                                        // 处理次级目标
                                        if (_yujiaSecondaryTargets != null && _yujiaSecondaryTargets.Count > 0)
                                        {
                                            foreach (var secondaryTarget in _yujiaSecondaryTargets)
                                            {
                                                if (secondaryTarget != null && !secondaryTarget.ShouldDie())
                                                {
                                                    TransferBuffFromTargetToAttacker(secondaryTarget, simaYi);
                                                }
                                            }
                                        }
                                    }
                                }
                                
                                // 处理窃国者侯技能攻击后效果：使所有目标获得2级[虚弱]，持续2回合
                                if (_attackSkillName == "窃国者侯" && _currentCoinIndex == _originalCoins.Length - 1)
                                {
                                    // 给主要目标添加虚弱状态
                                    _buffHandler.AddBuff(_currentTarget, new 虚弱(2, 2));
                                    BattleLog.Add($"窃国者侯使{_currentTarget.Name}获得2级虚弱，持续2回合");
                                    
                                    // 给次级目标添加虚弱状态
                                    if (_yujiaSecondaryTargets != null && _yujiaSecondaryTargets.Count > 0)
                                    {
                                        foreach (var secondaryTarget in _yujiaSecondaryTargets)
                                        {
                                            if (secondaryTarget != null && !secondaryTarget.ShouldDie())
                                            {
                                                _buffHandler.AddBuff(secondaryTarget, new 虚弱(2, 2));
                                                BattleLog.Add($"窃国者侯使{secondaryTarget.Name}获得2级虚弱，持续2回合");
                                            }
                                        }
                                    }
                                }
                                
                                // 处理镇岳反攻技能攻击后效果：额外造成真实伤害
                                if (_attackSkillName == "镇岳反攻" && _currentCoinIndex == _originalCoins.Length - 1)
                                {
                                    // 找到攻击者
                                    Character zhenyueAttacker = null;
                                    foreach (var entry in _slotToCharacterMap)
                                    {
                                        if (entry.Key == _currentAttackSlot)
                                        {
                                            zhenyueAttacker = entry.Value;
                                            break;
                                        }
                                    }
                                    
                                    if (zhenyueAttacker != null)
                                    {
                                        // 计算剩余护盾值
                                        int remainingShield = GetCharacterShield(zhenyueAttacker);
                                        
                                        // 计算真实伤害
                                        float shieldDamageMultiplier = 0.15f; // 15%基础倍率
                                        if (GetCharacterShield(_currentTarget) <= 0)
                                        {
                                            shieldDamageMultiplier = 0.20f; // 目标未持有护盾时提升至20%
                                        }
                                        
                                        int trueDamage = (int)(remainingShield * shieldDamageMultiplier);
                                        
                                        if (trueDamage > 0)
                                        {
                                            // 造成真实伤害
                                            ApplyDamage(trueDamage, _currentTarget, _currentAttackSlot, isDirectDamage: false);
                                            BattleLog.Add($"镇岳反攻额外造成{trueDamage}点真实伤害");
                                        }
                                    }
                                }
                                
                                // 增加硬币索引
                                _currentCoinIndex++;
                            }
                        }
                        break;
                    case ResolutionState.WaitingForSkillComplete:
                        _attackTimer += deltaTime;
                        if (_attackTimer >= SKILL_COMPLETE_DELAY)
                        {
                            _attackTimer = 0;
                            
                            // 检查是否有反击技能需要处理
                            if (_counterSkillInfo != null)
                            {
                                // 检查是否受到了伤害
                                if (_totalShieldDamage > _counterSkillInfo.OriginalShieldDamage || _totalHealthDamage > _counterSkillInfo.OriginalHealthDamage)
                                {
                                    // 受到了伤害，使用反击技能攻击对方
                                    ResolveSingleAction(_counterSkillInfo.CounterSlot, _counterSkillInfo.Attacker, _counterSkillInfo.Target);
                                }
                                else
                                {
                                    // 没有受到伤害，标记反击技能为已完成
                                    _counterSkillInfo.CounterSlot.IsCompleted = true;
                                    // 增加步骤并切换回WaitingForStep状态
                                    _resolutionStep++;
                                    _resolutionState = ResolutionState.WaitingForStep;
                                }
                                // 清除反击技能信息
                                _counterSkillInfo = null;
                            }
                            else
                            {
                                // 增加步骤并切换回WaitingForStep状态
                                _resolutionStep++;
                                _resolutionState = ResolutionState.WaitingForStep;
                            }
                        }
                        break;
                    case ResolutionState.WaitingForDodgeResult:
                        _attackTimer += deltaTime;
                        if (_attackTimer >= DODGE_RESULT_DELAY)
                        {
                            _attackTimer = 0;
                            // 增加步骤并切换回WaitingForStep状态
                            _resolutionStep++;
                            _resolutionState = ResolutionState.WaitingForStep;
                        }
                        break;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
    
    /// <summary>
    /// 从目标转移增益状态到攻击者
    /// </summary>
    /// <param name="target">目标角色</param>
    /// <param name="attacker">攻击者角色</param>
    private void TransferBuffFromTargetToAttacker(Character target, Character attacker)
    {
        // 获取目标的所有增益状态
        var targetBuffs = _buffHandler.GetBuffs(target);
        var transferableBuffs = targetBuffs.Where(buff => 
            buff.RemainingTurns.HasValue && buff.RemainingTurns > 0 && 
            buff.IsBuff && 
            !buff.IsFactionBuff
        ).ToList();
        
        if (transferableBuffs.Count > 0)
        {
            // 随机选择一个增益状态
            Random rng = new Random();
            var selectedBuff = transferableBuffs[rng.Next(transferableBuffs.Count)];
            
            // 为攻击者添加相同的增益状态
            if (selectedBuff is 韬晦)
            {
                _buffHandler.AddBuff(attacker, new 韬晦(selectedBuff.RemainingTurns.Value, selectedBuff.Strength));
            }
            else if (selectedBuff is 镇国)
            {
                _buffHandler.AddBuff(attacker, new 镇国(selectedBuff.RemainingTurns.Value, selectedBuff.Strength));
            }
            else if (selectedBuff is 同仇之盾)
            {
                _buffHandler.AddBuff(attacker, new 同仇之盾(false, selectedBuff.RemainingTurns.Value, selectedBuff.Strength));
            }
            else if (selectedBuff is 魏武固阵)
            {
                _buffHandler.AddBuff(attacker, new 魏武固阵(selectedBuff.RemainingTurns.Value, selectedBuff.Strength));
            }
            // 可以添加更多增益状态的处理
            
            // 移除目标身上的该增益状态
            _buffHandler.RemoveBuff(target, selectedBuff);
            
            BattleLog.Add($"窃国者侯将{target.Name}的{selectedBuff.Name}状态转移给了{attacker.Name}");
        }
    }
    
    /// <summary>
    /// 从目标复制减益状态到目标自身
    /// </summary>
    /// <param name="target">目标角色</param>
    /// <param name="maxCount">最大复制数量</param>
    private void DuplicateDebuffFromTargetToTarget(Character target, int maxCount = 2)
    {
        // 获取目标的所有减益状态
        var targetBuffs = _buffHandler.GetBuffs(target);
        var duplicateableDebuffs = targetBuffs.Where(buff => 
            buff.RemainingTurns.HasValue && buff.RemainingTurns > 0 && 
            !buff.IsBuff && 
            !buff.IsFactionBuff
        ).ToList();
        
        if (duplicateableDebuffs.Count > 0)
        {
            // 随机选择最多maxCount个减益状态
            Random rng = new Random();
            var selectedDebuffs = duplicateableDebuffs
                .OrderBy(x => rng.Next())
                .Take(maxCount)
                .ToList();
            
            foreach (var debuff in selectedDebuffs)
            {
                // 为目标添加相同的减益状态
                if (debuff is 脆弱)
                {
                    _buffHandler.AddBuff(target, new 脆弱(debuff.RemainingTurns.Value, debuff.Strength));
                }
                else if (debuff is 虚弱)
                {
                    _buffHandler.AddBuff(target, new 虚弱(debuff.RemainingTurns.Value, debuff.Strength));
                }
                else if (debuff is 围城)
                {
                    _buffHandler.AddBuff(target, new 围城(debuff.RemainingTurns.Value, debuff.Strength));
                }
                else if (debuff is TurnBasedRPG.Buffs.Debuff.Silence)
                {
                    _buffHandler.AddBuff(target, new TurnBasedRPG.Buffs.Debuff.Silence(debuff.RemainingTurns.Value, debuff.Strength));
                }
                // 可以添加更多减益状态的处理
                
                BattleLog.Add($"镇岳反攻复制了{target.Name}的{debuff.Name}状态");
            }
        }
    }
    
    // ==================== 目标系统相关方法 ====================
    
    // 为敌方行动槽分配目标
    public void AssignEnemyTargets()
    {
        // 重置所有敌方行动槽的目标
        foreach (var enemySlot in EnemySlots)
        {
            enemySlot.TargetSlot = null;
            enemySlot.IsUnilateralAttack = false;
        }
        
        var availableTargets = new List<ActionSlot>(PlayerSlots);
        var usedTargets = new HashSet<ActionSlot>();
        
        foreach (var enemySlot in EnemySlots)
        {
            ActionSlot target = null;
            
            // 优先选择未被其他敌方行动槽瞄准的目标
            var unusedTargets = availableTargets.Where(t => !usedTargets.Contains(t)).ToList();
            if (unusedTargets.Count > 0)
            {
                int index = _targetRandom.Next(unusedTargets.Count);
                target = unusedTargets[index];
                usedTargets.Add(target);
            }
            else
            {
                // 所有目标都被瞄准了，随机选择一个
                if (PlayerSlots.Count > 0)
                {
                    int index = _targetRandom.Next(PlayerSlots.Count);
                    target = PlayerSlots[index];
                }
            }
            
            if (target != null)
            {
                enemySlot.TargetSlot = target;
            }
        }
        
        // 初始判定单方面攻击
        EvaluateAllUnilateralAttacks();
        // 解决多对一冲突
        ResolveMultipleTargetsConflict();
    }
    
    // 为我方行动槽获取自动选择的目标
    public ActionSlot GetAutoTargetForPlayerSlot(ActionSlot playerSlot)
    {
        // 1. 找出所有瞄准此行动槽的敌方行动槽
        var targetingEnemies = EnemySlots.Where(e => e.TargetSlot == playerSlot).ToList();
        
        if (targetingEnemies.Count > 0)
        {
            // 2. 按速度降序，再按序号升序排序
            var sorted = targetingEnemies
                .OrderByDescending(e => e.Speed)
                .ThenBy(e => e.Index)
                .ToList();
            
            var selected = sorted.First();
            return selected;
        }
        
        // 3. 未被瞄准则随机选择，优先选择速度低于自身且未被其他己方行动槽瞄准的
        var targetedByOthers = PlayerSlots
            .Where(p => p != playerSlot && p.TargetSlot != null)
            .Select(p => p.TargetSlot)
            .ToHashSet();
        
        var preferredEnemies = EnemySlots.Where(e => 
            e.Speed < playerSlot.Speed && 
            !targetedByOthers.Contains(e)
        ).ToList();
        
        if (preferredEnemies.Count > 0)
        {
            int index = _targetRandom.Next(preferredEnemies.Count);
            var selected = preferredEnemies[index];
            return selected;
        }
        
        // 没有符合条件的，随机选一个
        if (EnemySlots.Count > 0)
        {
            int index = _targetRandom.Next(EnemySlots.Count);
            var selected = EnemySlots[index];
            return selected;
        }
        
        return null;
    }
    
    // 设置行动槽目标
    public void SetSlotTarget(ActionSlot slot, ActionSlot target)
    {
        if (slot == null || target == null)
            return;
        
        string slotTeam = slot.IsAlly ? "我方" : "敌方";
        string targetTeam = target.IsAlly ? "我方" : "敌方";
        
        slot.TargetSlot = target;
        slot.TargetSelectionOrder = ++_currentTargetSelectionOrder;
        
        // 特殊处理：如果是我方速度高的行动槽瞄准敌方速度低的行动槽，
        // 则强制将敌方行动槽的目标也设置为我方行动槽，形成互瞄关系（即使敌方已经有其他目标）
        if (slot.IsAlly && !target.IsAlly && slot.Speed > target.Speed)
        {
            target.TargetSlot = slot;
            target.TargetSelectionOrder = ++_currentTargetSelectionOrder;
        }
        
        // 重新评估单方面攻击和冲突
        EvaluateAllUnilateralAttacks();
        ResolveMultipleTargetsConflict();
    }
    
    // 评估所有单方面攻击
    public void EvaluateAllUnilateralAttacks()
    {
        // 重置所有行动槽的单方面攻击标记
        foreach (var slot in PlayerSlots.Concat(EnemySlots))
        {
            slot.IsUnilateralAttack = false;
        }
        
        // 检查每个行动槽
        foreach (var slot in PlayerSlots.Concat(EnemySlots))
        {
            if (slot.TargetSlot == null)
                continue;
            
            // 判定条件1：我方行动槽选取了速度值不低于自身的敌方行动槽，且被选为目标的敌方行动槽的目标不是本行动槽
            if (slot.IsAlly && !slot.TargetSlot.IsAlly)
            {
                if (slot.Speed <= slot.TargetSlot.Speed && slot.TargetSlot.TargetSlot != slot)
                {
                    slot.IsUnilateralAttack = true;
                }
            }
            
            // 判定条件2：行动槽没有被任何对方的行动槽指定为目标
            bool isTargetedByAnyone = false;
            var oppositeSlots = slot.IsAlly ? EnemySlots : PlayerSlots;
            foreach (var oppositeSlot in oppositeSlots)
            {
                if (oppositeSlot.TargetSlot == slot)
                {
                    isTargetedByAnyone = true;
                    break;
                }
            }
            
            if (!isTargetedByAnyone && slot.TargetSlot != null)
            {
                slot.IsUnilateralAttack = true;
            }
        }
    }
    
    // 解决多对一冲突（多个我方行动槽瞄准同一个敌方行动槽）
    public void ResolveMultipleTargetsConflict()
    {
        // 按敌方行动槽分组
        var targetGroups = PlayerSlots
            .Where(p => p.TargetSlot != null && !p.TargetSlot.IsAlly)
            .GroupBy(p => p.TargetSlot);
        
        foreach (var group in targetGroups)
        {
            var enemySlot = group.Key;
            var playerSlots = group.ToList();
            
            if (playerSlots.Count <= 1)
                continue;
            
            // 筛选符合条件的：速度高于目标，或者被目标瞄准
            var candidates = playerSlots.Where(p => 
                p.Speed > enemySlot.Speed || enemySlot.TargetSlot == p
            ).ToList();
            
            if (candidates.Count == 0)
            {
                // 全都不符合，全部单方面攻击
                foreach (var p in playerSlots)
                {
                    p.IsUnilateralAttack = true;
                }
                continue;
            }
            
            // 选出最后一个被选择的
            var winner = candidates.OrderByDescending(p => p.TargetSelectionOrder).First();
            
            // 标记其他为单方面攻击
            foreach (var p in playerSlots)
            {
                if (p != winner)
                {
                    p.IsUnilateralAttack = true;
                }
            }
        }
    }
    
    // 重置目标系统
    public void ResetTargetSystem()
    {
        _currentTargetSelectionOrder = 0;
        _manualSelectionSource = null;
        _inManualSelectionMode = false;
        
        foreach (var slot in PlayerSlots.Concat(EnemySlots))
        {
            slot.TargetSlot = null;
            slot.IsUnilateralAttack = false;
            slot.TargetSelectionOrder = 0;
            slot.IsTargetLocked = false;
        }
    }
    
    // 获取手动选择模式状态
    public bool IsInManualSelectionMode()
    {
        return _inManualSelectionMode;
    }
    
    // 获取手动选择的源行动槽
    public ActionSlot GetManualSelectionSource()
    {
        return _manualSelectionSource;
    }
    
    // 开始手动选择目标
    public void StartManualTargetSelection(ActionSlot sourceSlot)
    {
        _manualSelectionSource = sourceSlot;
        _inManualSelectionMode = true;
    }
    
    // 结束手动选择目标
    public void EndManualTargetSelection(ActionSlot targetSlot)
    {
        if (_manualSelectionSource != null && targetSlot != null)
        {
            SetSlotTarget(_manualSelectionSource, targetSlot);
        }
        _manualSelectionSource = null;
        _inManualSelectionMode = false;
    }
    
    // 取消手动选择目标
    public void CancelManualTargetSelection()
    {
        _manualSelectionSource = null;
        _inManualSelectionMode = false;
    }
    
    // ==================== 执行顺序重构相关方法 ====================
    
    // 计算行动槽执行顺序
    private void CalculateExecutionOrder()
    {
        _executionOrder.Clear();
        var allSlots = PlayerSlots.Concat(EnemySlots).ToList();
        var pairedSlots = new HashSet<ActionSlot>();
        
        // 按角色速度排序（同速时我方优先），再按行动槽序号
        var charactersBySpeed = Players.Concat(Enemies)
            .OrderByDescending(c => c.Speed)
            .ThenBy(c => !c.IsAlly); // 让我方角色在同速时排在前面
        
        foreach (var character in charactersBySpeed)
        {
            // 获取该角色的所有行动槽
            var characterSlots = allSlots
                .Where(s => _slotToCharacterMap[s] == character && !pairedSlots.Contains(s))
                .OrderBy(s => s.Index)
                .ToList();
            
            foreach (var slot in characterSlots)
            {
                if (pairedSlots.Contains(slot))
                    continue;
                
                // 检查是否有配对关系（互瞄且非单方面攻击）
                bool hasPair = false;
                if (slot.TargetSlot != null && !slot.IsUnilateralAttack && 
                    slot.TargetSlot.TargetSlot == slot && !pairedSlots.Contains(slot.TargetSlot))
                {
                    // 成对添加
                    if (slot.Speed >= slot.TargetSlot.Speed)
                    {
                        _executionOrder.Add(slot);
                        _executionOrder.Add(slot.TargetSlot);
                    }
                    else
                    {
                        _executionOrder.Add(slot.TargetSlot);
                        _executionOrder.Add(slot);
                    }
                    pairedSlots.Add(slot);
                    pairedSlots.Add(slot.TargetSlot);
                    hasPair = true;
                }
                
                if (!hasPair)
                {
                    // 单独添加
                    _executionOrder.Add(slot);
                    pairedSlots.Add(slot);
                }
            }
        }
    }
    
    // ==================== 非对位配对处理方法 ====================
    
    // ==================== 闪避技能新逻辑 ====================
    
    // 检查闪避技能是否应该对抗某个攻击技能
    private bool ShouldDodgeEngageWithAttack(ActionSlot dodgeSlot, ActionSlot attackSlot)
    {
        Character dodger = _slotToCharacterMap[dodgeSlot];
        Character attacker = _slotToCharacterMap[attackSlot];
        
        // 1. 不对反击技能生效（包括视为反击的特殊技）
        if (IsCounterSkill(attackSlot))
        {
            return false;
        }
        
        // 2. 攻击技能必须是攻击类型
        if (attackSlot.Type != ActionType.Attack)
        {
            return false;
        }
        
        // 3. 检查攻击技能的目标是否符合条件
        bool shouldEngage = false;
        
        // 条件A：攻击技能选定闪避技能所在行动槽为目标
        if (attackSlot.TargetSlot == dodgeSlot)
        {
            shouldEngage = true;
        }
        
        // 条件B：攻击技能未选定闪避行动槽，但选定了闪避角色的其他行动槽
        if (!shouldEngage && attackSlot.TargetSlot != null)
        {
            Character attackTarget = _slotToCharacterMap[attackSlot.TargetSlot];
            if (attackTarget == dodger)
            {
                shouldEngage = true;
            }
        }
        
        // 条件C：攻击行动槽的速度值低于闪避行动槽的速度值
        if (shouldEngage && attackSlot.Speed >= dodgeSlot.Speed)
        {
            shouldEngage = false;
        }
        
        return shouldEngage;
    }
    
    // 处理任意两个行动槽的对抗
    private void ResolveArbitrarySlotPair(ActionSlot slot1, ActionSlot slot2)
    {
        Character char1 = _slotToCharacterMap[slot1];
        Character char2 = _slotToCharacterMap[slot2];
        
        // 检查角色死亡
        if (IsCharacterDead(char1) && IsCharacterDead(char2))
        {
            slot1.IsDestroyed = true;
            slot1.IsCompleted = true;
            slot2.IsDestroyed = true;
            slot2.IsCompleted = true;
            return;
        }
        
        // 标记已死亡角色的行动槽
        if (IsCharacterDead(char1))
        {
            slot1.IsDestroyed = true;
            slot1.IsCompleted = true;
        }
        
        if (IsCharacterDead(char2))
        {
            slot2.IsDestroyed = true;
            slot2.IsCompleted = true;
        }
        
        // 确定哪个是玩家哪个是敌方
        ActionSlot playerSlot = slot1.IsAlly ? slot1 : slot2;
        ActionSlot enemySlot = slot1.IsAlly ? slot2 : slot1;
        Character playerChar = slot1.IsAlly ? char1 : char2;
        Character enemyChar = slot1.IsAlly ? char2 : char1;
        
        // 检查是否都已被摧毁
        if (playerSlot.IsDestroyed && enemySlot.IsDestroyed)
        {
            return;
        }
        
        // 获取有效的目标
        Character effectivePlayerTarget = enemyChar;
        Character effectiveEnemyTarget = playerChar;
        
        // 如果玩家攻击目标已死亡，寻找新目标
        if (playerSlot.Type == ActionType.Attack && !playerSlot.IsDestroyed && !playerSlot.IsCompleted && IsCharacterDead(enemyChar))
        {
            List<Character> aliveEnemies = Enemies.Where(e => !e.ShouldDie()).ToList();
            if (aliveEnemies.Count > 0)
            {
                Random random = new Random();
                effectivePlayerTarget = aliveEnemies[random.Next(aliveEnemies.Count)];
            }
        }
        
        // 如果敌方攻击目标已死亡，寻找新目标
        if (enemySlot.Type == ActionType.Attack && !enemySlot.IsDestroyed && !enemySlot.IsCompleted && IsCharacterDead(playerChar))
        {
            List<Character> alivePlayers = Players.Where(p => !p.ShouldDie()).ToList();
            if (alivePlayers.Count > 0)
            {
                Random random = new Random();
                effectiveEnemyTarget = alivePlayers[random.Next(alivePlayers.Count)];
            }
        }
        
        // 使用原有的ResolveActionPair逻辑，但传递正确的目标
        // 首先处理闪避技能与攻击技能的交互（使用新的闪避逻辑）
        if (playerSlot.Type == ActionType.Attack && !playerSlot.IsDestroyed && !playerSlot.IsCompleted && !IsCharacterDead(playerChar))
        {
            // 查找所有敌方闪避技能
            foreach (var enemyDodgeSlot in EnemySlots)
            {
                Character dodgeTarget = _slotToCharacterMap[enemyDodgeSlot];
                if (enemyDodgeSlot.Type == ActionType.Dodge && !enemyDodgeSlot.IsDestroyed && !enemyDodgeSlot.IsCompleted && !IsCharacterDead(dodgeTarget))
                {
                    // 使用新的闪避检查逻辑
                    if (ShouldDodgeEngageWithAttack(enemyDodgeSlot, playerSlot))
                    {
                        ResolveAttackVsDodge(playerSlot, enemyDodgeSlot, dodgeTarget);
                        if (playerSlot.IsDestroyed || playerSlot.IsCompleted)
                        {
                            break;
                        }
                    }
                }
            }
        }
        
        if (enemySlot.Type == ActionType.Attack && !enemySlot.IsDestroyed && !enemySlot.IsCompleted && !IsCharacterDead(enemyChar))
        {
            // 查找所有我方闪避技能
            foreach (var playerDodgeSlot in PlayerSlots)
            {
                Character dodgeTarget = _slotToCharacterMap[playerDodgeSlot];
                if (playerDodgeSlot.Type == ActionType.Dodge && !playerDodgeSlot.IsDestroyed && !playerDodgeSlot.IsCompleted && !IsCharacterDead(dodgeTarget))
                {
                    // 使用新的闪避检查逻辑
                    if (ShouldDodgeEngageWithAttack(playerDodgeSlot, enemySlot))
                    {
                        ResolveAttackVsDodge(enemySlot, playerDodgeSlot, dodgeTarget);
                        if (enemySlot.IsDestroyed || enemySlot.IsCompleted)
                        {
                            break;
                        }
                    }
                }
            }
        }
        
        // 检查是否只剩一方
        if (playerSlot.IsDestroyed || IsCharacterDead(playerChar))
        {
            if (!IsCharacterDead(enemyChar) && !IsCharacterDead(effectiveEnemyTarget))
            {
                ResolveSingleAction(enemySlot, enemyChar, effectiveEnemyTarget);
            }
            return;
        }
        
        if (enemySlot.IsDestroyed || IsCharacterDead(enemyChar))
        {
            if (!IsCharacterDead(playerChar) && !IsCharacterDead(effectivePlayerTarget))
            {
                ResolveSingleAction(playerSlot, playerChar, effectivePlayerTarget);
            }
            return;
        }
        
        // 双方都存活，调用原有的ResolveActionPair
        ResolveActionPair(playerSlot, enemySlot, playerChar, enemyChar);
    }
}
