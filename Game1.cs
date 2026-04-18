﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TurnBasedRPG.Characters;
using TurnBasedRPG.Characters.Allies;
using TurnBasedRPG.Characters.Skills.曹仁;
using TurnBasedRPG.Characters.Skills.夏侯惇;
using TurnBasedRPG.Characters.Skills.司马懿;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private BattleSystem _battleSystem;
    private SpriteFont _font; // Arial Black for English
    private SpriteFont _chineseFont; // Microsoft YaHei Bold for Chinese
    private Texture2D _pixel;
    private KeyboardState _previousKeyboardState;
    private MouseState _previousMouseState;
    
    // 技能详情查看相关变量
    private bool _isSkillDetailMode = false;
    private ActionSlot _selectedSkillSlot = null;
    private bool _isZooming = false;
    private bool _isZoomingOut = false;
    private float _zoomScale = 1.0f;
    private float _fadeAlpha = 1.0f;
    private Vector2 _cameraOffset = Vector2.Zero;
    private Vector2 _targetCameraOffset = Vector2.Zero;
    
    // 游戏状态相关变量
    private enum GameState { MainMenu, Battle, Encyclopedia, Tutorial }
    private GameState _currentGameState = GameState.MainMenu;
    
    // 角色详情界面相关变量
    private bool _isCharacterDetailMode = false;
    private Character _selectedCharacter = null;
    private bool _isCharacterZooming = false;
    private bool _isCharacterZoomingOut = false;
    private float _characterZoomScale = 1.0f;
    private float _characterFadeAlpha = 1.0f;
    private Vector2 _characterCameraOffset = Vector2.Zero;
    private Vector2 _characterTargetCameraOffset = Vector2.Zero;
    
    // 游戏百科页面相关变量
    private List<string> _encyclopediaTitles = new List<string>(); // 百科标题列表
    private Dictionary<string, string> _encyclopediaContent = new Dictionary<string, string>(); // 百科内容字典
    private string _selectedEncyclopediaTitle = null; // 当前选中的百科标题
    private float _encyclopediaScrollOffset = 0.0f; // 百科页面滚动条偏移量
    private bool _isDraggingEncyclopediaScrollBar = false; // 是否正在拖动百科页面滚动条
    
    // 教程系统相关变量
    private TurnBasedRPG.Tutorials.TutorialManager? _tutorialManager; // 教程管理器
    

    
    // 战斗日志滚动条相关变量
    private float _battleLogScrollOffset = 0.0f;
    private bool _isDraggingBattleLogScrollBar = false;
    private string _selectedSkillButton = null; // 技能1、技能2、技能3、守备技能
    private float _scrollOffset = 0.0f; // 滚动条偏移量
    private MainTitle _mainTitle;
    private static string logFilePath = "output.txt";
    
    // 伤害显示效果类
    private class DamageText
    {
        public string Text { get; set; }
        public Vector2 Position { get; set; }
        public Color TextColor { get; set; }
        public float Alpha { get; set; }
        public float YOffset { get; set; }
        public float LifeTime { get; set; }
        public float MaxLifeTime { get; set; }
        
        public DamageText(string text, Vector2 position, Color textColor)
        {
            Text = text;
            Position = position;
            TextColor = textColor;
            Alpha = 1.0f;
            YOffset = 0.0f;
            MaxLifeTime = 1.5f; // 1.5秒生命周期
            LifeTime = 0.0f;
        }
        
        public void Update(float deltaTime)
        {
            LifeTime += deltaTime;
            YOffset -= 30.0f * deltaTime; // 向上移动
            Alpha = 1.0f - (LifeTime / MaxLifeTime); // 逐渐透明
        }
        
        public bool IsDead()
        {
            return LifeTime >= MaxLifeTime;
        }
    }
    
    // 伤害显示效果列表
    private List<DamageText> _damageTexts = new List<DamageText>();
    
    // 角色到血条位置的映射字典
    private Dictionary<Character, Vector2> _characterHealthBarPositions = new Dictionary<Character, Vector2>();
    
    // 目标系统相关字段
    private Dictionary<ActionSlot, Vector2> _actionSlotLeftMidpoints = new Dictionary<ActionSlot, Vector2>(); // 行动槽左边缘中点
    private Dictionary<ActionSlot, Vector2> _actionSlotRightMidpoints = new Dictionary<ActionSlot, Vector2>(); // 行动槽右边缘中点
    private ActionSlot _hoveredActionSlot = null; // 当前鼠标悬停的行动槽
    private float _yellowBorderTimer = 0f; // 黄色边框闪烁计时器
    private const float YELLOW_BORDER_CYCLE = 0.5f; // 黄色边框闪烁周期（0.5秒）
    
    // 伤害事件处理方法
    private void BattleSystem_OnDamage(object sender, DamageEventArgs e)
    {
        if (_characterHealthBarPositions.ContainsKey(e.Target))
        {
            Vector2 healthBarPos = _characterHealthBarPositions[e.Target];
            string damageText = $"-{e.DamageAmount}";
            Color textColor = e.DamageType == HealthShieldDamageType.Shield ? Color.Blue : Color.Red;
            
            // 在血条最右侧的上方显示伤害文字
            Vector2 damagePos = new Vector2(healthBarPos.X + 100, healthBarPos.Y);
            _damageTexts.Add(new DamageText(damageText, damagePos, textColor));
        }
    }

    // 使用静态变量缓存日志消息，避免频繁打开文件
    private static List<string> _logMessages = new List<string>();
    private static object _logLock = new object();
    private static float _logFlushTimer = 0f;
    private const float LOG_FLUSH_INTERVAL = 1f; // 每秒刷新一次日志
    
    public static void Log(string message)
    {
        try
        {
            string logMessage = $"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}] {message}";
            // 设置控制台输出编码为UTF-8
            Console.OutputEncoding = new System.Text.UTF8Encoding(true);
            Console.WriteLine(logMessage);
            
            // 仅保留以下内容之一的日志：
            // - 回合结束/战斗结束时的贡献统计列表
            // - ApplyDamage相关内容
            // - AddShield相关内容
            bool shouldLogToFile = 
                message.Contains("贡献统计") || 
                message.Contains("贡献列表") ||
                message.Contains("ApplyDamage") ||
                message.Contains("AddShield");
            
            if (shouldLogToFile)
            {
                // 先缓存日志消息
                lock (_logLock)
                {
                    _logMessages.Add(logMessage);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"日志写入失败: {ex.Message}");
        }
    }
    
    // 新增方法：一次性将所有缓存的日志写入文件
    public static void FlushLogs()
    {
        try
        {
            lock (_logLock)
            {
                if (_logMessages.Count > 0)
                {
                    // 使用UTF-8带签名（BOM）编码
                    using (StreamWriter writer = new StreamWriter(logFilePath, true, new System.Text.UTF8Encoding(true)))
                    {
                        foreach (string msg in _logMessages)
                        {
                            writer.WriteLine(msg);
                        }
                    }
                    _logMessages.Clear();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"日志刷新失败: {ex.Message}");
        }
    }

    public Game1()
    {
        // 清空日志文件
        try
        {
            File.WriteAllText(logFilePath, "", new System.Text.UTF8Encoding(true));
            Log("[游戏启动] 日志文件初始化成功");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"日志文件初始化失败: {ex.Message}");
        }
        
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        _graphics.PreferredBackBufferWidth = 1600;
        _graphics.PreferredBackBufferHeight = 900;
        // 设置为无边框窗口模式
        _graphics.IsFullScreen = false;
        _graphics.PreferredBackBufferWidth = 1600;
        _graphics.PreferredBackBufferHeight = 900;
        // 应用设置
        _graphics.ApplyChanges();
        Log("[游戏启动] 游戏初始化成功");
    }

    protected override void Initialize()
    {
        try
        {
            base.Initialize();
        }
        catch (Exception)
        {
            throw;
        }
    }

    protected override void LoadContent()
    {
        try
        {
            // 设置控制台输出编码为 UTF-8
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Log("[游戏启动] 开始加载内容");
            
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            Log("[游戏启动] 创建SpriteBatch成功");
            
            try
            {
                _font = Content.Load<SpriteFont>("ChineseSpriteFont"); // Arial Black for English
                Log("[游戏启动] 加载ChineseSpriteFont成功");
            }
            catch (Exception fontEx)
            {
                Log($"[游戏启动] 加载ChineseSpriteFont失败: {fontEx.Message}");
                throw;
            }
            
            try
            {
                _chineseFont = Content.Load<SpriteFont>("ChineseBoldFont"); // Microsoft YaHei Bold for Chinese
                Log("[游戏启动] 加载ChineseBoldFont成功");
            }
            catch (Exception fontEx)
            {
                Log($"[游戏启动] 加载ChineseBoldFont失败: {fontEx.Message}");
                throw;
            }
            
            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
            Log("[游戏启动] 创建像素纹理成功");
            
            try
            {
                _battleSystem = new BattleSystem();
                // 订阅伤害事件
                _battleSystem.OnDamage += BattleSystem_OnDamage;
                Log("[游戏启动] 创建BattleSystem成功");
            }
            catch (Exception battleEx)
            {
                Log($"[游戏启动] 创建BattleSystem失败: {battleEx.Message}");
                throw;
            }
            
            try
            {
                // 初始化主菜单
                _mainTitle = new MainTitle(_spriteBatch, _font, _chineseFont, _pixel);
                Log("[游戏启动] 创建MainTitle成功");
            }
            catch (Exception mainTitleEx)
            {
                Log($"[游戏启动] 创建MainTitle失败: {mainTitleEx.Message}");
                throw;
            }
            
            Log("[游戏启动] 内容加载完成");
            
            // 初始化百科内容
            InitializeEncyclopediaContent();
            
            // 初始化教程管理器
            _tutorialManager = new TurnBasedRPG.Tutorials.TutorialManager();
            _tutorialManager.Initialize(_spriteBatch, _font, _chineseFont, _pixel, GraphicsDevice, this);
        }
        catch (Exception ex)
        {
            Log($"[游戏启动] 加载内容时发生异常: {ex.Message}");
            Log($"[游戏启动] 异常堆栈: {ex.StackTrace}");
            throw;
        }
    }

    private void InitializeEncyclopediaContent()
    {
        // 清空现有内容
        _encyclopediaTitles.Clear();
        _encyclopediaContent.Clear();
        
        // 添加标题和内容（二级标题用[SUBHEADING]标记，三级标题用[HEADING]标记）
        string title1 = "伤害计算公式";
        string content1 = @"[SUBHEADING]（一） 总公式[/SUBHEADING]
    最终伤害=基础值 × 技能等级修正 × 一类增伤 × 最终增伤 × 攻击方式易损 × 伤害种类易损。
[SUBHEADING]（二） 各乘区计算规则[/SUBHEADING]
    2.1  基础值=技能基础点数+硬币投掷总点数。
    2.2  技能等级修正= 1 + (攻击方技能等级-防御方防御等级)×3%。计算结果最低为0.2。
    2.3  一类增伤= 1 +攻击方伤害增加-防御方伤害减免。计算结果最低为0.2。
    2.4  最终增伤= 1 +攻击方最终伤害增加-防御方最终伤害减免。计算结果最低为0.2。
    2.5  攻击方式易损：根据技能攻击类型（斩/钝/穿刺/法术）采用目标对应系数，最低为0.1。
    2.6  伤害种类易损：根据技能伤害类型（物理/魔法/真实）采用目标对应系数。物理与魔法默认1.0，真实默认2.0，最低为0.1。
[SUBHEADING]（三） 暴击系统规则[/SUBHEADING]
    3.1  暴击判定：最终暴击率=基础暴击率+最终暴击率-暴击抗性-最终暴击抗性，最低为0%。
    3.2  暴击伤害：暴击伤害倍率= 1 +暴击伤害-暴击伤害抗性，最低为1.0。触发时，最终伤害乘算此倍率。";
        
        string title2 = "护盾系统规则";
        string content2 = @"[SUBHEADING]（一） 护盾施加[/SUBHEADING]
    1.1  实际护盾值=基础护盾值 ×(133% +护盾修正)。
[SUBHEADING]（二） 护盾消耗[/SUBHEADING]
    2.1  伤害结算顺序：优先扣除护盾，耗尽后再扣除生命值。
    2.2  护盾被击破：当次伤害使护盾值从>0变为≤0时触发。
[SUBHEADING]（三） 护盾相关效果[/SUBHEADING]
    3.1  持有护盾时，角色获得30%最终伤害减免（来自""同仇之盾""）。
    3.2  护盾被击破或受到伤害时，可触发""仁心""、""刚烈""、""默守""等技能特效。";
        
        string title3 = "状态系统规则";
        string content3 = @"[SUBHEADING]（一） 核心属性[/SUBHEADING]
    1.1  强度：影响效果数值，通常有上限。
    1.2  剩余回合：每回合结束减少，null表示永久。
    1.3  势力标记：带有此标记的状态（如""同仇之盾""）不可被转移/复制/驱散。
[SUBHEADING]（二） 生命周期流程[/SUBHEADING]
    2.1  回合开始：重置角色临时属性 → 更新所有状态 → 重新计算最终属性。
    2.2  回合结束：非永久状态剩余回合数减1→ 移除剩余回合数归零的状态。";
        
        string title4 = "士气值与技能对抗";
        string content4 = @"[SUBHEADING]（一） 技能构成[/SUBHEADING]
    1.1  技能拥有基础点数、若干个可投掷的硬币（正/反）、及每个硬币的固定点数。
[SUBHEADING]（二） 士气值影响[/SUBHEADING]
    2.1  初始士气为0，硬币投出正面概率为50%。
    2.2  士气值范围-20至20，正面概率= 50% + 2× 士气值，即在10%到90%间波动。
[SUBHEADING]（三） 攻击技能对抗（拼点）[/SUBHEADING]
    3.1  双方技能进行多次""拼点""：各自投掷一轮硬币计算总点数。
    3.2  拼点失败方移除其最靠前的一枚硬币。
    3.3  若仍有硬币，则再次拼点；否则，由胜方用剩余硬币投掷点数攻击败方。";
        
        string title5 = "技能池与弃牌";
        string content5 = @"[SUBHEADING]（一） 常规技能池轮换[/SUBHEADING]
    1.1  战斗开始时，行动槽获得一个由6个技能组成的技能池（3个1技能、2个2技能、1个3技能）及一个随机序列。
    1.2  每回合，玩家从行动槽和备选槽的技能中选取一个使用。
    1.3  回合结束时，丢弃行动槽中的技能，将备选槽技能移入，并从序列中取下一个技能填充备选槽。
    1.4  序列抽空后，立即生成新的技能池和序列。
[SUBHEADING]（二） 魏国武将弃牌机制[/SUBHEADING]
    2.1  魏国武将每回合会额外丢弃备选槽中未被使用的技能。
    2.2  根据丢弃的技能，获得独有""[决断]""状态，从而获得额外收益。此机制可用于管理技能回转节奏。";
        
        string title6 = "速度值规则";
        string content6 = @"[SUBHEADING]（一） 速度值生成[/SUBHEADING]
    1.1  每回合开始时，每个角色从其独有的速度值区间内随机抽取一个整数，作为本回合速度值。
[SUBHEADING]（二） 全局排序规则[/SUBHEADING]
    2.1  所有角色按速度值降序排列。
    2.2  同队内速度值相同的角色，按选取角色时的顺序排列。
    2.3  同一角色的多个行动槽，按槽位序号升序排列。";
        
        string title7 = "行动槽系统概述";
        string content7 = @"[SUBHEADING]（一） 核心概念[/SUBHEADING]
    1.1  战斗以""行动槽""为单位进行，每个角色拥有多个可装备技能的行动槽。
    1.2  所有行动槽根据速度值全局排序，交替执行，取代传统的敌我回合制。
[SUBHEADING]（二） 整体战斗流程[/SUBHEADING]
    2.1  选择阶段：敌方AI自动选择目标；玩家为我方行动槽选择技能与目标（可手动）；界面显示箭头表明瞄准关系。
    2.2  准备阶段：系统评估""单方面攻击""、裁决""多对一""冲突、计算最终执行顺序。
    2.3  行动阶段：严格按照顺序执行业务逻辑（技能对抗、伤害结算等）。
    2.4  结束阶段：检查死亡、触发回合结束效果、重置状态。
[SUBHEADING]（三） 行动顺序细则[/SUBHEADING]
    3.1  配对执行：互为目标的行动槽对（非单方面攻击）将绑定执行，按速度较高者的顺序行动。
    3.2  实际行动：所有行动槽/配对按全局排序依次执行。同速时，我方角色先于敌方角色行动。
[SUBHEADING]（四） 目标选取机制[/SUBHEADING]
1． 我方自动选择
        4.1.1  优先反击：若被敌方瞄准，则自动选择其中速度最高的作为目标，形成对抗。
        4.1.2  主动选择：若未被瞄准，则优先选择速度低于自身且未被友方瞄准的敌人。
        4.1.3  随机选择：若无符合上述条件的目标，则随机选择。
2． 我方手动选择：点击我方空槽进入选择模式（黄框闪烁），再点击敌方行动槽设定目标。
3． 敌方AI选择：优先选择未被其他敌方瞄准的我方行动槽，以分散火力。
[SUBHEADING]（五） 单方面攻击判定[/SUBHEADING]
1． 判定条件（满足其一）：
        5.1.1  A瞄准B，但B未瞄准A，且A的速度值不高于B。
        5.1.2  行动槽未被任何敌方行动槽瞄准。
2． 效果：以天蓝色箭头表示。不进行拼点，攻击方直接投掷所有硬币结算伤害。
[SUBHEADING]（六） 多对一冲突解决[/SUBHEADING]
    6.1  当多个我方行动槽瞄准同一敌人时，仅保留""速度高于目标""或""被目标反瞄""的候选者。
    6.2  在所有候选者中，最后设置目标的一个与目标形成正常对抗（红色箭头），其余降级为单方面攻击。
    6.3  自动反瞄：若我方高速单位A选中了尚无目标的低速敌人B，系统将自动把B的目标设为A，促成互瞄。
[SUBHEADING]（七） 闪避技能交互逻辑[/SUBHEADING]
    7.1  生效条件（需同时满足）：
        7.1.1  来袭技能不是反击技能或""视为反击""的技能。
        7.1.2  来袭技能为攻击技能。
        7.1.3  来袭技能以自身所在或所属角色的任一行动槽为目标。
        7.1.4  来袭技能所在行动槽的速度值不高于自身。
    7.2  对抗处理：满足条件时，闪避槽在其行动时机触发独立判定流程，与攻击方通过投掷硬币比对点数决定结果。
[SUBHEADING]（八） 界面可视化辅助[/SUBHEADING]
    8.1  全局视图：鼠标未悬停时，所有箭头半透明显示。
    8.2  聚焦视图：鼠标悬停于任一行动槽时，仅高亮显示与之相关的""瞄准""与""被瞄准""箭头。";
        
        // 添加到列表和字典
        _encyclopediaTitles.Add(title1);
        _encyclopediaTitles.Add(title2);
        _encyclopediaTitles.Add(title3);
        _encyclopediaTitles.Add(title4);
        _encyclopediaTitles.Add(title5);
        _encyclopediaTitles.Add(title6);
        _encyclopediaTitles.Add(title7);
        
        _encyclopediaContent[title1] = content1;
        _encyclopediaContent[title2] = content2;
        _encyclopediaContent[title3] = content3;
        _encyclopediaContent[title4] = content4;
        _encyclopediaContent[title5] = content5;
        _encyclopediaContent[title6] = content6;
        _encyclopediaContent[title7] = content7;
    }

    protected override void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        KeyboardState currentKeyboardState = Keyboard.GetState();
        MouseState currentMouseState = Mouse.GetState();
        
        // 更新黄色边框闪烁计时器
        _yellowBorderTimer += deltaTime;
        
        // 定期刷新日志到文件
        _logFlushTimer += deltaTime;
        if (_logFlushTimer >= LOG_FLUSH_INTERVAL)
        {
            FlushLogs();
            _logFlushTimer = 0f;
        }
        
        // 更新伤害显示效果
        for (int i = _damageTexts.Count - 1; i >= 0; i--)
        {
            _damageTexts[i].Update(deltaTime);
            if (_damageTexts[i].IsDead())
            {
                _damageTexts.RemoveAt(i);
            }
        }
        
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || currentKeyboardState.IsKeyDown(Keys.Escape))
        {
            // 退出前刷新所有日志
            FlushLogs();
            Exit();
        }

        // 根据游戏状态更新
        if (_currentGameState == GameState.MainMenu)
        {
            // 主界面逻辑
            if (_mainTitle.CheckClick(currentMouseState, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight))
            {
                switch (_mainTitle.LastClickedButton)
                {
                    case MainTitle.ButtonType.StartGame:
                        // 获取用户选择的行动槽数量
                        int slotCount = _mainTitle.SelectedSlotCount;
                        
                        // 开始战斗
                        // 根据已选择的角色创建BattleSystem
                        List<Character> allies = _mainTitle.SelectedAllies;
                        if (allies.Count == 0)
                        {
                            // 如果未选择，使用列表第一位己方角色
                            allies.Add(new Characters.Allies.示例角色1());
                        }
                        
                        List<Character> enemies = _mainTitle.SelectedEnemies;
                        if (enemies.Count == 0)
                        {
                            // 如果未选择，使用列表第一位敌方角色
                            enemies.Add(new Characters.Enemies.示例敌怪1());
                        }
                        
                        // 验证角色数量是否超过行动槽数量
                        if (allies.Count > slotCount || enemies.Count > slotCount)
                        {
                            // 这里可以添加一个错误提示的UI元素
                            return;
                        }
                        
                        // 创建BattleSystem时传入行动槽数量
                        _battleSystem = new BattleSystem(allies, enemies, slotCount);
                        // 订阅伤害事件
                        _battleSystem.OnDamage += BattleSystem_OnDamage;
                        _currentGameState = GameState.Battle;
                        break;
                        
                    case MainTitle.ButtonType.Encyclopedia:
                        _currentGameState = GameState.Encyclopedia;
                        break;
                        
                    case MainTitle.ButtonType.Tutorial:
                        _currentGameState = GameState.Tutorial;
                        break;
                }
            }
        }
        else if (_currentGameState == GameState.Encyclopedia)
        {
            // 游戏百科界面
            CheckEncyclopediaClick(currentMouseState, currentKeyboardState);
        }
        else if (_currentGameState == GameState.Tutorial)
        {
            // 教程入口界面 - 使用教程管理器
            if (_tutorialManager != null)
            {
                // 第一次进入教程状态时，显示关卡选择
                if (!_tutorialManager.ShowingLevelSelect && !_tutorialManager.IsInTutorial)
                {
                    _tutorialManager.ShowLevelSelect();
                }
                
                // 更新教程管理器
                _tutorialManager.Update(gameTime, currentMouseState, _previousMouseState, 
                                       currentKeyboardState, _previousKeyboardState);
                
                // 检查关卡选择点击
                if (_tutorialManager.ShowingLevelSelect)
                {
                    _tutorialManager.CheckLevelSelectClick(currentMouseState, _previousMouseState);
                }
                
                // 按Backspace键返回主菜单
                if (IsKeyPressed(Keys.Back, currentKeyboardState))
                {
                    ReturnToMainMenuFromTutorial();
                }
            }
        }
        else if (_currentGameState == GameState.Battle)
        {
            // 检测X按钮点击
            int xButtonSize = 30;
            int xButtonX = _graphics.PreferredBackBufferWidth - xButtonSize - 10;
            int xButtonY = 10;
            if (currentMouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
            {
                if (IsPointInRectangle(currentMouseState.X, currentMouseState.Y, xButtonX, xButtonY, xButtonSize, xButtonSize))
                {
                    // 回到主菜单
                    _currentGameState = GameState.MainMenu;
                    // 重置战斗相关变量
                    _isSkillDetailMode = false;
                    _isCharacterDetailMode = false;
                    _isZooming = false;
                    _isZoomingOut = false;
                    _isCharacterZooming = false;
                    _isCharacterZoomingOut = false;
                    _zoomScale = 1.0f;
                    _fadeAlpha = 1.0f;
                    _cameraOffset = Vector2.Zero;
                    _targetCameraOffset = Vector2.Zero;
                    _characterZoomScale = 1.0f;
                    _characterFadeAlpha = 1.0f;
                    _characterCameraOffset = Vector2.Zero;
                    _characterTargetCameraOffset = Vector2.Zero;
                    _selectedSkillButton = null;
                    _scrollOffset = 0.0f;
                    // 重置战斗日志滚动条
                    _battleLogScrollOffset = 0.0f;
                    _isDraggingBattleLogScrollBar = false;
                    // 重置角色选择
                    _mainTitle = new MainTitle(_spriteBatch, _font, _chineseFont, _pixel);
                    return;
                }
            }
            
            // 战斗界面逻辑
            // 空格键暂停/继续战斗
            if (IsKeyPressed(Keys.Space, currentKeyboardState))
            {
                _battleSystem.IsPaused = !_battleSystem.IsPaused;
            }
            
            if (_battleSystem.BattleEnded)
            {
                if (IsKeyPressed(Keys.R, currentKeyboardState))
                {
                    _battleSystem.Restart();
                }
                else if (IsKeyPressed(Keys.M, currentKeyboardState))
                {
                    // 回到主菜单
                    _currentGameState = GameState.MainMenu;
                }
            }
            else if (_battleSystem.CurrentPhase == BattlePhase.PlayerSelection)
            {
                if (IsKeyPressed(Keys.A, currentKeyboardState))
                {
                    // 保存当前技能序列作为原始技能序列
                    ActionSlot currentSlot = _battleSystem.PlayerSlots[_battleSystem.CurrentPlayerSlot];
                    currentSlot.SaveSkillSequence();
                    currentSlot.IsAlternativeSkillSelected = false; // A键选择行动槽技能（SelectedSkill）

                    _battleSystem.SetPlayerSlotAction(ActionType.Attack);
                }
                else if (IsKeyPressed(Keys.D, currentKeyboardState))
                {
                    // 获取当前行动槽所属的角色
                    Character currentCharacter = null;
                    if (_battleSystem != null && _battleSystem.PlayerSlots.Count > _battleSystem.CurrentPlayerSlot)
                    {
                        // 根据行动槽索引分配角色，确保每个角色对应固定的行动槽范围
                        int playerSlotIndex = _battleSystem.CurrentPlayerSlot;
                        int slotCounter = 0;
                        
                        foreach (var player in _battleSystem.Players)
                        {
                            // 计算每个角色的行动槽数量
                            int slotsPerCharacter = _battleSystem.PlayerSlots.Count / _battleSystem.Players.Count;
                            int remainingSlots = _battleSystem.PlayerSlots.Count % _battleSystem.Players.Count;
                            int slotsForThisCharacter = slotsPerCharacter + (slotCounter < remainingSlots ? 1 : 0);
                            
                            if (playerSlotIndex >= slotCounter * slotsPerCharacter && playerSlotIndex < slotCounter * slotsPerCharacter + slotsForThisCharacter)
                            {
                                currentCharacter = player;
                                break;
                            }
                            
                            slotCounter++;
                        }
                        
                        // 如果没有找到角色，使用默认角色
                        if (currentCharacter == null && _battleSystem.Players.Count > 0)
                        {
                            currentCharacter = _battleSystem.Players[0];
                        }
                    }
                    
                    if (currentCharacter != null)
                    {
                        BaseSkill defendSkill = currentCharacter.GetSkillByActionType(ActionType.Defend);
                        if (defendSkill != null && defendSkill.CanBeSelected)
                        {
                            _battleSystem.SetPlayerSlotAction(ActionType.Defend);
                        }
                    }
                }
                else if (IsKeyPressed(Keys.H, currentKeyboardState))
                {
                    // 获取当前行动槽所属的角色
                    Character currentCharacter = null;
                    if (_battleSystem != null && _battleSystem.PlayerSlots.Count > _battleSystem.CurrentPlayerSlot)
                    {
                        // 根据行动槽索引分配角色，确保每个角色对应固定的行动槽范围
                        int playerSlotIndex = _battleSystem.CurrentPlayerSlot;
                        int slotCounter = 0;
                        
                        foreach (var player in _battleSystem.Players)
                        {
                            // 计算每个角色的行动槽数量
                            int slotsPerCharacter = _battleSystem.PlayerSlots.Count / _battleSystem.Players.Count;
                            int remainingSlots = _battleSystem.PlayerSlots.Count % _battleSystem.Players.Count;
                            int slotsForThisCharacter = slotsPerCharacter + (slotCounter < remainingSlots ? 1 : 0);
                            
                            if (playerSlotIndex >= slotCounter * slotsPerCharacter && playerSlotIndex < slotCounter * slotsPerCharacter + slotsForThisCharacter)
                            {
                                currentCharacter = player;
                                break;
                            }
                            
                            slotCounter++;
                        }
                        
                        // 如果没有找到角色，使用默认角色
                        if (currentCharacter == null && _battleSystem.Players.Count > 0)
                        {
                            currentCharacter = _battleSystem.Players[0];
                        }
                    }
                    
                    if (currentCharacter != null)
                    {
                        BaseSkill healSkill = currentCharacter.GetSkillByActionType(ActionType.Heal);
                        if (healSkill != null && healSkill.CanBeSelected)
                        {
                            _battleSystem.SetPlayerSlotAction(ActionType.Heal);
                        }
                    }
                }
                else if (IsKeyPressed(Keys.S, currentKeyboardState))
                {
                    // 获取当前行动槽所属的角色
                    Character currentCharacter = null;
                    if (_battleSystem != null && _battleSystem.PlayerSlots.Count > _battleSystem.CurrentPlayerSlot)
                    {
                        // 根据行动槽索引分配角色，确保每个角色对应固定的行动槽范围
                        int playerSlotIndex = _battleSystem.CurrentPlayerSlot;
                        int slotCounter = 0;
                        
                        foreach (var player in _battleSystem.Players)
                        {
                            // 计算每个角色的行动槽数量
                            int slotsPerCharacter = _battleSystem.PlayerSlots.Count / _battleSystem.Players.Count;
                            int remainingSlots = _battleSystem.PlayerSlots.Count % _battleSystem.Players.Count;
                            int slotsForThisCharacter = slotsPerCharacter + (slotCounter < remainingSlots ? 1 : 0);
                            
                            if (playerSlotIndex >= slotCounter * slotsPerCharacter && playerSlotIndex < slotCounter * slotsPerCharacter + slotsForThisCharacter)
                            {
                                currentCharacter = player;
                                break;
                            }
                            
                            slotCounter++;
                        }
                        
                        // 如果没有找到角色，使用默认角色
                        if (currentCharacter == null && _battleSystem.Players.Count > 0)
                        {
                            currentCharacter = _battleSystem.Players[0];
                        }
                    }
                    
                    if (currentCharacter != null)
                    {
                        BaseSkill dodgeSkill = currentCharacter.GetSkillByActionType(ActionType.Dodge);
                        if (dodgeSkill != null && dodgeSkill.CanBeSelected)
                        {
                            _battleSystem.SetPlayerSlotAction(ActionType.Dodge);
                        }
                    }
                }
                else if (IsKeyPressed(Keys.C, currentKeyboardState))
                {
                    // 获取当前行动槽所属的角色
                    Character currentCharacter = null;
                    if (_battleSystem != null && _battleSystem.PlayerSlots.Count > _battleSystem.CurrentPlayerSlot)
                    {
                        // 根据行动槽索引分配角色，确保每个角色对应固定的行动槽范围
                        int playerSlotIndex = _battleSystem.CurrentPlayerSlot;
                        int slotCounter = 0;
                        
                        foreach (var player in _battleSystem.Players)
                        {
                            // 计算每个角色的行动槽数量
                            int slotsPerCharacter = _battleSystem.PlayerSlots.Count / _battleSystem.Players.Count;
                            int remainingSlots = _battleSystem.PlayerSlots.Count % _battleSystem.Players.Count;
                            int slotsForThisCharacter = slotsPerCharacter + (slotCounter < remainingSlots ? 1 : 0);
                            
                            if (playerSlotIndex >= slotCounter * slotsPerCharacter && playerSlotIndex < slotCounter * slotsPerCharacter + slotsForThisCharacter)
                            {
                                currentCharacter = player;
                                break;
                            }
                            
                            slotCounter++;
                        }
                        
                        // 如果没有找到角色，使用默认角色
                        if (currentCharacter == null && _battleSystem.Players.Count > 0)
                        {
                            currentCharacter = _battleSystem.Players[0];
                        }
                    }
                    
                    if (currentCharacter != null)
                    {
                        BaseSkill counterSkill = currentCharacter.GetSkillByActionType(ActionType.Counter);
                        if (counterSkill != null)
                        {
                            _battleSystem.SetPlayerSlotAction(ActionType.Counter);
                        }
                    }
                }
                else if (IsKeyPressed(Keys.B, currentKeyboardState))
                {
                    // 保存当前技能序列作为原始技能序列
                    ActionSlot currentSlot = _battleSystem.PlayerSlots[_battleSystem.CurrentPlayerSlot];
                    currentSlot.SaveSkillSequence();
                    currentSlot.IsAlternativeSkillSelected = true; // B键选择备选槽技能（NextSkill）

                    _battleSystem.SetPlayerSlotAction(ActionType.Attack);
                }
                else if (IsKeyPressed(Keys.Back, currentKeyboardState))
                {
                    if (_isSkillDetailMode)
                    {
                        // 开始镜头拉远动画
                        _isZoomingOut = true;
                    }
                    else if (_isCharacterDetailMode)
                    {
                        // 开始角色详情界面的镜头拉远动画
                        _isCharacterZoomingOut = true;
                    }
                    else
                    {
                        _battleSystem.UndoLastPlayerAction();
                    }
                }
                
                // 检测鼠标点击技能按钮
                CheckMouseClick(currentMouseState);
            }
            else if (_battleSystem.CurrentPhase == BattlePhase.Resolution)
            {
                _battleSystem.UpdateResolution((float)gameTime.ElapsedGameTime.TotalSeconds);
            }
            
            // 检测鼠标点击技能行动槽和角色图标（在所有战斗阶段都可以点击）
            if (!_isSkillDetailMode && !_isCharacterDetailMode)
            {
                CheckSkillSlotClick(currentMouseState);
                CheckCharacterIconClick(currentMouseState);
            }
            else if (_isSkillDetailMode)
            {
                // 检测退出技能详情模式的鼠标点击
                CheckExitSkillDetailClick(currentMouseState);
            }
            else if (_isCharacterDetailMode)
            {
                // 检测角色详情界面的技能按钮点击
                CheckCharacterSkillButtonClick(currentMouseState);
                // 检测滚动条操作
                CheckScrollBarClick(currentMouseState);
            }
            
            // 处理战斗日志滚动条
            CheckBattleLogScrollBar(currentMouseState);
            
            // 处理缩放动画
            if (_isZooming || _isZoomingOut)
            {
                UpdateZoom();
            }
            else if (_isCharacterZooming || _isCharacterZoomingOut)
            {
                UpdateCharacterZoom();
            }
        }

        _previousKeyboardState = currentKeyboardState;
        _previousMouseState = currentMouseState;
        
        // 检测鼠标悬停的行动槽（每帧都要检测）
        if (_battleSystem != null && _currentGameState == GameState.Battle)
        {
            _hoveredActionSlot = GetHoveredActionSlot(currentMouseState);
        }
        
        base.Update(gameTime);
    }
    
    // 获取鼠标悬停的行动槽
    private ActionSlot GetHoveredActionSlot(MouseState mouseState)
    {
        if (_battleSystem == null)
            return null;
        
        int slotWidth = 90;
        int slotHeight = 70;
        int infoBarHeight = 25;
        int spacing = 15;
        int rowSpacing = 40;
        int slotsPerRow = 4;
        int iconSize = 80;
        int slotOffset = 50;
        int moveRight = 90;
        
        // 检查玩家技能槽
        int playerSlotIndex = 0;
        for (int characterIndex = 0; characterIndex < _battleSystem.Players.Count; characterIndex++)
        {
            int slotsPerCharacter = _battleSystem.PlayerSlots.Count / _battleSystem.Players.Count;
            int remainingSlots = _battleSystem.PlayerSlots.Count % _battleSystem.Players.Count;
            int slotsForThisCharacter = slotsPerCharacter + (characterIndex < remainingSlots ? 1 : 0);
            
            int startX, startY;
            if (characterIndex == 0)
            {
                startX = 20 + iconSize + slotOffset + moveRight;
                startY = 60 + iconSize / 2 - (slotHeight - infoBarHeight) / 2;
            }
            else if (characterIndex == 1)
            {
                startX = 120 + iconSize + slotOffset + moveRight;
                startY = 190 + iconSize / 2 - (slotHeight - infoBarHeight) / 2;
            }
            else if (characterIndex == 2)
            {
                startX = 20 + iconSize + slotOffset + moveRight;
                startY = 320 + iconSize / 2 - (slotHeight - infoBarHeight) / 2;
            }
            else if (characterIndex == 3)
            {
                startX = 120 + iconSize + slotOffset + moveRight;
                startY = 450 + iconSize / 2 - (slotHeight - infoBarHeight) / 2;
            }
            else
            {
                startX = 20 + iconSize + slotOffset + moveRight;
                startY = 580 + iconSize / 2 - (slotHeight - infoBarHeight) / 2;
            }
            
            for (int j = 0; j < slotsForThisCharacter; j++)
            {
                if (playerSlotIndex >= _battleSystem.PlayerSlots.Count)
                    break;
                
                ActionSlot slot = _battleSystem.PlayerSlots[playerSlotIndex];
                int row = j / slotsPerRow;
                int col = j % slotsPerRow;
                int slotX = startX + (slotWidth + spacing) * col;
                int slotY = startY + (slotHeight + rowSpacing) * row - infoBarHeight;
                
                Rectangle slotRect = new Rectangle(slotX, slotY, slotWidth, slotHeight + infoBarHeight);
                if (slotRect.Contains(mouseState.Position))
                {
                    return slot;
                }
                playerSlotIndex++;
            }
        }
        
        // 检查敌人技能槽
        int enemySlotIndex = 0;
        for (int characterIndex = 0; characterIndex < _battleSystem.Enemies.Count; characterIndex++)
        {
            int slotsPerCharacter = _battleSystem.EnemySlots.Count / _battleSystem.Enemies.Count;
            int remainingSlots = _battleSystem.EnemySlots.Count % _battleSystem.Enemies.Count;
            int slotsForThisCharacter = slotsPerCharacter + (characterIndex < remainingSlots ? 1 : 0);
            
            int numRows = (slotsForThisCharacter + slotsPerRow - 1) / slotsPerRow;
            int lastRowSlotsCount = slotsForThisCharacter;
            if (lastRowSlotsCount > slotsPerRow)
            {
                lastRowSlotsCount = lastRowSlotsCount % slotsPerRow;
                if (lastRowSlotsCount == 0)
                    lastRowSlotsCount = slotsPerRow;
            }
            
            float currentRowWidth = lastRowSlotsCount * slotWidth + (lastRowSlotsCount - 1) * spacing;
            
            int startX, startY;
            if (characterIndex == 0)
            {
                startX = (int)(_graphics.PreferredBackBufferWidth - 200 - iconSize - slotOffset - currentRowWidth);
                startY = 60 + iconSize / 2 - (slotHeight - infoBarHeight) / 2;
            }
            else if (characterIndex == 1)
            {
                startX = (int)(_graphics.PreferredBackBufferWidth - 100 - iconSize - slotOffset - currentRowWidth);
                startY = 190 + iconSize / 2 - (slotHeight - infoBarHeight) / 2;
            }
            else if (characterIndex == 2)
            {
                startX = (int)(_graphics.PreferredBackBufferWidth - 200 - iconSize - slotOffset - currentRowWidth);
                startY = 320 + iconSize / 2 - (slotHeight - infoBarHeight) / 2;
            }
            else if (characterIndex == 3)
            {
                startX = (int)(_graphics.PreferredBackBufferWidth - 100 - iconSize - slotOffset - currentRowWidth);
                startY = 450 + iconSize / 2 - (slotHeight - infoBarHeight) / 2;
            }
            else
            {
                startX = (int)(_graphics.PreferredBackBufferWidth - 200 - iconSize - slotOffset - currentRowWidth);
                startY = 580 + iconSize / 2 - (slotHeight - infoBarHeight) / 2;
            }
            
            for (int j = 0; j < slotsForThisCharacter; j++)
            {
                if (enemySlotIndex >= _battleSystem.EnemySlots.Count)
                    break;
                
                ActionSlot slot = _battleSystem.EnemySlots[enemySlotIndex];
                int row = j / slotsPerRow;
                int col = j % slotsPerRow;
                
                int slotsInCurrentRow = row < numRows - 1 ? slotsPerRow : lastRowSlotsCount;
                float thisRowWidth = slotsInCurrentRow * slotWidth + (slotsInCurrentRow - 1) * spacing;
                
                int thisStartX;
                if (characterIndex == 0)
                    thisStartX = (int)(_graphics.PreferredBackBufferWidth - 200 - iconSize - slotOffset - thisRowWidth);
                else if (characterIndex == 1)
                    thisStartX = (int)(_graphics.PreferredBackBufferWidth - 100 - iconSize - slotOffset - thisRowWidth);
                else if (characterIndex == 2)
                    thisStartX = (int)(_graphics.PreferredBackBufferWidth - 200 - iconSize - slotOffset - thisRowWidth);
                else if (characterIndex == 3)
                    thisStartX = (int)(_graphics.PreferredBackBufferWidth - 100 - iconSize - slotOffset - thisRowWidth);
                else
                    thisStartX = (int)(_graphics.PreferredBackBufferWidth - 200 - iconSize - slotOffset - thisRowWidth);
                
                int slotX = thisStartX + (slotWidth + spacing) * col;
                int slotY = startY + (slotHeight + rowSpacing) * row - infoBarHeight;
                
                Rectangle slotRect = new Rectangle(slotX, slotY, slotWidth, slotHeight + infoBarHeight);
                if (slotRect.Contains(mouseState.Position))
                {
                    return slot;
                }
                enemySlotIndex++;
            }
        }
        
        return null;
    }
    
    private void CheckSkillSlotClick(MouseState currentMouseState)
    {
        // 只有在战斗准备阶段（玩家选择技能时）才能点击技能槽查看详情
        if (_battleSystem.CurrentPhase != BattlePhase.PlayerSelection && _battleSystem.CurrentPhase != BattlePhase.EnemySelection)
        {
            return;
        }
        
        if (currentMouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
        {
            // 先检查是否在手动选择目标模式
            if (_battleSystem.IsInManualSelectionMode())
            {
                HandleManualTargetSelectionClick(currentMouseState);
            }
            else
            {
                HandleNormalSkillSlotClick(currentMouseState);
            }
        }
    }
    
    // 处理手动选择目标模式的点击
    private void HandleManualTargetSelectionClick(MouseState currentMouseState)
    {
        ActionSlot targetSlot = null;
        int slotWidth = 90;
        int slotHeight = 70;
        int infoBarHeight = 25;
        int spacing = 15;
        int rowSpacing = 40;
        int slotsPerRow = 4;
        int iconSize = 80;
        int slotOffset = 50;
        
        // 检查敌人技能槽
        int enemySlotIndex = 0;
        for (int characterIndex = 0; characterIndex < _battleSystem.Enemies.Count; characterIndex++)
        {
            int slotsPerCharacter = _battleSystem.EnemySlots.Count / _battleSystem.Enemies.Count;
            int remainingSlots = _battleSystem.EnemySlots.Count % _battleSystem.Enemies.Count;
            int slotsForThisCharacter = slotsPerCharacter + (characterIndex < remainingSlots ? 1 : 0);
            
            int numRows = (slotsForThisCharacter + slotsPerRow - 1) / slotsPerRow;
            int lastRowSlotsCount = slotsForThisCharacter;
            if (lastRowSlotsCount > slotsPerRow)
            {
                lastRowSlotsCount = lastRowSlotsCount % slotsPerRow;
                if (lastRowSlotsCount == 0)
                    lastRowSlotsCount = slotsPerRow;
            }
            
            float currentRowWidth = lastRowSlotsCount * slotWidth + (lastRowSlotsCount - 1) * spacing;
            
            int startX, startY;
            if (characterIndex == 0)
            {
                startX = (int)(_graphics.PreferredBackBufferWidth - 200 - iconSize - slotOffset - currentRowWidth);
                startY = 60 + iconSize / 2 - (slotHeight - infoBarHeight) / 2;
            }
            else if (characterIndex == 1)
            {
                startX = (int)(_graphics.PreferredBackBufferWidth - 100 - iconSize - slotOffset - currentRowWidth);
                startY = 190 + iconSize / 2 - (slotHeight - infoBarHeight) / 2;
            }
            else if (characterIndex == 2)
            {
                startX = (int)(_graphics.PreferredBackBufferWidth - 200 - iconSize - slotOffset - currentRowWidth);
                startY = 320 + iconSize / 2 - (slotHeight - infoBarHeight) / 2;
            }
            else if (characterIndex == 3)
            {
                startX = (int)(_graphics.PreferredBackBufferWidth - 100 - iconSize - slotOffset - currentRowWidth);
                startY = 450 + iconSize / 2 - (slotHeight - infoBarHeight) / 2;
            }
            else
            {
                startX = (int)(_graphics.PreferredBackBufferWidth - 200 - iconSize - slotOffset - currentRowWidth);
                startY = 580 + iconSize / 2 - (slotHeight - infoBarHeight) / 2;
            }
            
            for (int j = 0; j < slotsForThisCharacter; j++)
            {
                if (enemySlotIndex >= _battleSystem.EnemySlots.Count)
                    break;
                
                ActionSlot slot = _battleSystem.EnemySlots[enemySlotIndex];
                int row = j / slotsPerRow;
                int col = j % slotsPerRow;
                
                int slotsInCurrentRow = row < numRows - 1 ? slotsPerRow : lastRowSlotsCount;
                float thisRowWidth = slotsInCurrentRow * slotWidth + (slotsInCurrentRow - 1) * spacing;
                
                int thisStartX;
                if (characterIndex == 0)
                    thisStartX = (int)(_graphics.PreferredBackBufferWidth - 200 - iconSize - slotOffset - thisRowWidth);
                else if (characterIndex == 1)
                    thisStartX = (int)(_graphics.PreferredBackBufferWidth - 100 - iconSize - slotOffset - thisRowWidth);
                else if (characterIndex == 2)
                    thisStartX = (int)(_graphics.PreferredBackBufferWidth - 200 - iconSize - slotOffset - thisRowWidth);
                else if (characterIndex == 3)
                    thisStartX = (int)(_graphics.PreferredBackBufferWidth - 100 - iconSize - slotOffset - thisRowWidth);
                else
                    thisStartX = (int)(_graphics.PreferredBackBufferWidth - 200 - iconSize - slotOffset - thisRowWidth);
                
                int slotX = thisStartX + (slotWidth + spacing) * col;
                int slotY = startY + (slotHeight + rowSpacing) * row - infoBarHeight;
                
                Rectangle slotRect = new Rectangle(slotX, slotY, slotWidth, slotHeight + infoBarHeight);
                if (slotRect.Contains(currentMouseState.Position))
                {
                    targetSlot = slot;
                    break;
                }
                enemySlotIndex++;
            }
            if (targetSlot != null)
                break;
        }
        
        if (targetSlot != null)
        {
            // 选择了目标，结束手动选择
            _battleSystem.EndManualTargetSelection(targetSlot);
            return;
        }
        else
        {
            // 点击了其他地方，取消手动选择
            _battleSystem.CancelManualTargetSelection();
            return;
        }
    }
    
    // 处理正常模式的技能槽点击
    private void HandleNormalSkillSlotClick(MouseState currentMouseState)
    {
        int slotWidth = 90;
        int slotHeight = 70;
        int infoBarHeight = 25;
        int spacing = 15;
        int rowSpacing = 40;
        int slotsPerRow = 4;
        int iconSize = 80;
        int slotOffset = 50;
        int moveRight = 90;
        
        // 检查玩家技能槽
        int playerSlotIndex = 0;
        for (int characterIndex = 0; characterIndex < _battleSystem.Players.Count; characterIndex++)
        {
            int slotsPerCharacter = _battleSystem.PlayerSlots.Count / _battleSystem.Players.Count;
            int remainingSlots = _battleSystem.PlayerSlots.Count % _battleSystem.Players.Count;
            int slotsForThisCharacter = slotsPerCharacter + (characterIndex < remainingSlots ? 1 : 0);
            
            int startX, startY;
            if (characterIndex == 0)
            {
                startX = 20 + iconSize + slotOffset + moveRight;
                startY = 60 + iconSize / 2 - (slotHeight - infoBarHeight) / 2;
            }
            else if (characterIndex == 1)
            {
                startX = 120 + iconSize + slotOffset + moveRight;
                startY = 190 + iconSize / 2 - (slotHeight - infoBarHeight) / 2;
            }
            else if (characterIndex == 2)
            {
                startX = 20 + iconSize + slotOffset + moveRight;
                startY = 320 + iconSize / 2 - (slotHeight - infoBarHeight) / 2;
            }
            else if (characterIndex == 3)
            {
                startX = 120 + iconSize + slotOffset + moveRight;
                startY = 450 + iconSize / 2 - (slotHeight - infoBarHeight) / 2;
            }
            else
            {
                startX = 20 + iconSize + slotOffset + moveRight;
                startY = 580 + iconSize / 2 - (slotHeight - infoBarHeight) / 2;
            }
            
            for (int j = 0; j < slotsForThisCharacter; j++)
            {
                if (playerSlotIndex >= _battleSystem.PlayerSlots.Count)
                    break;
                
                ActionSlot slot = _battleSystem.PlayerSlots[playerSlotIndex];
                int row = j / slotsPerRow;
                int col = j % slotsPerRow;
                int slotX = startX + (slotWidth + spacing) * col;
                int slotY = startY + (slotHeight + rowSpacing) * row - infoBarHeight;
                
                Rectangle slotRect = new Rectangle(slotX, slotY, slotWidth, slotHeight + infoBarHeight);
                if (slotRect.Contains(currentMouseState.Position))
                {
                    if (slot.Type != ActionType.None)
                    {
                        _selectedSkillSlot = slot;
                        _isZooming = true;
                        _isZoomingOut = false;
                        _fadeAlpha = 1.0f;
                        int windowWidth = _graphics.PreferredBackBufferWidth;
                        int windowHeight = _graphics.PreferredBackBufferHeight;
                        int slotCenterX = slotX + slotWidth / 2;
                        int slotCenterY = slotY + (slotHeight + infoBarHeight) / 2;
                        int leftThirdWidth = windowWidth / 3;
                        int detailIconSize = leftThirdWidth - 40;
                        int iconCenterX = 20 + detailIconSize / 2;
                        int iconCenterY = windowHeight / 2 - detailIconSize / 2 + detailIconSize / 2;
                        _cameraOffset = new Vector2(
                            windowWidth / 2 - slotCenterX,
                            windowHeight / 2 - slotCenterY
                        );
                        _targetCameraOffset = new Vector2(
                            windowWidth / 2 - iconCenterX,
                            windowHeight / 2 - iconCenterY
                        );
                        return;
                    }
                    else
                    {
                        // 技能槽为空，开始手动选择目标
                        _battleSystem.StartManualTargetSelection(slot);
                        _battleSystem.CurrentPlayerSlot = playerSlotIndex;
                        return;
                    }
                }
                playerSlotIndex++;
            }
        }
        
        // 检查敌人技能槽
        if (!_isSkillDetailMode)
        {
            int enemySlotIndex = 0;
            for (int characterIndex = 0; characterIndex < _battleSystem.Enemies.Count; characterIndex++)
            {
                int slotsPerCharacter = _battleSystem.EnemySlots.Count / _battleSystem.Enemies.Count;
                int remainingSlots = _battleSystem.EnemySlots.Count % _battleSystem.Enemies.Count;
                int slotsForThisCharacter = slotsPerCharacter + (characterIndex < remainingSlots ? 1 : 0);
                
                int numRows = (slotsForThisCharacter + slotsPerRow - 1) / slotsPerRow;
                int lastRowSlotsCount = slotsForThisCharacter;
                if (lastRowSlotsCount > slotsPerRow)
                {
                    lastRowSlotsCount = lastRowSlotsCount % slotsPerRow;
                    if (lastRowSlotsCount == 0)
                        lastRowSlotsCount = slotsPerRow;
                }
                
                for (int j = 0; j < slotsForThisCharacter; j++)
                {
                    if (enemySlotIndex >= _battleSystem.EnemySlots.Count)
                        break;
                    
                    ActionSlot slot = _battleSystem.EnemySlots[enemySlotIndex];
                    int row = j / slotsPerRow;
                    int col = j % slotsPerRow;
                    
                    int slotsInCurrentRow = row < numRows - 1 ? slotsPerRow : lastRowSlotsCount;
                    float currentRowWidth = slotsInCurrentRow * slotWidth + (slotsInCurrentRow - 1) * spacing;
                    
                    int startX, startY;
                    if (characterIndex == 0)
                    {
                        startX = (int)(_graphics.PreferredBackBufferWidth - 200 - iconSize - slotOffset - currentRowWidth);
                        startY = 60 + iconSize / 2 - (slotHeight - infoBarHeight) / 2;
                    }
                    else if (characterIndex == 1)
                    {
                        startX = (int)(_graphics.PreferredBackBufferWidth - 100 - iconSize - slotOffset - currentRowWidth);
                        startY = 190 + iconSize / 2 - (slotHeight - infoBarHeight) / 2;
                    }
                    else if (characterIndex == 2)
                    {
                        startX = (int)(_graphics.PreferredBackBufferWidth - 200 - iconSize - slotOffset - currentRowWidth);
                        startY = 320 + iconSize / 2 - (slotHeight - infoBarHeight) / 2;
                    }
                    else if (characterIndex == 3)
                    {
                        startX = (int)(_graphics.PreferredBackBufferWidth - 100 - iconSize - slotOffset - currentRowWidth);
                        startY = 450 + iconSize / 2 - (slotHeight - infoBarHeight) / 2;
                    }
                    else
                    {
                        startX = (int)(_graphics.PreferredBackBufferWidth - 200 - iconSize - slotOffset - currentRowWidth);
                        startY = 580 + iconSize / 2 - (slotHeight - infoBarHeight) / 2;
                    }
                    
                    int slotX = startX + (slotWidth + spacing) * col;
                    int slotY = startY + (slotHeight + rowSpacing) * row - infoBarHeight;
                    
                    Rectangle slotRect = new Rectangle(slotX, slotY, slotWidth, slotHeight + infoBarHeight);
                    if (slotRect.Contains(currentMouseState.Position))
                    {
                        if (slot.Type != ActionType.None)
                        {
                            _selectedSkillSlot = slot;
                            _isZooming = true;
                            _isZoomingOut = false;
                            _fadeAlpha = 1.0f;
                            int windowWidth = _graphics.PreferredBackBufferWidth;
                            int windowHeight = _graphics.PreferredBackBufferHeight;
                            int slotCenterX = slotX + slotWidth / 2;
                            int slotCenterY = slotY + (slotHeight + infoBarHeight) / 2;
                            int leftThirdWidth = windowWidth / 3;
                            int detailIconSize = leftThirdWidth - 40;
                            int iconCenterX = 20 + detailIconSize / 2;
                            int iconCenterY = windowHeight / 2 - detailIconSize / 2 + detailIconSize / 2;
                            _cameraOffset = new Vector2(
                                windowWidth / 2 - slotCenterX,
                                windowHeight / 2 - slotCenterY
                            );
                            _targetCameraOffset = new Vector2(
                                windowWidth / 2 - iconCenterX,
                                windowHeight / 2 - iconCenterY
                            );
                            return;
                        }
                    }
                    enemySlotIndex++;
                }
            }
        }
    }
    
    private void CheckExitSkillDetailClick(MouseState currentMouseState)
    {
        if (currentMouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
        {
            // 检查点击窗口左侧下方区域
            int leftThirdWidth = _graphics.PreferredBackBufferWidth / 3;
            int bottomHalfHeight = _graphics.PreferredBackBufferHeight / 2;
            
            Rectangle exitArea = new Rectangle(0, bottomHalfHeight, leftThirdWidth, bottomHalfHeight);
            if (exitArea.Contains(currentMouseState.Position))
            {
                // 开始镜头拉远动画
                _isZoomingOut = true;
            }
        }
    }
    
    private void UpdateZoom()
    {
        if (_isZooming)
        {
            // 镜头拉近动画
            if (_zoomScale < 3.0f)
            {
                _zoomScale += 0.08f; // 减慢缩放速度，使动画更明显
                _fadeAlpha -= 0.03f; // 减慢淡出速度
                if (_fadeAlpha < 0.0f)
                    _fadeAlpha = 0.0f;
                
                // 平滑过渡相机偏移，确保缩放中心逐渐移动
                _cameraOffset = Vector2.Lerp(_cameraOffset, _targetCameraOffset, 0.1f);
            }
            else
            {
                _isZooming = false;
                _fadeAlpha = 0.0f;
                _isSkillDetailMode = true; // 动画完成后才显示技能详情界面
            }
        }
        else if (_isZoomingOut)
        {
            // 镜头拉远动画
            if (_zoomScale > 1.0f)
            {
                _zoomScale -= 0.08f; // 减慢缩放速度，使动画更明显
                _fadeAlpha += 0.03f; // 减慢淡入速度
                if (_fadeAlpha > 1.0f)
                    _fadeAlpha = 1.0f;
                
                // 平滑过渡相机偏移到原点
                _cameraOffset = Vector2.Lerp(_cameraOffset, Vector2.Zero, 0.1f);
            }
            else
            {
                _isZoomingOut = false;
                _isSkillDetailMode = false;
                _selectedSkillSlot = null;
                _zoomScale = 1.0f;
                _fadeAlpha = 1.0f;
                _cameraOffset = Vector2.Zero;
            }
        }
    }

    private bool IsKeyPressed(Keys key, KeyboardState currentState)
    {
        return currentState.IsKeyDown(key) && !_previousKeyboardState.IsKeyDown(key);
    }

    private bool IsChineseText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }
        foreach (char c in text)
        {
            if (c >= 0x4E00 && c <= 0x9FFF)
            {
                return true;
            }
        }
        return false;
    }

    private SpriteFont GetFontForText(string text)
    {
        try
        {
            if (IsChineseText(text))
            {
                // 确保中文字体已加载且可用
                if (_chineseFont != null)
                {
                    try
                    {
                        // 测试中文字体是否能正常使用当前文本
                        _chineseFont.MeasureString(text);
                        return _chineseFont;
                    }
                    catch (Exception ex)
                    {
                        // 中文字体不支持某些字符，回退到默认字体
                        return _font;
                    }
                }
                else
                {
                    // 中文字体未加载，使用默认字体
                    return _font;
                }
            }
            else
            {
                return _font;
            }
        }
        catch (Exception)
        {
            // 发生异常时使用默认字体
            return _font;
        }
    }

    private void CheckMouseClick(MouseState currentMouseState)
    {
        // 在技能详情界面、角色详情界面或动画过程中不触发技能选择
        if (_isSkillDetailMode || _isCharacterDetailMode || _isZooming || _isZoomingOut || _isCharacterZooming || _isCharacterZoomingOut)
        {
            return;
        }
        
        // 检查鼠标左键是否按下
        if (currentMouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
        {
            int buttonWidth = 120; // 与DrawSkillSelectionButtons中一致
            int buttonHeight = 60;
            int spacing = 20; // 与DrawSkillSelectionButtons中一致
            int startY = 800;
            
            // 获取当前行动槽
            ActionSlot currentSlot = _battleSystem.PlayerSlots[_battleSystem.CurrentPlayerSlot];
            
            // 获取当前行动槽所属的角色
            Character currentCharacter = null;
            if (_battleSystem != null && _battleSystem.PlayerSlots.Count > _battleSystem.CurrentPlayerSlot)
            {
                // 根据行动槽索引分配角色，确保每个角色对应固定的行动槽范围
                int playerSlotIndex = _battleSystem.CurrentPlayerSlot;
                int slotCounter = 0;
                
                foreach (var player in _battleSystem.Players)
                {
                    // 计算每个角色的行动槽数量
                    int slotsPerCharacter = _battleSystem.PlayerSlots.Count / _battleSystem.Players.Count;
                    int remainingSlots = _battleSystem.PlayerSlots.Count % _battleSystem.Players.Count;
                    int slotsForThisCharacter = slotsPerCharacter + (slotCounter < remainingSlots ? 1 : 0);
                    
                    if (playerSlotIndex >= slotCounter * slotsPerCharacter && playerSlotIndex < slotCounter * slotsPerCharacter + slotsForThisCharacter)
                    {
                        currentCharacter = player;
                        break;
                    }
                    
                    slotCounter++;
                }
                
                // 如果没有找到角色，使用默认角色
                if (currentCharacter == null && _battleSystem.Players.Count > 0)
                {
                    currentCharacter = _battleSystem.Players[0];
                }
            }
            
            // 备选技能参数
            bool hasAltSkill = false;
            if (currentCharacter != null && currentSlot.NextSkill.HasValue)
            {
                BaseSkill altSkill = currentCharacter.GetSkillByActionType(ActionType.Attack, currentSlot.NextSkill);
                if (altSkill != null)
                {
                    hasAltSkill = true;
                }
            }
            
            // 防御技能参数
            BaseSkill defendSkill = null;
            if (currentCharacter != null)
            {
                defendSkill = currentCharacter.GetSkillByActionType(ActionType.Defend);
            }
            
            // 治疗技能参数
            BaseSkill healSkill = null;
            if (currentCharacter != null)
            {
                healSkill = currentCharacter.GetSkillByActionType(ActionType.Heal);
            }
            
            // 闪避技能参数
            BaseSkill dodgeSkill = null;
            if (currentCharacter != null)
            {
                dodgeSkill = currentCharacter.GetSkillByActionType(ActionType.Dodge);
            }
            
            // 反击技能参数
            BaseSkill counterSkill = null;
            if (currentCharacter != null)
            {
                counterSkill = currentCharacter.GetSkillByActionType(ActionType.Counter);
            }
            
            // 收集可用的技能按钮（与绘制时相同）
            List<SkillButtonInfo> availableButtons = new List<SkillButtonInfo>();
            
            // 攻击按钮
            availableButtons.Add(new SkillButtonInfo("A", "攻击", ActionType.Attack, 0, 0, 0));
            
            // 备选按钮
            if (hasAltSkill)
            {
                availableButtons.Add(new SkillButtonInfo("B", "备选", ActionType.Attack, 0, 0, 0));
            }
            
            // 防御按钮
            if (defendSkill != null && defendSkill.CanBeSelected)
            {
                availableButtons.Add(new SkillButtonInfo("D", "防御", ActionType.Defend, 0, 0, 0));
            }
            
            // 闪避按钮
            if (dodgeSkill != null && dodgeSkill.CanBeSelected)
            {
                availableButtons.Add(new SkillButtonInfo("S", "闪避", ActionType.Dodge, 0, 0, 0));
            }
            
            // 治疗按钮
            if (healSkill != null && healSkill.CanBeSelected)
            {
                availableButtons.Add(new SkillButtonInfo("H", "治疗", ActionType.Heal, 0, 0, 0));
            }
            
            // 反击按钮
            if (counterSkill != null && counterSkill.CanBeSelected)
            {
                availableButtons.Add(new SkillButtonInfo("C", "反击", ActionType.Counter, 0, 0, 0));
            }
            
            // 计算起始X坐标，确保按钮居中对齐（与绘制时相同）
            int totalWidth = availableButtons.Count * buttonWidth + (availableButtons.Count - 1) * spacing;
            int startX = (_graphics.PreferredBackBufferWidth - totalWidth) / 2;
            
            // 检查点击位置是否在某个按钮的区域内
            for (int i = 0; i < availableButtons.Count; i++)
            {
                SkillButtonInfo buttonInfo = availableButtons[i];
                int buttonX = startX + i * (buttonWidth + spacing);
                
                if (IsPointInRectangle(currentMouseState.X, currentMouseState.Y, buttonX, startY, buttonWidth, buttonHeight))
                {
                    // 保存当前技能序列作为原始技能序列
                    currentSlot.SaveSkillSequence();
                    
                    if (buttonInfo.Name == "攻击")
                    {
                        currentSlot.IsAlternativeSkillSelected = false;

                        _battleSystem.SetPlayerSlotAction(ActionType.Attack);
                    }
                    else if (buttonInfo.Name == "备选")
                    {
                        currentSlot.IsAlternativeSkillSelected = true;

                        _battleSystem.SetPlayerSlotAction(ActionType.Attack);
                    }
                    else if (buttonInfo.Name == "防御")
                    {
                        _battleSystem.SetPlayerSlotAction(ActionType.Defend);
                    }
                    else if (buttonInfo.Name == "闪避")
                    {
                        _battleSystem.SetPlayerSlotAction(ActionType.Dodge);
                    }
                    else if (buttonInfo.Name == "治疗")
                    {
                        _battleSystem.SetPlayerSlotAction(ActionType.Heal);
                    }
                    else if (buttonInfo.Name == "反击")
                    {
                        _battleSystem.SetPlayerSlotAction(ActionType.Counter);
                    }
                    
                    break;
                }
            }
        }
    }
    
    private void CheckCharacterIconClick(MouseState currentMouseState)
    {
        // 只有在战斗准备阶段（玩家选择技能时）才能点击角色图标查看详情
        if (_battleSystem.CurrentPhase != BattlePhase.PlayerSelection && _battleSystem.CurrentPhase != BattlePhase.EnemySelection)
        {
            return;
        }
        
        if (currentMouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
        {
            int iconSize = 80; // 与DrawCharacters方法中一致
            
            // 检查玩家角色图标
            for (int i = 0; i < _battleSystem.Players.Count; i++)
            {
                int playerIconX, playerIconY;
                
                // 根据角色索引计算位置，与DrawCharacters方法中一致
                if (i == 0) // 第一名玩家
                {
                    playerIconX = 20;
                    playerIconY = 60;
                }
                else if (i == 1) // 第二名玩家
                {
                    playerIconX = 120;
                    playerIconY = 190;
                }
                else if (i == 2) // 第三名玩家
                {
                    playerIconX = 20;
                    playerIconY = 320;
                }
                else if (i == 3) // 第四名玩家
                {
                    playerIconX = 120;
                    playerIconY = 450;
                }
                else // 第五名玩家
                {
                    playerIconX = 20;
                    playerIconY = 580;
                }
                
                Rectangle playerIconRect = new Rectangle(playerIconX, playerIconY, iconSize, iconSize);
                if (playerIconRect.Contains(currentMouseState.Position))
                {
                    _selectedCharacter = _battleSystem.Players[i];
                    _isCharacterZooming = true;
                    _isCharacterZoomingOut = false;
                    _characterFadeAlpha = 1.0f; // 初始化淡出 alpha
                    _selectedSkillButton = null; // 重置选中的技能按钮
                    _scrollOffset = 0.0f; // 重置滚动条偏移
                    
                    // 计算相机偏移，使角色图标在缩放时位于屏幕中心
                    int windowWidth = _graphics.PreferredBackBufferWidth;
                    int windowHeight = _graphics.PreferredBackBufferHeight;
                    int iconCenterX = playerIconX + iconSize / 2;
                    int iconCenterY = playerIconY + iconSize / 2;
                    
                    // 计算角色详情界面中角色图标的中心位置
                    int leftThirdWidth = windowWidth / 3;
                    int detailIconSize = leftThirdWidth - 40;
                    int detailIconCenterX = 20 + detailIconSize / 2;
                    int detailIconCenterY = windowHeight / 2 - detailIconSize / 2 + detailIconSize / 2; // 图标下边缘与中线平齐
                    
                    // 计算初始相机偏移（A点）
                    // 我们需要将被点击的角色图标中心（A点）移动到屏幕中心
                    _characterCameraOffset = new Vector2(
                        windowWidth / 2 - iconCenterX,
                        windowHeight / 2 - iconCenterY
                    );
                    
                    // 计算目标相机偏移（B点）
                    // 我们需要将角色详情图标中心（B点）移动到屏幕中心
                    _characterTargetCameraOffset = new Vector2(
                        windowWidth / 2 - detailIconCenterX,
                        windowHeight / 2 - detailIconCenterY
                    );
                    return;
                }
            }
            
            // 检查敌人角色图标
            for (int i = 0; i < _battleSystem.Enemies.Count; i++)
            {
                int enemyIconX, enemyIconY;
                
                // 根据角色索引计算位置，与DrawCharacters方法中一致
                if (i == 0) // 第一名敌方
                {
                    enemyIconX = _graphics.PreferredBackBufferWidth - 200;
                    enemyIconY = 60;
                }
                else if (i == 1) // 第二名敌方
                {
                    enemyIconX = _graphics.PreferredBackBufferWidth - 100;
                    enemyIconY = 190;
                }
                else if (i == 2) // 第三名敌方
                {
                    enemyIconX = _graphics.PreferredBackBufferWidth - 200;
                    enemyIconY = 320;
                }
                else if (i == 3) // 第四名敌方
                {
                    enemyIconX = _graphics.PreferredBackBufferWidth - 100;
                    enemyIconY = 450;
                }
                else // 第五名敌方
                {
                    enemyIconX = _graphics.PreferredBackBufferWidth - 200;
                    enemyIconY = 580;
                }
                
                Rectangle enemyIconRect = new Rectangle(enemyIconX, enemyIconY, iconSize, iconSize);
                if (enemyIconRect.Contains(currentMouseState.Position))
                {
                    _selectedCharacter = _battleSystem.Enemies[i];
                    _isCharacterZooming = true;
                    _isCharacterZoomingOut = false;
                    _characterFadeAlpha = 1.0f; // 初始化淡出 alpha
                    _selectedSkillButton = null; // 重置选中的技能按钮
                    _scrollOffset = 0.0f; // 重置滚动条偏移
                    
                    // 计算相机偏移，使角色图标在缩放时位于屏幕中心
                    int windowWidth = _graphics.PreferredBackBufferWidth;
                    int windowHeight = _graphics.PreferredBackBufferHeight;
                    int iconCenterX = enemyIconX + iconSize / 2;
                    int iconCenterY = enemyIconY + iconSize / 2;
                    
                    // 计算角色详情界面中角色图标的中心位置
                    int leftThirdWidth = windowWidth / 3;
                    int detailIconSize = leftThirdWidth - 40;
                    int detailIconCenterX = 20 + detailIconSize / 2;
                    int detailIconCenterY = windowHeight / 2 - detailIconSize / 2 + detailIconSize / 2; // 图标下边缘与中线平齐
                    
                    // 计算初始相机偏移（A点）
                    // 我们需要将被点击的角色图标中心（A点）移动到屏幕中心
                    _characterCameraOffset = new Vector2(
                        windowWidth / 2 - iconCenterX,
                        windowHeight / 2 - iconCenterY
                    );
                    
                    // 计算目标相机偏移（B点）
                    // 我们需要将角色详情图标中心（B点）移动到屏幕中心
                    _characterTargetCameraOffset = new Vector2(
                        windowWidth / 2 - detailIconCenterX,
                        windowHeight / 2 - detailIconCenterY
                    );
                    return;
                }
            }
        }
    }
    
    private void CheckCharacterSkillButtonClick(MouseState currentMouseState)
    {
        // 确保_selectedCharacter不为null
        if (_selectedCharacter == null)
        {
            return;
        }
        
        if (currentMouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
        {
            int windowWidth = _graphics.PreferredBackBufferWidth;
            int windowHeight = _graphics.PreferredBackBufferHeight;
            int leftThirdWidth = windowWidth / 3;
            int buttonWidth = 100;
            int buttonHeight = 40;
            int spacing = 10;
            int detailX = leftThirdWidth + 20;
            int detailY = 40;
            int lineHeight = 40;
            int buttonStartX = detailX;
            int buttonStartY = detailY + lineHeight * 2;
            
            // 检查左下角返回提示区域
            string exitText = "点击此处或按Backspace回到上一级";
            SpriteFont exitFont = GetFontForText(exitText);
            Vector2 exitSize = exitFont.MeasureString(exitText);
            if (IsPointInRectangle(currentMouseState.X, currentMouseState.Y, 10, (int)(windowHeight - exitSize.Y - 10), (int)exitSize.X, (int)exitSize.Y))
            {
                _isCharacterZoomingOut = true;
                _isCharacterZooming = false;
                _characterFadeAlpha = 0.0f;
                return;
            }
            
            // 被动技能按钮
            if (IsPointInRectangle(currentMouseState.X, currentMouseState.Y, buttonStartX, buttonStartY, buttonWidth, buttonHeight))
            {
                _selectedSkillButton = "被动";
                _scrollOffset = 0.0f; // 重置滚动条偏移
            }
            // 技能1按钮
            else if (IsPointInRectangle(currentMouseState.X, currentMouseState.Y, buttonStartX + buttonWidth + spacing, buttonStartY, buttonWidth, buttonHeight))
            {
                _selectedSkillButton = "技能1";
                _scrollOffset = 0.0f; // 重置滚动条偏移
            }
            // 技能2按钮
            else if (IsPointInRectangle(currentMouseState.X, currentMouseState.Y, buttonStartX + 2 * (buttonWidth + spacing), buttonStartY, buttonWidth, buttonHeight))
            {
                _selectedSkillButton = "技能2";
                _scrollOffset = 0.0f; // 重置滚动条偏移
            }
            // 技能3按钮
            else if (IsPointInRectangle(currentMouseState.X, currentMouseState.Y, buttonStartX + 3 * (buttonWidth + spacing), buttonStartY, buttonWidth, buttonHeight))
            {
                _selectedSkillButton = "技能3";
                _scrollOffset = 0.0f; // 重置滚动条偏移
            }
            // 守备技能按钮
            else if (IsPointInRectangle(currentMouseState.X, currentMouseState.Y, buttonStartX + 4 * (buttonWidth + spacing), buttonStartY, buttonWidth, buttonHeight))
            {
                _selectedSkillButton = "守备技能";
                _scrollOffset = 0.0f; // 重置滚动条偏移
            }
            // 状态按钮
            else if (IsPointInRectangle(currentMouseState.X, currentMouseState.Y, buttonStartX + 5 * (buttonWidth + spacing), buttonStartY, buttonWidth, buttonHeight))
            {
                _selectedSkillButton = "状态";
                _scrollOffset = 0.0f; // 重置滚动条偏移
            }
        }
    }
    
    private bool _isDraggingScrollBar = false;
    
    // 计算内容总高度的辅助方法
    private int CalculateTotalContentHeight()
    {
        if (_selectedCharacter == null || _selectedSkillButton == null)
        {
            return 300;
        }
        
        int windowWidth = _graphics.PreferredBackBufferWidth;
        int leftThirdWidth = windowWidth / 3;
        int rightTwoThirdsWidth = windowWidth - leftThirdWidth;
        int totalContentHeight = 300;
        
        if (_selectedSkillButton == "被动")
        {
            string passiveName = _selectedCharacter.PassiveName ?? "默认被动";
            SpriteFont passiveNameFont = GetFontForText(passiveName);
            Vector2 passiveNameSize = passiveNameFont.MeasureString(passiveName);
            
            string passiveDescription = _selectedCharacter.PassiveSkill ?? "这是一个模板角色，没有被动技能";
            if (!passiveDescription.Contains("\n"))
            {
                passiveDescription = passiveDescription.Replace("[", "\n[");
                if (passiveDescription.StartsWith("\n"))
                {
                    passiveDescription = passiveDescription.Substring(1);
                }
            }
            SpriteFont passiveDescFont = GetFontForText(passiveDescription);
            int passiveHeight = CalculateTextHeight(passiveDescFont, passiveDescription, rightTwoThirdsWidth - 40, 24);
            
            totalContentHeight = (int)(passiveNameSize.Y * 1.5f) + 40 + passiveHeight + 75 + 30 + 85 + 30 + 75 + 30;
        }
        else if (_selectedSkillButton == "技能1")
        {
            BaseSkill skill1;
            if (_selectedCharacter is 夏侯惇)
            {
                skill1 = new TurnBasedRPG.Characters.Skills.夏侯惇.横斩();
            }
            else if (_selectedCharacter is TurnBasedRPG.Characters.Allies.曹仁)
            {
                skill1 = new TurnBasedRPG.Characters.Skills.曹仁.盾击();
            }
            else if (_selectedCharacter is TurnBasedRPG.Characters.Allies.司马懿)
            {
                skill1 = new TurnBasedRPG.Characters.Skills.司马懿.机先();
            }
            else if (_selectedCharacter is TurnBasedRPG.Characters.Allies.曹丕)
            {
                skill1 = new TurnBasedRPG.Characters.Skills.曹丕.魏室初锋();
            }
            else
            {
                skill1 = new CombatSkill1();
            }
            totalContentHeight = CalculateSkillDetailHeight(skill1, rightTwoThirdsWidth - 40);
        }
        else if (_selectedSkillButton == "技能2")
        {
            BaseSkill skill2;
            if (_selectedCharacter is 夏侯惇)
            {
                skill2 = new TurnBasedRPG.Characters.Skills.夏侯惇.拔矢啖睛();
            }
            else if (_selectedCharacter is TurnBasedRPG.Characters.Allies.曹仁)
            {
                skill2 = new TurnBasedRPG.Characters.Skills.曹仁.镇岳反攻();
            }
            else if (_selectedCharacter is TurnBasedRPG.Characters.Allies.司马懿)
            {
                skill2 = new TurnBasedRPG.Characters.Skills.司马懿.汲魂();
            }
            else if (_selectedCharacter is TurnBasedRPG.Characters.Allies.曹丕)
            {
                skill2 = new TurnBasedRPG.Characters.Skills.曹丕.定策安邦();
            }
            else
            {
                skill2 = new CombatSkill2();
            }
            totalContentHeight = CalculateSkillDetailHeight(skill2, rightTwoThirdsWidth - 40);
        }
        else if (_selectedSkillButton == "技能3")
        {
            BaseSkill skill3;
            if (_selectedCharacter is 夏侯惇)
            {
                skill3 = new TurnBasedRPG.Characters.Skills.夏侯惇.铁壁战吼();
            }
            else if (_selectedCharacter is TurnBasedRPG.Characters.Allies.曹仁)
            {
                skill3 = new TurnBasedRPG.Characters.Skills.曹仁.御甲鸣镝();
            }
            else if (_selectedCharacter is TurnBasedRPG.Characters.Allies.司马懿)
            {
                skill3 = new TurnBasedRPG.Characters.Skills.司马懿.窃国者侯();
            }
            else if (_selectedCharacter is TurnBasedRPG.Characters.Allies.曹丕)
            {
                skill3 = new TurnBasedRPG.Characters.Skills.曹丕.受禅代汉();
            }
            else
            {
                skill3 = new CombatSkill3();
            }
            totalContentHeight = CalculateSkillDetailHeight(skill3, rightTwoThirdsWidth - 40);
        }
        else if (_selectedSkillButton == "守备技能")
        {
            totalContentHeight = 0;
            int tempCurrentY = 0;
            
            if (_selectedCharacter.DodgeSkill != null)
            {
                BaseSkill tempSkill = _selectedCharacter.DodgeSkill;
                totalContentHeight += 80 + CalculateSkillDetailHeight(tempSkill, rightTwoThirdsWidth - 40);
                tempCurrentY = totalContentHeight;
            }
            
            if (_selectedCharacter.DefendSkill != null)
            {
                BaseSkill defendSkill;
                if (_selectedCharacter is TurnBasedRPG.Characters.Allies.曹丕)
                {
                    defendSkill = new TurnBasedRPG.Characters.Skills.曹丕.御极守成();
                }
                else
                {
                    defendSkill = _selectedCharacter.DefendSkill;
                }
                totalContentHeight += 80 + CalculateSkillDetailHeight(defendSkill, rightTwoThirdsWidth - 40);
                tempCurrentY = totalContentHeight;
            }
            
            if (_selectedCharacter.HealSkill != null)
            {
                BaseSkill tempSkill = _selectedCharacter.HealSkill;
                totalContentHeight += 80 + CalculateSkillDetailHeight(tempSkill, rightTwoThirdsWidth - 40);
                tempCurrentY = totalContentHeight;
            }
            
            if (_selectedCharacter.Name != "曹仁" && _selectedCharacter.Name != "司马懿" && _selectedCharacter.Name != "曹丕" && _selectedCharacter.CounterSkill != null)
            {
                BaseSkill tempSkill = _selectedCharacter.CounterSkill;
                totalContentHeight += 80 + CalculateSkillDetailHeight(tempSkill, rightTwoThirdsWidth - 40);
                tempCurrentY = totalContentHeight;
            }
            
            if (_selectedCharacter is 曹仁)
            {
                BaseSkill moshouXufengSkill = new TurnBasedRPG.Characters.Skills.曹仁.默守蓄锋();
                totalContentHeight += 80 + CalculateSkillDetailHeight(moshouXufengSkill, rightTwoThirdsWidth - 40);
                tempCurrentY = totalContentHeight;
            }
            
            if (_selectedCharacter is 司马懿)
            {
                BaseSkill langguSkill = new TurnBasedRPG.Characters.Skills.司马懿.狼顾();
                totalContentHeight += 80 + CalculateSkillDetailHeight(langguSkill, rightTwoThirdsWidth - 40);
                tempCurrentY = totalContentHeight;
            }
            
            if (_selectedCharacter is TurnBasedRPG.Characters.Allies.曹丕)
            {
                BaseSkill zhihengSkill = new TurnBasedRPG.Characters.Skills.曹丕.制衡();
                int zhihengHeight = CalculateSkillDetailHeight(zhihengSkill, rightTwoThirdsWidth - 40);
                totalContentHeight += 80 + zhihengHeight;
                tempCurrentY = totalContentHeight;
                
                BaseSkill weiWuHongLiuSkill = new TurnBasedRPG.Characters.Skills.曹丕.魏武洪流();
                totalContentHeight += 80 + CalculateSkillDetailHeight(weiWuHongLiuSkill, rightTwoThirdsWidth - 40);
            }
        }
        else if (_selectedSkillButton == "状态")
        {
            totalContentHeight = 0;
            List<Status> tempStatuses = new List<Status>();
            
            if (_battleSystem != null && _battleSystem.BuffHandler != null)
            {
                var baseBuffs = _battleSystem.BuffHandler.GetBuffs(_selectedCharacter);
                foreach (var baseBuff in baseBuffs)
                {
                    string description = baseBuff.Description.Replace("x%", $"{baseBuff.Strength * 10}%");
                    tempStatuses.Add(new Status(baseBuff.Name, description, baseBuff.IconColor, baseBuff.Strength, baseBuff.RemainingTurns));
                }
            }
            else
            {
                if (_selectedCharacter.Name == "夏侯惇")
                {
                    tempStatuses.Add(new Status("不屈", "受到的伤害降低10%", Color.LightBlue, 1, 1));
                }
            }
            
            int tempY = 0;
            foreach (Status status in tempStatuses)
            {
                int statusIconSize = 60;
                int stateTotalHeight = statusIconSize;
                
                string statusDesc = status.Description;
                if (!statusDesc.Contains("\n"))
                {
                    statusDesc = statusDesc.Replace("[", "\n[");
                    if (statusDesc.StartsWith("\n"))
                    {
                        statusDesc = statusDesc.Substring(1);
                    }
                }
                SpriteFont descFont = GetFontForText(statusDesc);
                int descHeight = CalculateTextHeight(descFont, statusDesc, rightTwoThirdsWidth - (statusIconSize + 20) - 40, 24);
                stateTotalHeight = Math.Max(stateTotalHeight, 30 + descHeight);
                
                tempY += 80 + stateTotalHeight;
                totalContentHeight = tempY;
            }
        }
        
        return totalContentHeight;
    }
    
    // 计算技能详情高度的辅助方法
    private int CalculateSkillDetailHeight(BaseSkill skill, int width)
    {
        if (skill == null)
        {
            return 0;
        }
        
        int totalHeight = 0;
        int lineHeight = 30;
        
        // 技能图标和名称
        int iconSize = 40;
        totalHeight = Math.Max(totalHeight, iconSize);
        
        // 技能基础点数 + 硬币点数
        totalHeight = Math.Max(totalHeight, lineHeight + 20 + 10);
        
        // 技能的攻击类型与伤害类型
        int typeTextY = lineHeight * 2;
        totalHeight = Math.Max(totalHeight, typeTextY + lineHeight);
        
        // 技能的额外效果
        string effectText = "额外效果:\n" + (skill.ExtraEffects ?? "");
        if (!effectText.Contains("\n"))
        {
            effectText = effectText.Replace("[", "\n[");
            if (effectText.StartsWith("\n"))
            {
                effectText = effectText.Substring(1);
            }
        }
        SpriteFont effectFont = GetFontForText(effectText);
        int effectHeight = CalculateTextHeight(effectFont, effectText, width, 24);
        
        totalHeight = Math.Max(totalHeight, typeTextY + lineHeight + effectHeight);
        
        return totalHeight;
    }
    
    private void CheckScrollBarClick(MouseState currentMouseState)
    {
        int windowWidth = _graphics.PreferredBackBufferWidth;
        int windowHeight = _graphics.PreferredBackBufferHeight;
        int leftThirdWidth = windowWidth / 3;
        int detailX = leftThirdWidth + 20;
        int buttonHeight = 40;
        int buttonStartX = detailX;
        int buttonStartY = 40 + 40 * 2; // 与DrawCharacterDetail方法中的buttonStartY计算方式一致
        int skillDetailY = buttonStartY + buttonHeight + 20;
        int skillDetailHeight = windowHeight - skillDetailY - 50;
        int scrollBarWidth = 8;
        int scrollBarX = detailX + (windowWidth - leftThirdWidth) - 40 - scrollBarWidth;
        int scrollBarHeight = skillDetailHeight;
        int scrollBarY = skillDetailY;
        
        int totalContentHeight = CalculateTotalContentHeight();
        float maxScrollOffset = Math.Max(0, totalContentHeight - skillDetailHeight);
        
        // 计算滚动条滑块位置和大小
        float scrollRatio = maxScrollOffset > 0 ? _scrollOffset / maxScrollOffset : 0;
        int sliderHeight = totalContentHeight > 0 ? (int)(scrollBarHeight * (skillDetailHeight / (float)totalContentHeight)) : scrollBarHeight;
        sliderHeight = Math.Max(20, sliderHeight); // 滑块最小高度20像素
        int sliderY = skillDetailY + (int)(scrollRatio * (scrollBarHeight - sliderHeight));
        Rectangle sliderRect = new Rectangle(scrollBarX, sliderY, scrollBarWidth, sliderHeight);
        
        // 处理鼠标按下事件
        if (currentMouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
        {
            // 检查是否点击了滑块
            if (sliderRect.Contains(currentMouseState.Position))
            {
                _isDraggingScrollBar = true;
            }
            // 检查是否点击了滚动条背景
            else
            {
                Rectangle scrollBarRect = new Rectangle(scrollBarX, scrollBarY, scrollBarWidth, scrollBarHeight);
                if (scrollBarRect.Contains(currentMouseState.Position))
                {
                    // 计算滚动条位置对应的滚动偏移
                    float clickScrollRatio = (currentMouseState.Y - scrollBarY) / (float)scrollBarHeight;
                    _scrollOffset = clickScrollRatio * maxScrollOffset;
                    // 限制滚动范围
                    _scrollOffset = Math.Max(0, Math.Min(maxScrollOffset, _scrollOffset));
                }
            }
        }
        // 处理鼠标释放事件
        else if (currentMouseState.LeftButton == ButtonState.Released && _previousMouseState.LeftButton == ButtonState.Pressed)
        {
            _isDraggingScrollBar = false;
        }
        // 处理鼠标拖动事件
        else if (currentMouseState.LeftButton == ButtonState.Pressed && _isDraggingScrollBar)
        {
            // 计算拖动位置对应的滚动偏移
            float dragScrollRatio = (currentMouseState.Y - scrollBarY) / (float)scrollBarHeight;
            _scrollOffset = dragScrollRatio * maxScrollOffset;
            // 限制滚动范围
            _scrollOffset = Math.Max(0, Math.Min(maxScrollOffset, _scrollOffset));
        }
    }
    
    private void CheckEncyclopediaClick(MouseState currentMouseState, KeyboardState currentKeyboardState)
    {
        int windowWidth = _graphics.PreferredBackBufferWidth;
        int windowHeight = _graphics.PreferredBackBufferHeight;
        
        // 检查Backspace键
        if (IsKeyPressed(Keys.Back, currentKeyboardState))
        {
            ReturnToMainMenuFromEncyclopedia();
            return;
        }
        
        // 处理鼠标按下事件
        if (currentMouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
        {
            // 检查关闭按钮
            int closeButtonSize = 40;
            int closeButtonX = windowWidth - closeButtonSize - 20;
            int closeButtonY = 20;
            if (IsPointInRectangle(currentMouseState.X, currentMouseState.Y, closeButtonX, closeButtonY, closeButtonSize, closeButtonSize))
            {
                ReturnToMainMenuFromEncyclopedia();
                return;
            }
            
            // 检查标题按钮点击
            int buttonWidth = 150;
            int buttonHeight = 45;
            int buttonSpacing = 10;
            int buttonStartX = 20;
            int buttonStartY = 80;
            
            for (int i = 0; i < _encyclopediaTitles.Count; i++)
            {
                int buttonX = buttonStartX;
                int buttonY = buttonStartY + i * (buttonHeight + buttonSpacing);
                
                if (IsPointInRectangle(currentMouseState.X, currentMouseState.Y, buttonX, buttonY, buttonWidth, buttonHeight))
                {
                    _selectedEncyclopediaTitle = _encyclopediaTitles[i];
                    _encyclopediaScrollOffset = 0.0f; // 重置滚动条
                    return;
                }
            }
            
            // 检查滚动条点击
            if (_selectedEncyclopediaTitle != null)
            {
                int contentX = buttonStartX + buttonWidth + 30;
                int contentY = 80;
                int contentWidth = windowWidth - contentX - 40;
                int textStartY = contentY + 60;
                int textHeight = (windowHeight - contentY - 40) - 80;
                int scrollBarWidth = 10;
                int scrollBarX = contentX + contentWidth - scrollBarWidth;
                int scrollBarY = textStartY;
                int scrollBarHeight = textHeight;
                
                // 计算内容总高度
                string contentText = _encyclopediaContent.ContainsKey(_selectedEncyclopediaTitle) 
                    ? _encyclopediaContent[_selectedEncyclopediaTitle] 
                    : "暂无内容";
                SpriteFont textFont = IsChineseText(contentText) ? _chineseFont : _font;
                int totalContentHeight = CalculateTextHeight(textFont, contentText, contentWidth - 40, 24);
                
                float maxScrollOffset = Math.Max(0, totalContentHeight - textHeight);
                float scrollRatio = maxScrollOffset > 0 ? _encyclopediaScrollOffset / maxScrollOffset : 0;
                int sliderHeight = totalContentHeight > 0 ? (int)(scrollBarHeight * (textHeight / (float)Math.Max(textHeight, totalContentHeight))) : scrollBarHeight;
                sliderHeight = Math.Max(20, sliderHeight);
                int sliderY = scrollBarY + (int)(scrollRatio * (scrollBarHeight - sliderHeight));
                Rectangle sliderRect = new Rectangle(scrollBarX, sliderY, scrollBarWidth, sliderHeight);
                
                // 检查是否点击了滑块
                if (sliderRect.Contains(currentMouseState.Position))
                {
                    _isDraggingEncyclopediaScrollBar = true;
                }
                // 检查是否点击了滚动条背景
                else
                {
                    Rectangle scrollBarRect = new Rectangle(scrollBarX, scrollBarY, scrollBarWidth, scrollBarHeight);
                    if (scrollBarRect.Contains(currentMouseState.Position))
                    {
                        // 计算滚动条位置对应的滚动偏移
                        float clickScrollRatio = (currentMouseState.Y - scrollBarY) / (float)scrollBarHeight;
                        _encyclopediaScrollOffset = clickScrollRatio * maxScrollOffset;
                        _encyclopediaScrollOffset = Math.Max(0, Math.Min(maxScrollOffset, _encyclopediaScrollOffset));
                    }
                }
            }
        }
        // 处理鼠标释放事件
        else if (currentMouseState.LeftButton == ButtonState.Released && _previousMouseState.LeftButton == ButtonState.Pressed)
        {
            _isDraggingEncyclopediaScrollBar = false;
        }
        // 处理鼠标拖动事件
        else if (currentMouseState.LeftButton == ButtonState.Pressed && _isDraggingEncyclopediaScrollBar && _selectedEncyclopediaTitle != null)
        {
            int contentX = 20 + 150 + 30;
            int contentY = 80;
            int contentWidth = windowWidth - contentX - 40;
            int textStartY = contentY + 60;
            int textHeight = (windowHeight - contentY - 40) - 80;
            int scrollBarWidth = 10;
            int scrollBarX = contentX + contentWidth - scrollBarWidth;
            int scrollBarY = textStartY;
            int scrollBarHeight = textHeight;
            
            // 计算内容总高度
            string contentText = _encyclopediaContent.ContainsKey(_selectedEncyclopediaTitle) 
                ? _encyclopediaContent[_selectedEncyclopediaTitle] 
                : "暂无内容";
            SpriteFont textFont = IsChineseText(contentText) ? _chineseFont : _font;
            int totalContentHeight = CalculateTextHeight(textFont, contentText, contentWidth - 40, 24);
            
            float maxScrollOffset = Math.Max(0, totalContentHeight - textHeight);
            float dragScrollRatio = (currentMouseState.Y - scrollBarY) / (float)scrollBarHeight;
            _encyclopediaScrollOffset = dragScrollRatio * maxScrollOffset;
            _encyclopediaScrollOffset = Math.Max(0, Math.Min(maxScrollOffset, _encyclopediaScrollOffset));
        }
    }
    
    private void ReturnToMainMenuFromEncyclopedia()
    {
        _currentGameState = GameState.MainMenu;
        _selectedEncyclopediaTitle = null;
        _encyclopediaScrollOffset = 0.0f;
        _isDraggingEncyclopediaScrollBar = false;
        _mainTitle = new MainTitle(_spriteBatch, _font, _chineseFont, _pixel);
    }
    
    public void ReturnToMainMenuFromTutorial()
    {
        if (_tutorialManager != null)
        {
            _tutorialManager.ExitTutorial();
        }
        _currentGameState = GameState.MainMenu;
        _mainTitle = new MainTitle(_spriteBatch, _font, _chineseFont, _pixel);
    }

    
    private void UpdateCharacterZoom()
    {
        if (_isCharacterZooming)
        {
            // 镜头拉近动画
            if (_characterZoomScale < 3.0f)
            {
                _characterZoomScale += 0.08f; // 减慢缩放速度，使动画更明显
                _characterFadeAlpha -= 0.03f; // 减慢淡出速度
                if (_characterFadeAlpha < 0.0f)
                    _characterFadeAlpha = 0.0f;
                
                // 平滑过渡相机偏移，确保缩放中心逐渐移动
                _characterCameraOffset = Vector2.Lerp(_characterCameraOffset, _characterTargetCameraOffset, 0.1f);
            }
            else
            {
                _isCharacterZooming = false;
                _characterFadeAlpha = 0.0f;
                _isCharacterDetailMode = true; // 动画完成后才显示角色详情界面
            }
        }
        else if (_isCharacterZoomingOut)
        {
            // 镜头拉远动画
            if (_characterZoomScale > 1.0f)
            {
                _characterZoomScale -= 0.08f; // 减慢缩放速度，使动画更明显
                _characterFadeAlpha += 0.03f; // 减慢淡入速度
                if (_characterFadeAlpha > 1.0f)
                    _characterFadeAlpha = 1.0f;
                
                // 平滑过渡相机偏移到原点
                _characterCameraOffset = Vector2.Lerp(_characterCameraOffset, Vector2.Zero, 0.1f);
            }
            else
            {
                _isCharacterZoomingOut = false;
                _isCharacterDetailMode = false;
                _selectedCharacter = null;
                _selectedSkillButton = null;
                _characterZoomScale = 1.0f;
                _characterFadeAlpha = 1.0f;
                _characterCameraOffset = Vector2.Zero;
                _scrollOffset = 0.0f;
            }
        }
    }
    
    private void DrawCharacterDetail()
    {
        // 确保_selectedCharacter不为null
        if (_selectedCharacter == null)
        {
            return;
        }
        
        int windowWidth = _graphics.PreferredBackBufferWidth;
        int windowHeight = _graphics.PreferredBackBufferHeight;
        int leftThirdWidth = windowWidth / 3;
        int rightTwoThirdsWidth = windowWidth - leftThirdWidth;
        int middleHeight = windowHeight / 2;
        
        // 计算透明度
        float detailAlpha = 1.0f;
        if (!_isCharacterDetailMode)
        {
            detailAlpha = 1.0f - _characterFadeAlpha;
            if (detailAlpha < 0.0f) detailAlpha = 0.0f;
            if (detailAlpha > 1.0f) detailAlpha = 1.0f;
        }
        Color whiteWithAlpha = new Color(1.0f, 1.0f, 1.0f, detailAlpha);
        
        // 绘制角色图标
        int iconSize = leftThirdWidth - 40;
        int iconX = 20;
        int iconY = middleHeight - iconSize;
        
        Color characterColor = _selectedCharacter.Name == "英雄" ? Color.Green : Color.Red;
        _spriteBatch.Draw(_pixel, new Rectangle(iconX, iconY, iconSize, iconSize), characterColor * detailAlpha);
        
        // 绘制角色详情
        int detailX = leftThirdWidth + 20;
        int detailY = 40;
        int lineHeight = 40;
        
        // 角色名称（大字号）
        string characterName = _selectedCharacter.Name;
        SpriteFont nameFont = GetFontForText(characterName);
        Vector2 nameSize = nameFont.MeasureString(characterName);
        _spriteBatch.DrawString(nameFont, characterName, new Vector2(detailX, detailY), whiteWithAlpha, 0, Vector2.Zero, 1.5f, SpriteEffects.None, 0);
        
        // 角色等级（与角色名称字号一致）
        string levelText = $"Lv.{_selectedCharacter.Level}";
        SpriteFont levelFont = GetFontForText(levelText);
        // 向右平移100像素，并向上垂直偏移以确保中线对齐
        _spriteBatch.DrawString(levelFont, levelText, new Vector2(detailX + nameSize.X + 130, detailY - 5), whiteWithAlpha, 0, Vector2.Zero, 1.5f, SpriteEffects.None, 0);
        
        // 士气值（小字号）
        string moraleText = $"士气: {_selectedCharacter.Morale}";
        SpriteFont moraleFont = GetFontForText(moraleText);
        Vector2 moraleSize = moraleFont.MeasureString(moraleText);
        // 向右平移200像素
        _spriteBatch.DrawString(moraleFont, moraleText, new Vector2(detailX + nameSize.X + 320, detailY + 10), whiteWithAlpha, 0, Vector2.Zero, 0.8f, SpriteEffects.None, 0);
        
        // 血条
        int healthBarWidth = rightTwoThirdsWidth - 40;
        int healthBarHeight = 20;
        int healthBarX = detailX;
        int healthBarY = detailY + lineHeight;
        
        // 血条背景
        _spriteBatch.Draw(_pixel, new Rectangle(healthBarX, healthBarY, healthBarWidth, healthBarHeight), Color.Gray * detailAlpha);
        
        // 血条填充
        float healthRatio = (float)_selectedCharacter.CurrentHealth / _selectedCharacter.MaxHealth;
        int filledWidth = (int)(healthBarWidth * healthRatio);
        _spriteBatch.Draw(_pixel, new Rectangle(healthBarX, healthBarY, filledWidth, healthBarHeight), Color.Red * detailAlpha);
        
        // 血量值
        string healthText = $"{_selectedCharacter.CurrentHealth}/{_selectedCharacter.MaxHealth}";
        SpriteFont healthFont = GetFontForText(healthText);
        Vector2 healthSize = healthFont.MeasureString(healthText);
        _spriteBatch.DrawString(healthFont, healthText, new Vector2(healthBarX + healthBarWidth / 2 - healthSize.X / 2, healthBarY + healthBarHeight / 2 - healthSize.Y / 2), whiteWithAlpha, 0, Vector2.Zero, 0.8f, SpriteEffects.None, 0);
        
        // 技能按钮
        int buttonWidth = 100;
        int buttonHeight = 40;
        int spacing = 10;
        int buttonStartX = detailX;
        int buttonStartY = detailY + lineHeight * 2;
        
        // 被动技能按钮
        _spriteBatch.Draw(_pixel, new Rectangle(buttonStartX, buttonStartY, buttonWidth, buttonHeight), Color.Gray * detailAlpha);
        string passiveText = "被动";
        SpriteFont passiveFont = GetFontForText(passiveText);
        Vector2 passiveSize = passiveFont.MeasureString(passiveText);
        _spriteBatch.DrawString(passiveFont, passiveText, new Vector2(buttonStartX + buttonWidth / 2 - passiveSize.X / 2, buttonStartY + buttonHeight / 2 - passiveSize.Y / 2), whiteWithAlpha, 0, Vector2.Zero, 0.8f, SpriteEffects.None, 0);
        
        // 技能1按钮
        _spriteBatch.Draw(_pixel, new Rectangle(buttonStartX + buttonWidth + spacing, buttonStartY, buttonWidth, buttonHeight), Color.Gray * detailAlpha);
        string skill1Text = "技能1";
        SpriteFont skill1Font = GetFontForText(skill1Text);
        Vector2 skill1Size = skill1Font.MeasureString(skill1Text);
        _spriteBatch.DrawString(skill1Font, skill1Text, new Vector2(buttonStartX + buttonWidth + spacing + buttonWidth / 2 - skill1Size.X / 2, buttonStartY + buttonHeight / 2 - skill1Size.Y / 2), whiteWithAlpha, 0, Vector2.Zero, 0.8f, SpriteEffects.None, 0);
        
        // 技能2按钮
        _spriteBatch.Draw(_pixel, new Rectangle(buttonStartX + 2 * (buttonWidth + spacing), buttonStartY, buttonWidth, buttonHeight), Color.Gray * detailAlpha);
        string skill2Text = "技能2";
        SpriteFont skill2Font = GetFontForText(skill2Text);
        Vector2 skill2Size = skill2Font.MeasureString(skill2Text);
        _spriteBatch.DrawString(skill2Font, skill2Text, new Vector2(buttonStartX + 2 * (buttonWidth + spacing) + buttonWidth / 2 - skill2Size.X / 2, buttonStartY + buttonHeight / 2 - skill2Size.Y / 2), whiteWithAlpha, 0, Vector2.Zero, 0.8f, SpriteEffects.None, 0);
        
        // 技能3按钮
        _spriteBatch.Draw(_pixel, new Rectangle(buttonStartX + 3 * (buttonWidth + spacing), buttonStartY, buttonWidth, buttonHeight), Color.Gray * detailAlpha);
        string skill3Text = "技能3";
        SpriteFont skill3Font = GetFontForText(skill3Text);
        Vector2 skill3Size = skill3Font.MeasureString(skill3Text);
        _spriteBatch.DrawString(skill3Font, skill3Text, new Vector2(buttonStartX + 3 * (buttonWidth + spacing) + buttonWidth / 2 - skill3Size.X / 2, buttonStartY + buttonHeight / 2 - skill3Size.Y / 2), whiteWithAlpha, 0, Vector2.Zero, 0.8f, SpriteEffects.None, 0);
        
        // 守备技能按钮
        _spriteBatch.Draw(_pixel, new Rectangle(buttonStartX + 4 * (buttonWidth + spacing), buttonStartY, buttonWidth, buttonHeight), Color.Gray * detailAlpha);
        string defenseSkillText = "守备技能";
        SpriteFont defenseSkillFont = GetFontForText(defenseSkillText);
        Vector2 defenseSkillSize = defenseSkillFont.MeasureString(defenseSkillText);
        _spriteBatch.DrawString(defenseSkillFont, defenseSkillText, new Vector2(buttonStartX + 4 * (buttonWidth + spacing) + buttonWidth / 2 - defenseSkillSize.X / 2, buttonStartY + buttonHeight / 2 - defenseSkillSize.Y / 2), whiteWithAlpha, 0, Vector2.Zero, 0.8f, SpriteEffects.None, 0);
        
        // 状态按钮
        _spriteBatch.Draw(_pixel, new Rectangle(buttonStartX + 5 * (buttonWidth + spacing), buttonStartY, buttonWidth, buttonHeight), Color.Gray * detailAlpha);
        string statusText = "状态";
        SpriteFont statusFont = GetFontForText(statusText);
        Vector2 statusSize = statusFont.MeasureString(statusText);
        _spriteBatch.DrawString(statusFont, statusText, new Vector2(buttonStartX + 5 * (buttonWidth + spacing) + buttonWidth / 2 - statusSize.X / 2, buttonStartY + buttonHeight / 2 - statusSize.Y / 2), whiteWithAlpha, 0, Vector2.Zero, 0.8f, SpriteEffects.None, 0);
        
        // 绘制技能详情（带滚动条）
        int skillDetailY = buttonStartY + buttonHeight + 20;
        int skillDetailHeight = windowHeight - skillDetailY - 50;
        
        // 绘制滚动区域背景
        _spriteBatch.Draw(_pixel, new Rectangle(detailX, skillDetailY, rightTwoThirdsWidth - 40, skillDetailHeight), Color.DarkGray * detailAlpha);
        
        // 绘制技能详情内容（根据选中的技能按钮），使用裁剪区域
        Rectangle scissorRect = new Rectangle(detailX, skillDetailY, rightTwoThirdsWidth - 40, skillDetailHeight);
        Rectangle originalScissorRect = GraphicsDevice.ScissorRectangle;
        GraphicsDevice.ScissorRectangle = scissorRect;
        
        // 开始一个新的spriteBatch批次，启用裁剪
        _spriteBatch.End();
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, new RasterizerState { ScissorTestEnable = true });
        
        // 更新角色buff效果，计算最终攻击等级和防御等级
        if (_battleSystem != null && _battleSystem.BuffHandler != null)
        {
            _selectedCharacter.UpdateBuffs(_battleSystem.BuffHandler);
        }
        else
        {
            _selectedCharacter.UpdateBuffs();
        }
        
        int totalContentHeight = 300; // 默认内容高度
        
        if (_selectedSkillButton == "被动")
        {
            try
            {
                // 先计算内容总高度
                string passiveName = _selectedCharacter.PassiveName ?? "默认被动";
                SpriteFont passiveNameFont = GetFontForText(passiveName);
                Vector2 passiveNameSize = passiveNameFont.MeasureString(passiveName);
                
                string passiveDescription = _selectedCharacter.PassiveSkill ?? "这是一个模板角色，没有被动技能";
                if (!passiveDescription.Contains("\n"))
                {
                    passiveDescription = passiveDescription.Replace("[", "\n[");
                    if (passiveDescription.StartsWith("\n"))
                    {
                        passiveDescription = passiveDescription.Substring(1);
                    }
                }
                SpriteFont passiveDescFont = GetFontForText(passiveDescription);
                int passiveHeight = CalculateTextHeight(passiveDescFont, passiveDescription, rightTwoThirdsWidth - 40, 24);
                
                // 计算完整的内容总高度
                totalContentHeight = (int)(passiveNameSize.Y * 1.5f) + 40 + passiveHeight + 75 + 30 + 85 + 30 + 75 + 30 + 75 + 30;
                
                // 绘制被动技能详情
                int currentY = skillDetailY - (int)_scrollOffset;
                
                // 被动技能名称（大字号）
                _spriteBatch.DrawString(passiveNameFont, passiveName, new Vector2(detailX, currentY), whiteWithAlpha, 0, Vector2.Zero, 1.5f, SpriteEffects.None, 0);
                
                // 被动技能描述（一般字号，自动换行）
                // 使用带有条件格式的自动换行绘制（更好的中文换行支持）
                DrawMultiLineTextWithConditionFormat(passiveDescFont, passiveDescription, new Vector2(detailX, currentY + 40), whiteWithAlpha, rightTwoThirdsWidth - 40);
                
                // 绘制抗性信息
                int resistanceY = currentY + 40 + passiveHeight + 75; // 被动技能描述下方75像素
                
                // 标题字体（稍大）
                SpriteFont titleFont = GetFontForText("攻击方式易损：");
                // 内容字体
                SpriteFont contentFont = GetFontForText("-斩击1.0");
                
                // 攻击方式易损（大字号标题）
                _spriteBatch.DrawString(titleFont, "攻击方式易损：", new Vector2(detailX, resistanceY), whiteWithAlpha, 0, Vector2.Zero, 1.2f, SpriteEffects.None, 0);
                string slashText = $"-斩击{_selectedCharacter.SlashVulnerability.ToString("0.0")}";
                string bluntText = $"-钝击{_selectedCharacter.BluntVulnerability.ToString("0.0")}";
                string pierceText = $"-穿刺{_selectedCharacter.PierceVulnerability.ToString("0.0")}";
                string spellText = $"-法术{_selectedCharacter.SpellVulnerability.ToString("0.0")}";
                
                _spriteBatch.DrawString(contentFont, slashText, new Vector2(detailX, resistanceY + 30), whiteWithAlpha);
                _spriteBatch.DrawString(contentFont, bluntText, new Vector2(detailX + 120, resistanceY + 30), whiteWithAlpha);
                _spriteBatch.DrawString(contentFont, pierceText, new Vector2(detailX + 240, resistanceY + 30), whiteWithAlpha);
                _spriteBatch.DrawString(contentFont, spellText, new Vector2(detailX + 360, resistanceY + 30), whiteWithAlpha);
                
                // 伤害种类易损（大字号标题）
                _spriteBatch.DrawString(titleFont, "伤害种类易损：", new Vector2(detailX, resistanceY + 85), whiteWithAlpha, 0, Vector2.Zero, 1.2f, SpriteEffects.None, 0);
                string physicalText = $"-物理{_selectedCharacter.PhysicalVulnerability.ToString("0.0")}";
                string magicText = $"-魔法{_selectedCharacter.MagicVulnerability.ToString("0.0")}";
                string trueText = $"-真实{_selectedCharacter.TrueVulnerability.ToString("0.0")}";
                
                _spriteBatch.DrawString(contentFont, physicalText, new Vector2(detailX, resistanceY + 115), whiteWithAlpha);
                _spriteBatch.DrawString(contentFont, magicText, new Vector2(detailX + 120, resistanceY + 115), whiteWithAlpha);
                _spriteBatch.DrawString(contentFont, trueText, new Vector2(detailX + 240, resistanceY + 115), whiteWithAlpha);
                
                // 显示最终攻击等级与最终防御等级
                int levelY = resistanceY + 115 + 75; // 伤害种类易损下方75像素
                string finalLevelText;
                if (_selectedCharacter.HasWeiWuGuZhen && !_selectedCharacter.HasWuDaoDuZun)
                {
                    finalLevelText = $"攻击等级（已停用）：{_selectedCharacter.FinalAttackLevel} 防御等级：{_selectedCharacter.FinalDefenseLevel}";
                }
                else
                {
                    finalLevelText = $"攻击等级：{_selectedCharacter.FinalAttackLevel} 防御等级：{_selectedCharacter.FinalDefenseLevel}";
                }
                SpriteFont finalLevelFont = GetFontForText(finalLevelText);
                _spriteBatch.DrawString(finalLevelFont, finalLevelText, new Vector2(detailX, levelY), whiteWithAlpha, 0, Vector2.Zero, 1.2f, SpriteEffects.None, 0);
                
                // 显示速度范围
                int speedY = levelY + 75; // 攻击等级/防御等级下方75像素
                string speedText = $"速度范围：{_selectedCharacter.FinalMinSpeed}-{_selectedCharacter.FinalMaxSpeed}";
                SpriteFont speedFont = GetFontForText(speedText);
                _spriteBatch.DrawString(speedFont, speedText, new Vector2(detailX, speedY), whiteWithAlpha, 0, Vector2.Zero, 1.2f, SpriteEffects.None, 0);
            }
            catch (Exception)
            {
                // 发生异常时，记录错误但不使游戏闪退
            }
        }
        else if (_selectedSkillButton == "技能1")
        {
            // 创建并计算技能1的数值
            BaseSkill skill1;
            if (_selectedCharacter is 夏侯惇)
            {
                skill1 = new TurnBasedRPG.Characters.Skills.夏侯惇.横斩();
            }
            else if (_selectedCharacter is TurnBasedRPG.Characters.Allies.曹仁)
            {
                skill1 = new TurnBasedRPG.Characters.Skills.曹仁.盾击();
            }
            else if (_selectedCharacter is TurnBasedRPG.Characters.Allies.司马懿)
            {
                skill1 = new TurnBasedRPG.Characters.Skills.司马懿.机先();
            }
            else if (_selectedCharacter is TurnBasedRPG.Characters.Allies.曹丕)
            {
                skill1 = new TurnBasedRPG.Characters.Skills.曹丕.魏室初锋();
            }
            else if (_selectedCharacter is TurnBasedRPG.Characters.Allies.曹操)
            {
                skill1 = new TurnBasedRPG.Characters.Skills.曹操.煮酒论英();
            }
            else if (_selectedCharacter is TurnBasedRPG.Characters.Allies.张辽)
            {
                skill1 = new TurnBasedRPG.Characters.Skills.张辽.霜戟();
            }
            else
            {
                skill1 = new CombatSkill1();
            }
            _selectedCharacter.CalculateSkillValues(skill1);
            totalContentHeight = DrawSkillDetailInCharacter(skill1, detailX, skillDetailY, rightTwoThirdsWidth - 40, skillDetailHeight, detailAlpha);
        }
        else if (_selectedSkillButton == "技能2")
        {
            // 创建并计算技能2的数值
            BaseSkill skill2;
            if (_selectedCharacter is 夏侯惇)
            {
                skill2 = new TurnBasedRPG.Characters.Skills.夏侯惇.拔矢啖睛();
            }
            else if (_selectedCharacter is TurnBasedRPG.Characters.Allies.曹仁)
            {
                skill2 = new TurnBasedRPG.Characters.Skills.曹仁.镇岳反攻();
            }
            else if (_selectedCharacter is TurnBasedRPG.Characters.Allies.司马懿)
            {
                skill2 = new TurnBasedRPG.Characters.Skills.司马懿.汲魂();
            }
            else if (_selectedCharacter is TurnBasedRPG.Characters.Allies.曹丕)
            {
                skill2 = new TurnBasedRPG.Characters.Skills.曹丕.定策安邦();
            }
            else if (_selectedCharacter is TurnBasedRPG.Characters.Allies.曹操)
            {
                skill2 = new TurnBasedRPG.Characters.Skills.曹操.屯田固本();
            }
            else if (_selectedCharacter is TurnBasedRPG.Characters.Allies.张辽)
            {
                skill2 = new TurnBasedRPG.Characters.Skills.张辽.破溃();
            }
            else
            {
                skill2 = new CombatSkill2();
            }
            _selectedCharacter.CalculateSkillValues(skill2);
            totalContentHeight = DrawSkillDetailInCharacter(skill2, detailX, skillDetailY, rightTwoThirdsWidth - 40, skillDetailHeight, detailAlpha);
        }
        else if (_selectedSkillButton == "技能3")
        {
            // 创建并计算技能3的数值
            BaseSkill skill3;
            if (_selectedCharacter is 夏侯惇)
            {
                skill3 = new TurnBasedRPG.Characters.Skills.夏侯惇.铁壁战吼();
            }
            else if (_selectedCharacter is TurnBasedRPG.Characters.Allies.曹仁)
            {
                skill3 = new TurnBasedRPG.Characters.Skills.曹仁.御甲鸣镝();
            }
            else if (_selectedCharacter is TurnBasedRPG.Characters.Allies.司马懿)
            {
                skill3 = new TurnBasedRPG.Characters.Skills.司马懿.窃国者侯();
            }
            else if (_selectedCharacter is TurnBasedRPG.Characters.Allies.曹丕)
            {
                skill3 = new TurnBasedRPG.Characters.Skills.曹丕.受禅代汉();
            }
            else if (_selectedCharacter is TurnBasedRPG.Characters.Allies.曹操)
            {
                skill3 = new TurnBasedRPG.Characters.Skills.曹操.天下归心();
            }
            else if (_selectedCharacter is TurnBasedRPG.Characters.Allies.张辽)
            {
                skill3 = new TurnBasedRPG.Characters.Skills.张辽.威震逍遥津();
            }
            else
            {
                skill3 = new CombatSkill3();
            }
            _selectedCharacter.CalculateSkillValues(skill3);
            totalContentHeight = DrawSkillDetailInCharacter(skill3, detailX, skillDetailY, rightTwoThirdsWidth - 40, skillDetailHeight, detailAlpha);
        }
        else if (_selectedSkillButton == "守备技能")
        {
            // 计算守备技能页面的内容高度
            totalContentHeight = 0;
            int tempCurrentY = 0;
            
            if (_selectedCharacter.DodgeSkill != null)
            {
                BaseSkill tempSkill = _selectedCharacter.DodgeSkill;
                totalContentHeight += 80 + DrawSkillDetailInCharacter(tempSkill, detailX, skillDetailY + tempCurrentY, rightTwoThirdsWidth - 40, skillDetailHeight, detailAlpha);
                tempCurrentY = totalContentHeight;
            }
            
            // 如果是张辽，添加疾行技能
            if (_selectedCharacter is TurnBasedRPG.Characters.Allies.张辽)
            {
                BaseSkill jiXingSkill = new TurnBasedRPG.Characters.Skills.张辽.疾行();
                totalContentHeight += 80 + DrawSkillDetailInCharacter(jiXingSkill, detailX, skillDetailY + tempCurrentY, rightTwoThirdsWidth - 40, skillDetailHeight, detailAlpha);
                tempCurrentY = totalContentHeight;
            }
            
            if (_selectedCharacter.DefendSkill != null)
            {
                BaseSkill defendSkill;
                if (_selectedCharacter is TurnBasedRPG.Characters.Allies.曹丕)
                {
                    defendSkill = new TurnBasedRPG.Characters.Skills.曹丕.御极守成();
                }
                else
                {
                    defendSkill = _selectedCharacter.DefendSkill;
                }
                totalContentHeight += 80 + DrawSkillDetailInCharacter(defendSkill, detailX, skillDetailY + tempCurrentY, rightTwoThirdsWidth - 40, skillDetailHeight, detailAlpha);
                tempCurrentY = totalContentHeight;
            }
            
            if (_selectedCharacter.HealSkill != null)
            {
                BaseSkill tempSkill = _selectedCharacter.HealSkill;
                totalContentHeight += 80 + DrawSkillDetailInCharacter(tempSkill, detailX, skillDetailY + tempCurrentY, rightTwoThirdsWidth - 40, skillDetailHeight, detailAlpha);
                tempCurrentY = totalContentHeight;
            }
            
            if (_selectedCharacter.Name != "曹仁" && _selectedCharacter.Name != "司马懿" && _selectedCharacter.Name != "曹丕" && _selectedCharacter.Name != "曹操" && _selectedCharacter.CounterSkill != null)
            {
                BaseSkill tempSkill = _selectedCharacter.CounterSkill;
                totalContentHeight += 80 + DrawSkillDetailInCharacter(tempSkill, detailX, skillDetailY + tempCurrentY, rightTwoThirdsWidth - 40, skillDetailHeight, detailAlpha);
                tempCurrentY = totalContentHeight;
            }
            
            if (_selectedCharacter is 曹仁)
            {
                BaseSkill moshouXufengSkill = new TurnBasedRPG.Characters.Skills.曹仁.默守蓄锋();
                totalContentHeight += 80 + DrawSkillDetailInCharacter(moshouXufengSkill, detailX, skillDetailY + tempCurrentY, rightTwoThirdsWidth - 40, skillDetailHeight, detailAlpha);
                tempCurrentY = totalContentHeight;
            }
            
            if (_selectedCharacter is 司马懿)
            {
                BaseSkill langguSkill = new TurnBasedRPG.Characters.Skills.司马懿.狼顾();
                totalContentHeight += 80 + DrawSkillDetailInCharacter(langguSkill, detailX, skillDetailY + tempCurrentY, rightTwoThirdsWidth - 40, skillDetailHeight, detailAlpha);
                tempCurrentY = totalContentHeight;
            }
            
            if (_selectedCharacter is TurnBasedRPG.Characters.Allies.曹丕)
            {
                BaseSkill zhihengSkill = new TurnBasedRPG.Characters.Skills.曹丕.制衡();
                int zhihengHeight = DrawSkillDetailInCharacter(zhihengSkill, detailX, skillDetailY + tempCurrentY, rightTwoThirdsWidth - 40, skillDetailHeight, detailAlpha);
                totalContentHeight += 80 + zhihengHeight;
                tempCurrentY = totalContentHeight;
                
                BaseSkill weiWuHongLiuSkill = new TurnBasedRPG.Characters.Skills.曹丕.魏武洪流();
                totalContentHeight += 80 + DrawSkillDetailInCharacter(weiWuHongLiuSkill, detailX, skillDetailY + tempCurrentY, rightTwoThirdsWidth - 40, skillDetailHeight, detailAlpha);
            }
            
            // 如果是曹操，额外显示青釭开天技能
            if (_selectedCharacter is TurnBasedRPG.Characters.Allies.曹操)
            {
                BaseSkill qinggangKaitianSkill = new TurnBasedRPG.Characters.Skills.曹操.青釭开天();
                totalContentHeight += 80 + DrawSkillDetailInCharacter(qinggangKaitianSkill, detailX, skillDetailY + tempCurrentY, rightTwoThirdsWidth - 40, skillDetailHeight, detailAlpha);
            }
            
            // 绘制守备技能详情
            int currentY = skillDetailY - (int)_scrollOffset;
            
            if (_selectedCharacter.DodgeSkill != null)
            {
                // 计算技能数值
                _selectedCharacter.CalculateSkillValues(_selectedCharacter.DodgeSkill);
                int skillHeight = DrawSkillDetailInCharacter(_selectedCharacter.DodgeSkill, detailX, currentY, rightTwoThirdsWidth - 40, skillDetailHeight, detailAlpha);
                currentY += 80 + skillHeight;
            }
            
            // 如果是张辽，额外显示疾行技能
            if (_selectedCharacter is TurnBasedRPG.Characters.Allies.张辽)
            {
                BaseSkill jiXingSkill = new TurnBasedRPG.Characters.Skills.张辽.疾行();
                _selectedCharacter.CalculateSkillValues(jiXingSkill);
                int jiXingHeight = DrawSkillDetailInCharacter(jiXingSkill, detailX, currentY, rightTwoThirdsWidth - 40, skillDetailHeight, detailAlpha);
                currentY += 80 + jiXingHeight;
            }
            
            if (_selectedCharacter.DefendSkill != null)
            {
                // 为曹丕使用特殊的御极守成技能
                BaseSkill defendSkill;
                if (_selectedCharacter is TurnBasedRPG.Characters.Allies.曹丕)
                {
                    defendSkill = new TurnBasedRPG.Characters.Skills.曹丕.御极守成();
                }
                else
                {
                    defendSkill = _selectedCharacter.DefendSkill;
                }
                // 计算技能数值
                _selectedCharacter.CalculateSkillValues(defendSkill);
                int skillHeight = DrawSkillDetailInCharacter(defendSkill, detailX, currentY, rightTwoThirdsWidth - 40, skillDetailHeight, detailAlpha);
                currentY += 80 + skillHeight;
            }
            
            if (_selectedCharacter.HealSkill != null)
            {
                // 计算技能数值
                _selectedCharacter.CalculateSkillValues(_selectedCharacter.HealSkill);
                int skillHeight = DrawSkillDetailInCharacter(_selectedCharacter.HealSkill, detailX, currentY, rightTwoThirdsWidth - 40, skillDetailHeight, detailAlpha);
                currentY += 80 + skillHeight;
            }
            
            // 如果是曹仁、司马懿、曹丕或曹操，不显示默认的CounterSkill
            if (_selectedCharacter.Name != "曹仁" && _selectedCharacter.Name != "司马懿" && _selectedCharacter.Name != "曹丕" && _selectedCharacter.Name != "曹操" && _selectedCharacter.CounterSkill != null)
            {
                // 计算技能数值
                _selectedCharacter.CalculateSkillValues(_selectedCharacter.CounterSkill);
                int skillHeight = DrawSkillDetailInCharacter(_selectedCharacter.CounterSkill, detailX, currentY, rightTwoThirdsWidth - 40, skillDetailHeight, detailAlpha);
                currentY += 80 + skillHeight;
            }
            
            // 如果是曹仁，额外显示默守蓄锋技能
            if (_selectedCharacter is 曹仁)
            {
                BaseSkill moshouXufengSkill = new TurnBasedRPG.Characters.Skills.曹仁.默守蓄锋();
                _selectedCharacter.CalculateSkillValues(moshouXufengSkill);
                int skillHeight = DrawSkillDetailInCharacter(moshouXufengSkill, detailX, currentY, rightTwoThirdsWidth - 40, skillDetailHeight, detailAlpha);
            }
            
            // 如果是司马懿，额外显示狼顾技能
            if (_selectedCharacter is 司马懿)
            {
                BaseSkill langguSkill = new TurnBasedRPG.Characters.Skills.司马懿.狼顾();
                _selectedCharacter.CalculateSkillValues(langguSkill);
                int skillHeight = DrawSkillDetailInCharacter(langguSkill, detailX, currentY, rightTwoThirdsWidth - 40, skillDetailHeight, detailAlpha);
            }
            // 如果是曹丕，额外显示制衡和魏武洪流技能
            if (_selectedCharacter is TurnBasedRPG.Characters.Allies.曹丕)
            {
                BaseSkill zhihengSkill = new TurnBasedRPG.Characters.Skills.曹丕.制衡();
                _selectedCharacter.CalculateSkillValues(zhihengSkill);
                int zhihengHeight = DrawSkillDetailInCharacter(zhihengSkill, detailX, currentY, rightTwoThirdsWidth - 40, skillDetailHeight, detailAlpha);
                currentY += 80 + zhihengHeight;
                
                BaseSkill weiWuHongLiuSkill = new TurnBasedRPG.Characters.Skills.曹丕.魏武洪流();
                _selectedCharacter.CalculateSkillValues(weiWuHongLiuSkill);
                int weiWuHongLiuHeight = DrawSkillDetailInCharacter(weiWuHongLiuSkill, detailX, currentY, rightTwoThirdsWidth - 40, skillDetailHeight, detailAlpha);
            }
            
            // 如果是曹操，额外显示青釭开天技能
            if (_selectedCharacter is TurnBasedRPG.Characters.Allies.曹操)
            {
                BaseSkill qinggangKaitianSkill = new TurnBasedRPG.Characters.Skills.曹操.青釭开天();
                _selectedCharacter.CalculateSkillValues(qinggangKaitianSkill);
                int skillHeight = DrawSkillDetailInCharacter(qinggangKaitianSkill, detailX, currentY, rightTwoThirdsWidth - 40, skillDetailHeight, detailAlpha);
            }
        }
        else if (_selectedSkillButton == "状态")
        {
            try
            {
                // 先计算状态页面的内容高度
                totalContentHeight = 0;
                List<Status> tempStatuses = new List<Status>();
                
                if (_battleSystem != null && _battleSystem.BuffHandler != null)
                {
                    var baseBuffs = _battleSystem.BuffHandler.GetBuffs(_selectedCharacter);
                    foreach (var baseBuff in baseBuffs)
                    {
                        string description = baseBuff.Description.Replace("x%", $"{baseBuff.Strength * 10}%");
                        tempStatuses.Add(new Status(baseBuff.Name, description, baseBuff.IconColor, baseBuff.Strength, baseBuff.RemainingTurns));
                    }
                }
                else
                {
                    if (_selectedCharacter.Name == "夏侯惇")
                    {
                        tempStatuses.Add(new Status("不屈", "受到的伤害降低10%", Color.LightBlue, 1, 1));
                    }
                }
                
                int tempY = 0;
                foreach (Status status in tempStatuses)
                {
                    int statusIconSize = 60;
                    int stateTotalHeight = statusIconSize;
                    
                    string statusDesc = status.Description;
                    if (!statusDesc.Contains("\n"))
                    {
                        statusDesc = statusDesc.Replace("[", "\n[");
                        if (statusDesc.StartsWith("\n"))
                        {
                            statusDesc = statusDesc.Substring(1);
                        }
                    }
                    SpriteFont descFont = GetFontForText(statusDesc);
                    int descHeight = CalculateTextHeight(descFont, statusDesc, rightTwoThirdsWidth - (statusIconSize + 20) - 40, 24);
                    stateTotalHeight = Math.Max(stateTotalHeight, 30 + descHeight);
                    
                    tempY += 80 + stateTotalHeight;
                    totalContentHeight = tempY;
                }
                
                // 绘制状态详情
                int currentY = skillDetailY - (int)_scrollOffset;
                
                // 从BuffHandler获取角色的实际buff列表
                List<Status> statuses = new List<Status>();
                
                if (_battleSystem != null && _battleSystem.BuffHandler != null)
                {
                    var baseBuffs = _battleSystem.BuffHandler.GetBuffs(_selectedCharacter);
                    foreach (var baseBuff in baseBuffs)
                    {
                        // 替换x为实际的强度值
                    string description = baseBuff.Description.Replace("x%", $"{baseBuff.Strength * 10}%");
                    statuses.Add(new Status(baseBuff.Name, description, baseBuff.IconColor, baseBuff.Strength, baseBuff.RemainingTurns));
                    }
                }
                else
                {
                    // 当BuffHandler不可用时，使用默认buff（仅用于测试）
                    if (_selectedCharacter.Name == "夏侯惇")
                    {
                        statuses.Add(new Status("不屈", "受到的伤害降低10%", Color.LightBlue, 1, 1));
                    }
                }
                
                foreach (Status status in statuses)
                {
                    // 绘制状态图标
                    int statusIconSize = 60;
                    _spriteBatch.Draw(_pixel, new Rectangle(detailX, currentY, statusIconSize, statusIconSize), status.Color * detailAlpha);
                    
                    // 绘制回合数数字（右下角，仅在RemainingTurns不为null时显示）
                    if (status.RemainingTurns.HasValue)
                    {
                        string turnsText = status.RemainingTurns.ToString();
                        SpriteFont turnsFont = GetFontForText(turnsText);
                        _spriteBatch.DrawString(turnsFont, turnsText, new Vector2(detailX + statusIconSize - 15, currentY + statusIconSize - 15), whiteWithAlpha, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
                    }
                    
                    // 绘制强度数字（左下角，仅在强度>0时显示）
                    if (status.Strength > 0)
                    {
                        string strengthText = status.Strength.ToString();
                        SpriteFont strengthFont = GetFontForText(strengthText);
                        _spriteBatch.DrawString(strengthFont, strengthText, new Vector2(detailX + 5, currentY + statusIconSize - 15), whiteWithAlpha, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
                    }
                    
                    // 绘制状态名称（大字号）
                    SpriteFont statusNameFont = GetFontForText(status.Name);
                    _spriteBatch.DrawString(statusNameFont, status.Name, new Vector2(detailX + statusIconSize + 20, currentY), whiteWithAlpha, 0, Vector2.Zero, 1.2f, SpriteEffects.None, 0);
                    
                    // 计算状态的总高度
                    int stateTotalHeight = statusIconSize;
                    
                    // 绘制状态描述（一般字号，使用自动换行和条件文本格式）
                    string statusDesc = status.Description;
                    // 仅在没有手动换行符的情况下，处理"[条件]效果"格式
                    if (!statusDesc.Contains("\n"))
                    {
                        // 处理"[条件]效果"格式，在每个"["前添加换行符
                        statusDesc = statusDesc.Replace("[", "\n[");
                        // 移除开头的换行符
                        if (statusDesc.StartsWith("\n"))
                        {
                            statusDesc = statusDesc.Substring(1);
                        }
                    }
                    SpriteFont descFont = GetFontForText(statusDesc);
                    int descHeight = DrawMultiLineTextWithConditionFormat(descFont, statusDesc, new Vector2(detailX + statusIconSize + 20, currentY + 30), whiteWithAlpha, rightTwoThirdsWidth - (statusIconSize + 20) - 40);
                    stateTotalHeight = Math.Max(stateTotalHeight, 30 + descHeight);
                    
                    currentY += 80 + stateTotalHeight;
                }
            }
            catch (Exception ex)
            {
                // 发生异常时，记录错误但不使游戏闪退
            }
        }
        
        // 结束spriteBatch批次并恢复原始裁剪区域
        _spriteBatch.End();
        GraphicsDevice.ScissorRectangle = originalScissorRect;
        _spriteBatch.Begin();
        
        // 绘制滚动条（在技能详情内容之上）- 黑色为底色，白色为滑块
        int scrollBarWidth = 8;
        int scrollBarX = detailX + rightTwoThirdsWidth - 40 - scrollBarWidth;
        int scrollBarHeight = skillDetailHeight;
        _spriteBatch.Draw(_pixel, new Rectangle(scrollBarX, skillDetailY, scrollBarWidth, scrollBarHeight), Color.Black * detailAlpha);
        
        // 计算滚动条滑块位置和大小
        float maxScrollOffset = Math.Max(0, totalContentHeight - skillDetailHeight);
        float scrollRatio = maxScrollOffset > 0 ? _scrollOffset / maxScrollOffset : 0;
        int sliderHeight = totalContentHeight > 0 ? (int)(scrollBarHeight * (skillDetailHeight / (float)totalContentHeight)) : scrollBarHeight;
        sliderHeight = Math.Max(20, sliderHeight); // 滑块最小高度20像素
        int sliderY = skillDetailY + (int)(scrollRatio * (scrollBarHeight - sliderHeight));
        _spriteBatch.Draw(_pixel, new Rectangle(scrollBarX, sliderY, scrollBarWidth, sliderHeight), Color.White * detailAlpha);
        
        // 绘制返回提示
        string exitText = "点击此处或按Backspace回到上一级";
        SpriteFont exitFont = GetFontForText(exitText);
        Vector2 exitSize = exitFont.MeasureString(exitText);
        _spriteBatch.DrawString(exitFont, exitText, new Vector2(10, windowHeight - exitSize.Y - 10), whiteWithAlpha, 0, Vector2.Zero, 0.8f, SpriteEffects.None, 0);
    }
    
    private int DrawSkillDetailInCharacter(BaseSkill skill, int x, int y, int width, int height, float alpha)
    {
        // 确保skill不为null
        if (skill == null)
        {
            return 0;
        }
        
        // 更新角色buff效果，计算最终攻击等级和防御等级
        if (_battleSystem != null && _battleSystem.BuffHandler != null)
        {
            _selectedCharacter.UpdateBuffs(_battleSystem.BuffHandler);
        }
        else
        {
            _selectedCharacter.UpdateBuffs();
        }
        
        int totalHeight = 0;
        try
        {
            int lineHeight = 30;
            Color whiteWithAlpha = new Color(1.0f, 1.0f, 1.0f, alpha);
        
        // 绘制技能图标（根据技能类型使用不同颜色，与技能界面一致）
        int iconSize = 40;
        Color iconColor;
        if (skill is DefendSkill)
        {
            iconColor = Color.Blue * alpha; // 防御技能使用蓝色
        }
        else if (skill is HealSkill)
        {
            iconColor = Color.Green * alpha; // 治疗技能使用绿色
        }
        else if (skill is DodgeSkill)
        {
            iconColor = Color.Yellow * alpha; // 闪避技能使用黄色
        }
        else if (skill is CounterSkill)
        {
            iconColor = Color.Purple * alpha; // 反击技能使用紫色
        }
        else
        {
            iconColor = Color.Red * alpha; // 攻击技能使用红色
        }
        _spriteBatch.Draw(_pixel, new Rectangle(x, y, iconSize, iconSize), iconColor);
        
        // 技能名称
        string skillName = skill.Name;
        SpriteFont nameFont = GetFontForText(skillName);
        Vector2 skillNameSize = nameFont.MeasureString(skillName);
        _spriteBatch.DrawString(nameFont, skillName, new Vector2(x + iconSize + 10, y), whiteWithAlpha, 0, Vector2.Zero, 1.2f, SpriteEffects.None, 0);
        
        // 技能等级信息
        string levelText = string.Empty;
        if (skill.ActionType == ActionType.Attack || skill.ActionType == ActionType.Counter)
        {
            if (_selectedCharacter.HasWeiWuGuZhen)
            {
                levelText = $"防御等级{_selectedCharacter.FinalDefenseLevel}";
            }
            else
            {
                levelText = $"攻击等级{_selectedCharacter.FinalAttackLevel}";
            }
        }
        else if (skill.ActionType == ActionType.Defend || skill.ActionType == ActionType.Dodge)
        {
            levelText = $"防御等级{_selectedCharacter.FinalDefenseLevel}";
        }
        
        if (!string.IsNullOrEmpty(levelText))
        {
            SpriteFont levelFont = GetFontForText(levelText);
            // 向右平移150像素
            _spriteBatch.DrawString(levelFont, levelText, new Vector2(x + iconSize + 10 + skillNameSize.X + 170, y), whiteWithAlpha, 0, Vector2.Zero, 1.2f, SpriteEffects.None, 0);
        }
        
        totalHeight = Math.Max(totalHeight, iconSize);
        
        // 技能基础点数 + 硬币点数
        string valueText = $"{skill.BaseValue} + {skill.CoinValue}";
        SpriteFont valueFont = GetFontForText(valueText);
        _spriteBatch.DrawString(valueFont, valueText, new Vector2(x, y + lineHeight), whiteWithAlpha, 0, Vector2.Zero, 1.0f, SpriteEffects.None, 0);
        
        // 绘制硬币图标
        int coinSize = 20;
        int coinSpacing = 5;
        int coinStartX = x + (int)valueFont.MeasureString(valueText).X + 20;
        for (int i = 0; i < skill.CoinCount; i++)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(coinStartX + i * (coinSize + coinSpacing), y + lineHeight + 5, coinSize, coinSize), whiteWithAlpha);
        }
        
        totalHeight = Math.Max(totalHeight, lineHeight + coinSize + 10);
        
        // 技能的攻击类型与伤害类型（如果有）
        int typeTextY = y + lineHeight * 2;
        if (skill.ActionType == ActionType.Attack || skill.ActionType == ActionType.Counter)
        {
            // 对于攻击技能和反击技能，显示攻击类型和伤害类型
            string damageTypeText = GetDamageTypeChinese(skill.DamageType);
            string typeText = $"攻击类型: {skill.GetAttackTypeName()}, 伤害类型: {damageTypeText}";
            SpriteFont typeFont = GetFontForText(typeText);
            _spriteBatch.DrawString(typeFont, typeText, new Vector2(x, typeTextY), whiteWithAlpha, 0, Vector2.Zero, 0.9f, SpriteEffects.None, 0);
        }
        else if (skill is DefendSkill)
        {
            string typeText = "防御技能";
            SpriteFont typeFont = GetFontForText(typeText);
            _spriteBatch.DrawString(typeFont, typeText, new Vector2(x, typeTextY), whiteWithAlpha, 0, Vector2.Zero, 0.9f, SpriteEffects.None, 0);
        }
        else if (skill is HealSkill)
        {
            string typeText = "治疗技能";
            SpriteFont typeFont = GetFontForText(typeText);
            _spriteBatch.DrawString(typeFont, typeText, new Vector2(x, typeTextY), whiteWithAlpha, 0, Vector2.Zero, 0.9f, SpriteEffects.None, 0);
        }
        else if (skill is DodgeSkill)
        {
            string typeText = "闪避技能";
            SpriteFont typeFont = GetFontForText(typeText);
            _spriteBatch.DrawString(typeFont, typeText, new Vector2(x, typeTextY), whiteWithAlpha, 0, Vector2.Zero, 0.9f, SpriteEffects.None, 0);
        }
        
        totalHeight = Math.Max(totalHeight, typeTextY - y + lineHeight);
        
        // 技能的额外效果（如果有）
        string effectText = GetSkillEffectTextForCharacterDetail(skill);
        int effectHeight = 0;
        if (!string.IsNullOrEmpty(effectText))
        {
            // 仅在没有手动换行符的情况下，处理"[条件]效果"格式
            if (!effectText.Contains("\n"))
            {
                // 处理"[条件]效果"格式，在每个"["前添加换行符
                effectText = effectText.Replace("[", "\n[");
                // 移除开头的换行符
                if (effectText.StartsWith("\n"))
                {
                    effectText = effectText.Substring(1);
                }
            }
            
            SpriteFont effectFont = GetFontForText(effectText);
            effectHeight = DrawMultiLineTextWithConditionFormat(effectFont, effectText, new Vector2(x, typeTextY + lineHeight), whiteWithAlpha, width);
        }
        
        totalHeight = Math.Max(totalHeight, typeTextY - y + lineHeight + effectHeight);
        }
        catch (Exception ex)
        {
            // 发生异常时，记录错误但不使游戏闪退
            Console.WriteLine($"Error drawing skill detail: {ex.Message}");
        }
        
        return totalHeight;
    }
    
    private string GetSkillEffectTextForCharacterDetail(BaseSkill skill)
    {
        if (skill == null || string.IsNullOrEmpty(skill.ExtraEffects))
        {
            return string.Empty;
        }
        return "额外效果:\n" + skill.ExtraEffects;
    }
    
    // 将DamageType枚举转换为中文文本
    private string GetDamageTypeChinese(TurnBasedRPG.Systems.DamageType damageType)
    {
        return damageType switch
        {
            TurnBasedRPG.Systems.DamageType.Physical => "物理伤害",
            TurnBasedRPG.Systems.DamageType.Magic => "魔法伤害",
            TurnBasedRPG.Systems.DamageType.True => "真实伤害",
            _ => damageType.ToString()
        };
    }
    
    // 绘制多行文本，支持自动换行
    private void DrawMultiLineText(SpriteFont font, string text, Vector2 position, Microsoft.Xna.Framework.Color color, int maxWidth = 0)
    {
        float lineHeight = 20 * 1.1f; // 行距调整为原有的1.1倍
        if (maxWidth <= 0)
        {
            // 如果没有指定最大宽度，按换行符分割
            string[] lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                _spriteBatch.DrawString(font, lines[i], new Vector2(position.X, position.Y + i * lineHeight), color);
            }
        }
        else
        {
            // 自动换行
            string[] paragraphs = text.Split('\n');
            float y = position.Y;
            foreach (string paragraph in paragraphs)
            {
                if (string.IsNullOrWhiteSpace(paragraph))
                {
                    y += lineHeight;
                    continue;
                }
                
                string[] words = paragraph.Split(' ');
                string currentLine = words[0];
                
                for (int i = 1; i < words.Length; i++)
                {
                    string testLine = currentLine + " " + words[i];
                    if (font.MeasureString(testLine).X <= maxWidth)
                    {
                        currentLine = testLine;
                    }
                    else
                    {
                        _spriteBatch.DrawString(font, currentLine, new Vector2(position.X, y), color);
                        y += lineHeight;
                        currentLine = words[i];
                    }
                }
                
                _spriteBatch.DrawString(font, currentLine, new Vector2(position.X, y), color);
                y += lineHeight;
            }
        }
    }
    
    // 计算文本高度（考虑自动换行）
    private int CalculateTextHeight(SpriteFont font, string text, int maxWidth, int lineHeight)
    {
        try
        {
            // 处理句号后立刻换行
            text = text.Replace("。", "。\n");
            
            int adjustedLineHeight = (int)(lineHeight * 1.1f); // 行距调整为原有的1.1倍
            if (maxWidth <= 0)
            {
                // 如果没有指定最大宽度，按换行符分割
                string[] lines = text.Split('\n');
                return lines.Length * adjustedLineHeight;
            }
            else
            {
                // 自动换行计算
                string[] paragraphs = text.Split('\n');
                int height = 0;
                foreach (string paragraph in paragraphs)
                {
                    if (string.IsNullOrWhiteSpace(paragraph))
                    {
                        height += adjustedLineHeight;
                        continue;
                    }
                    
                    string trimmedParagraph = paragraph.Trim();
                    
                    // 检查是否是二级标题（[SUBHEADING]...[/SUBHEADING]格式）
                    if (trimmedParagraph.StartsWith("[SUBHEADING]") && trimmedParagraph.EndsWith("[/SUBHEADING]"))
                    {
                        // 标题使用更大的高度
                        height += (int)(36 * 1.3f);
                        continue;
                    }
                    
                    // 检查是否是三级标题（[HEADING]...[/HEADING]格式）
                    if (trimmedParagraph.StartsWith("[HEADING]") && trimmedParagraph.EndsWith("[/HEADING]"))
                    {
                        // 标题使用更大的高度
                        height += (int)(36 * 1.3f);
                        continue;
                    }
                    
                    // 对于中文文本，使用逐字符检查的方式计算换行
                    string currentLine = "";
                    int lineCount = 1;
                    
                    for (int i = 0; i < paragraph.Length; i++)
                    {
                        try
                        {
                            string testLine = currentLine + paragraph[i];
                            if (font.MeasureString(testLine).X <= maxWidth)
                            {
                                currentLine = testLine;
                            }
                            else
                            {
                                lineCount++;
                                currentLine = paragraph[i].ToString();
                            }
                        }
                        catch
                        {
                            // 遇到不支持的字符，跳过
                            continue;
                        }
                    }
                    
                    height += lineCount * adjustedLineHeight;
                }
                return height;
            }
        }
        catch (Exception ex)
        {
            Log($"Error in CalculateTextHeight: {ex.Message}");
            return 100; // 返回一个默认高度
        }
    }
    
    // 绘制带有条件格式的多行文本（支持百科标题格式）
    private int DrawMultiLineTextWithConditionFormat(SpriteFont font, string text, Vector2 position, Microsoft.Xna.Framework.Color color, int maxWidth)
    {
        float y = position.Y;
        float lineHeight = 24 * 1.1f; // 行距调整为原有的1.1倍
        float startY = position.Y;
        
        try
        {
            // 处理句号后立刻换行
            text = text.Replace("。", "。\n");
            
            string[] paragraphs = text.Split('\n');
            
            foreach (string paragraph in paragraphs)
            {
                if (string.IsNullOrWhiteSpace(paragraph))
                {
                    y += lineHeight;
                    continue;
                }
                
                string trimmedParagraph = paragraph.Trim();
                
                // 检查是否是二级标题（[SUBHEADING]...[/SUBHEADING]格式）
                if (trimmedParagraph.StartsWith("[SUBHEADING]") && trimmedParagraph.EndsWith("[/SUBHEADING]"))
                {
                    // 提取标题文本
                    string headingText = trimmedParagraph.Substring(12, trimmedParagraph.Length - 25);
                    
                    // 计算标题宽度
                    float headingWidth = CalculateStringWidth(headingText, _chineseFont, _font);
                    
                    // 绘制标题背景（RGB(80,80,80)）
                    int backgroundPadding = 10;
                    _spriteBatch.Draw(_pixel, new Rectangle((int)position.X - 5, (int)y - 3, (int)headingWidth + backgroundPadding * 2, 36), new Color(80, 80, 80, 255));
                    
                    // 绘制标题文字（带白色轮廓，加大字号）
                    Vector2 headingPosition = new Vector2(position.X + backgroundPadding, y);
                    
                    // 绘制白色轮廓
                    DrawTextWithFontFallback(headingText, headingPosition + new Vector2(-1, -1), Color.White, position.X);
                    DrawTextWithFontFallback(headingText, headingPosition + new Vector2(1, -1), Color.White, position.X);
                    DrawTextWithFontFallback(headingText, headingPosition + new Vector2(-1, 1), Color.White, position.X);
                    DrawTextWithFontFallback(headingText, headingPosition + new Vector2(1, 1), Color.White, position.X);
                    
                    // 绘制黑色文字
                    DrawTextWithFontFallback(headingText, headingPosition, Color.Black, position.X);
                    
                    // 标题使用更大的行距
                    y += 36 * 1.3f;
                    continue;
                }
                
                // 检查是否是三级标题（[HEADING]...[/HEADING]格式）
                if (trimmedParagraph.StartsWith("[HEADING]") && trimmedParagraph.EndsWith("[/HEADING]"))
                {
                    // 提取标题文本
                    string headingText = trimmedParagraph.Substring(9, trimmedParagraph.Length - 18);
                    
                    // 计算标题宽度
                    float headingWidth = CalculateStringWidth(headingText, _chineseFont, _font);
                    
                    // 绘制标题背景（RGB(80,80,80)）
                    int backgroundPadding = 10;
                    _spriteBatch.Draw(_pixel, new Rectangle((int)position.X - 5, (int)y - 3, (int)headingWidth + backgroundPadding * 2, 36), new Color(80, 80, 80, 255));
                    
                    // 绘制标题文字（带白色轮廓，加大字号）
                    Vector2 headingPosition = new Vector2(position.X + backgroundPadding, y);
                    
                    // 绘制白色轮廓
                    DrawTextWithFontFallback(headingText, headingPosition + new Vector2(-1, -1), Color.White, position.X);
                    DrawTextWithFontFallback(headingText, headingPosition + new Vector2(1, -1), Color.White, position.X);
                    DrawTextWithFontFallback(headingText, headingPosition + new Vector2(-1, 1), Color.White, position.X);
                    DrawTextWithFontFallback(headingText, headingPosition + new Vector2(1, 1), Color.White, position.X);
                    
                    // 绘制黑色文字
                    DrawTextWithFontFallback(headingText, headingPosition, Color.Black, position.X);
                    
                    // 标题使用更大的行距
                    y += 36 * 1.3f;
                    continue;
                }
                
                // 自动换行处理
                string remainingText = trimmedParagraph;
                
                while (!string.IsNullOrEmpty(remainingText))
                {
                    // 找到合适的换行点
                    string currentLine = "";
                    int breakIndex = -1;
                    
                    try
                    {
                        // 逐字符检查，找到第一个需要换行的位置
                        for (int i = 0; i < remainingText.Length; i++)
                        {
                            try
                            {
                                string testLine = currentLine + remainingText[i];
                                float testLineWidth = CalculateStringWidth(testLine, _chineseFont, _font);
                                if (testLineWidth > maxWidth)
                                {
                                    breakIndex = i;
                                    break;
                                }
                                currentLine = testLine;
                            }
                            catch
                            {
                                // 遇到不支持的字符，跳过
                                continue;
                            }
                        }
                        
                        // 如果所有文本都能容纳
                        if (breakIndex == -1)
                        {
                            currentLine = remainingText;
                            remainingText = "";
                        }
                        else
                        {
                            // 检查是否在方括号内
                            int lastOpenBracket = remainingText.LastIndexOf('[', breakIndex);
                            int lastCloseBracket = remainingText.LastIndexOf(']', breakIndex);
                            
                            // 如果在方括号内，尝试找到方括号之前的换行点
                            if (lastOpenBracket > lastCloseBracket)
                            {
                                // 在方括号内，寻找方括号之前的换行点
                                breakIndex = lastOpenBracket;
                            }
                            
                            // 直接在当前位置换行，不考虑空格、标点或方括号
                            currentLine = remainingText.Substring(0, breakIndex);
                            remainingText = remainingText.Substring(breakIndex);
                        }
                    }
                    catch (Exception)
                    {
                        // 直接使用剩余文本作为当前行，避免无限循环
                        currentLine = remainingText;
                        remainingText = "";
                    }
                    
                    // 处理多个方括号内的文本
                    int startPos = 0;
                    float currentX = position.X; // 当前绘制位置的X坐标
                    while (startPos < currentLine.Length)
                    {
                        // 找到下一个方括号开始
                        int startIndex = currentLine.IndexOf('[', startPos);
                        if (startIndex == -1)
                        {
                            // 没有更多方括号，绘制剩余文本
                            if (startPos < currentLine.Length)
                            {
                                string remainingPart = currentLine.Substring(startPos);
                                currentX = DrawTextWithFontFallback(remainingPart, new Vector2(currentX, y), color, currentX);
                            }
                            break;
                        }
                        
                        // 绘制方括号前的文本
                        if (startIndex > startPos)
                        {
                            string beforeCondition = currentLine.Substring(startPos, startIndex - startPos);
                            currentX = DrawTextWithFontFallback(beforeCondition, new Vector2(currentX, y), color, currentX);
                        }
                        
                        // 找到对应的方括号结束
                        int endIndex = currentLine.IndexOf(']', startIndex);
                        if (endIndex != -1)
                        {
                            endIndex += 1; // 包含 closing bracket
                            
                            // 绘制条件文本（黑色文字，白色描边）
                            string conditionText = currentLine.Substring(startIndex, endIndex - startIndex);
                            Vector2 conditionPosition = new Vector2(currentX, y);
                            
                            try
                            {
                                // 绘制白色描边
                                DrawTextWithFontFallback(conditionText, conditionPosition + new Vector2(-1, -1), Microsoft.Xna.Framework.Color.White, currentX);
                                DrawTextWithFontFallback(conditionText, conditionPosition + new Vector2(1, -1), Microsoft.Xna.Framework.Color.White, currentX);
                                DrawTextWithFontFallback(conditionText, conditionPosition + new Vector2(-1, 1), Microsoft.Xna.Framework.Color.White, currentX);
                                DrawTextWithFontFallback(conditionText, conditionPosition + new Vector2(1, 1), Microsoft.Xna.Framework.Color.White, currentX);
                                // 绘制黑色文字
                                currentX = DrawTextWithFontFallback(conditionText, conditionPosition, Microsoft.Xna.Framework.Color.Black, currentX);
                            }
                            catch (Exception)
                            {
                            }
                            startPos = endIndex;
                        }
                        else
                        {
                            // 没有找到对应的结束方括号，绘制剩余文本
                            string remainingPart = currentLine.Substring(startPos);
                            currentX = DrawTextWithFontFallback(remainingPart, new Vector2(currentX, y), color, currentX);
                            break;
                        }
                    }
                    
                    y += lineHeight;
                }
            }
        }
        catch (Exception)
        {
        }
        return (int)(y - startY);
    }
    
    // 计算字符串宽度，根据字符类型选择合适的字体
    private float CalculateStringWidth(string text, SpriteFont chineseFont, SpriteFont defaultFont)
    {
        float width = 0;
        int i = 0;
        while (i < text.Length)
        {
            if (IsChineseCharacter(text[i]) && chineseFont != null)
            {
                // 尝试绘制中文字符
                try
                {
                    string chineseChar = text[i].ToString();
                    width += chineseFont.MeasureString(chineseChar).X;
                    i++;
                }
                catch
                {
                    // 中文字体不支持，使用默认字体
                    string charStr = text[i].ToString();
                    width += defaultFont.MeasureString(charStr).X;
                    i++;
                }
            }
            else
            {
                // 非中文字符，使用默认字体
                string charStr = text[i].ToString();
                width += defaultFont.MeasureString(charStr).X;
                i++;
            }
        }
        return width;
    }
    
    // 逐字符绘制文本，根据字符类型选择合适的字体
    private float DrawTextWithFontFallback(string text, Vector2 position, Microsoft.Xna.Framework.Color color, float currentX)
    {
        int i = 0;
        float x = position.X;
        
        while (i < text.Length)
        {
            if (IsChineseCharacter(text[i]) && _chineseFont != null)
            {
                // 尝试用中文字体绘制
                try
                {
                    string chineseChar = text[i].ToString();
                    _spriteBatch.DrawString(_chineseFont, chineseChar, new Vector2(x, position.Y), color);
                    x += _chineseFont.MeasureString(chineseChar).X;
                    i++;
                }
                catch
                {
                    // 中文字体失败，尝试默认字体
                    try
                    {
                        string charStr = text[i].ToString();
                        _spriteBatch.DrawString(_font, charStr, new Vector2(x, position.Y), color);
                        x += _font.MeasureString(charStr).X;
                        i++;
                    }
                    catch
                    {
                        // 都失败，跳过这个字符
                        i++;
                    }
                }
            }
            else
            {
                // 非中文字符，使用默认字体
                try
                {
                    string charStr = text[i].ToString();
                    _spriteBatch.DrawString(_font, charStr, new Vector2(x, position.Y), color);
                    x += _font.MeasureString(charStr).X;
                    i++;
                }
                catch
                {
                    // 失败，跳过这个字符
                    i++;
                }
            }
        }
        return x;
    }
    
    // 判断是否为中文字符或中文标点
    private bool IsChineseCharacter(char c)
    {
        // 中文字符的Unicode范围
        bool isChineseChar = c >= 0x4E00 && c <= 0x9FFF;
        // 中文标点符号的Unicode范围
        bool isChinesePunctuation = (c >= 0x3000 && c <= 0x303F) || c == 0xFF01 || c == 0xFF02 || c == 0xFF03 || 
                                   c == 0xFF04 || c == 0xFF05 || c == 0xFF06 || c == 0xFF07 || c == 0xFF08 || 
                                   c == 0xFF09 || c == 0xFF0A || c == 0xFF0B || c == 0xFF0C || c == 0xFF0D || 
                                   c == 0xFF0E || c == 0xFF0F || c == 0xFF10 || c == 0xFF11 || c == 0xFF12 || 
                                   c == 0xFF13 || c == 0xFF14 || c == 0xFF15 || c == 0xFF16 || c == 0xFF17 || 
                                   c == 0xFF18 || c == 0xFF19 || c == 0xFF1A || c == 0xFF1B || c == 0xFF1C || 
                                   c == 0xFF1D || c == 0xFF1E || c == 0xFF1F || c == 0xFF20 || c == 0xFF3B || 
                                   c == 0xFF3C || c == 0xFF3D || c == 0xFF3E || c == 0xFF3F || c == 0xFF40 || 
                                   c == 0xFF5B || c == 0xFF5C || c == 0xFF5D || c == 0xFF5E || c == 0xFF60 || 
                                   c == 0xFF61 || c == 0xFF62 || c == 0xFF63;
        return isChineseChar || isChinesePunctuation;
    }

    private bool IsPointInRectangle(int x, int y, int rectX, int rectY, int rectWidth, int rectHeight)
    {
        return x >= rectX && x <= rectX + rectWidth && y >= rectY && y <= rectY + rectHeight;
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.DarkSlateGray);

        // 根据游戏状态绘制
        if (_currentGameState == GameState.MainMenu)
        {
            // 主界面
            _spriteBatch.Begin();
            _mainTitle.Draw(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
            _spriteBatch.End();
        }
        else if (_currentGameState == GameState.Encyclopedia)
        {
            // 游戏百科界面
            _spriteBatch.Begin();
            DrawEncyclopedia();
            _spriteBatch.End();
        }
        else if (_currentGameState == GameState.Tutorial)
        {
            // 教程界面 - 使用教程管理器绘制
            _spriteBatch.Begin();
            GraphicsDevice.Clear(Color.DarkOrange);
            
            if (_tutorialManager != null)
            {
                _tutorialManager.Draw();
            }
            
            _spriteBatch.End();
        }
        else if (_currentGameState == GameState.Battle)
        {
            if (_isCharacterDetailMode && _selectedCharacter != null && !_isCharacterZoomingOut)
            {
                // 角色详情模式
                _spriteBatch.Begin();
                
                // 绘制角色详情（添加淡入效果）
                float detailAlpha = 1.0f - _characterFadeAlpha;
                if (detailAlpha < 0.0f) detailAlpha = 0.0f;
                if (detailAlpha > 1.0f) detailAlpha = 1.0f;
                
                // 绘制背景
                _spriteBatch.Draw(_pixel, new Rectangle(0, 0, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight), Color.DarkSlateGray);
                
                // 绘制角色详情，使用透明度
                DrawCharacterDetail();
                
                // 如果正在退出，添加淡出效果
                if (_isCharacterZoomingOut)
                {
                    float fadeOutAlpha = 1.0f - detailAlpha;
                    _spriteBatch.Draw(_pixel, new Rectangle(0, 0, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight), Color.DarkSlateGray * fadeOutAlpha);
                }
                
                _spriteBatch.End();
            }
            else if (_isSkillDetailMode && _selectedSkillSlot != null && !_isZoomingOut)
            {
                // 技能详情模式
                _spriteBatch.Begin();
                
                // 绘制技能详情（添加淡入效果）
                float detailAlpha = 1.0f - _fadeAlpha;
                if (detailAlpha < 0.0f) detailAlpha = 0.0f;
                if (detailAlpha > 1.0f) detailAlpha = 1.0f;
                
                // 绘制背景
                _spriteBatch.Draw(_pixel, new Rectangle(0, 0, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight), Color.DarkSlateGray);
                
                // 绘制技能详情，使用透明度
                DrawSkillDetail();
                
                // 如果正在退出，添加淡出效果
                if (_isZoomingOut)
                {
                    float fadeOutAlpha = 1.0f - detailAlpha;
                    _spriteBatch.Draw(_pixel, new Rectangle(0, 0, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight), Color.DarkSlateGray * fadeOutAlpha);
                }
                
                _spriteBatch.End();
            }
            else
            {
                // 计算变换矩阵，应用缩放和偏移
                // 构建正确的变换矩阵：先平移到屏幕中心，缩放，再平移回来
                Vector2 screenCenter = new Vector2(_graphics.PreferredBackBufferWidth / 2, _graphics.PreferredBackBufferHeight / 2);
                Matrix transformMatrix = Matrix.CreateTranslation(-screenCenter.X, -screenCenter.Y, 0) *
                                        Matrix.CreateScale(_zoomScale) *
                                        Matrix.CreateTranslation(screenCenter.X, screenCenter.Y, 0) *
                                        Matrix.CreateTranslation(_cameraOffset.X, _cameraOffset.Y, 0);
                
                // 正常模式
                _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, transformMatrix);
                
                // 根据fadeAlpha调整透明度
                float alpha = _fadeAlpha;
                if (alpha < 0.0f) alpha = 0.0f;
                if (alpha > 1.0f) alpha = 1.0f;
                
                // 绘制正常界面
                DrawCharacters();
                DrawActionSlots();
                DrawAllArrows();
                DrawBattleMessage();
                DrawBattleLog();
                DrawPauseIndicator();
                if (!_isZooming && !_isZoomingOut && !_isSkillDetailMode && !_isCharacterDetailMode && !_isCharacterZooming && !_isCharacterZoomingOut)
                {
                    // 只有在正常模式下才绘制待选技能
                    DrawControls();
                }
                
                // 如果正在拉近或拉远，绘制淡出效果
                if ((_isZooming || _isZoomingOut) && _fadeAlpha < 1.0f)
                {
                    // 绘制半透明覆盖层
                    _spriteBatch.Draw(_pixel, new Rectangle(0, 0, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight), Color.DarkSlateGray * (1.0f - alpha));
                }
                
                // 绘制回到主菜单的X按钮
                int xButtonSize = 30;
                int xButtonX = _graphics.PreferredBackBufferWidth - xButtonSize - 10;
                int xButtonY = 10;
                _spriteBatch.Draw(_pixel, new Rectangle(xButtonX, xButtonY, xButtonSize, xButtonSize), Color.Red);
                string xText = "X";
                Vector2 xTextSize = _font.MeasureString(xText);
                _spriteBatch.DrawString(_font, xText, new Vector2(xButtonX + (xButtonSize - xTextSize.X) / 2, xButtonY + (xButtonSize - xTextSize.Y) / 2), Color.White);
                
                // 绘制回到主菜单的提示
                if (_battleSystem.BattleEnded)
                {
                    string menuText = "按M键回到主菜单";
                    SpriteFont menuFont = GetFontForText(menuText);
                    Vector2 textSize = menuFont.MeasureString(menuText);
                    int windowWidth = _graphics.PreferredBackBufferWidth;
                    Vector2 position = new Vector2(
                        (windowWidth - textSize.X) / 2,
                        _graphics.PreferredBackBufferHeight - textSize.Y - 60
                    );
                    _spriteBatch.DrawString(menuFont, menuText, position, Color.White);
                }
                
                // 绘制伤害显示效果
                foreach (var damageText in _damageTexts)
                {
                    Vector2 drawPosition = damageText.Position + new Vector2(0, damageText.YOffset);
                    Color drawColor = damageText.TextColor * damageText.Alpha;
                    SpriteFont damageFont = GetFontForText(damageText.Text);
                    Vector2 textOrigin = damageFont.MeasureString(damageText.Text) / 2;
                    
                    // 绘制白色描边（偏移一点）
                    _spriteBatch.DrawString(damageFont, damageText.Text, drawPosition + new Vector2(-1, -1), Color.White * damageText.Alpha, 0f, textOrigin, 1.0f, SpriteEffects.None, 0f);
                    _spriteBatch.DrawString(damageFont, damageText.Text, drawPosition + new Vector2(1, -1), Color.White * damageText.Alpha, 0f, textOrigin, 1.0f, SpriteEffects.None, 0f);
                    _spriteBatch.DrawString(damageFont, damageText.Text, drawPosition + new Vector2(-1, 1), Color.White * damageText.Alpha, 0f, textOrigin, 1.0f, SpriteEffects.None, 0f);
                    _spriteBatch.DrawString(damageFont, damageText.Text, drawPosition + new Vector2(1, 1), Color.White * damageText.Alpha, 0f, textOrigin, 1.0f, SpriteEffects.None, 0f);
                    
                    // 绘制主文字
                    _spriteBatch.DrawString(damageFont, damageText.Text, drawPosition, drawColor, 0f, textOrigin, 1.0f, SpriteEffects.None, 0f);
                }
                
                _spriteBatch.End();
            }
        }

        base.Draw(gameTime);
    }
    
    private void DrawEncyclopedia()
    {
        int windowWidth = _graphics.PreferredBackBufferWidth;
        int windowHeight = _graphics.PreferredBackBufferHeight;
        
        // 绘制背景
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, windowWidth, windowHeight), new Color(125, 125, 125, 255));
        
        // 绘制右上角关闭按钮（红底白X）
        int closeButtonSize = 40;
        int closeButtonX = windowWidth - closeButtonSize - 20;
        int closeButtonY = 20;
        _spriteBatch.Draw(_pixel, new Rectangle(closeButtonX, closeButtonY, closeButtonSize, closeButtonSize), Color.Red);
        
        // 绘制白色X
        string closeText = "X";
        SpriteFont closeFont = GetFontForText(closeText);
        Vector2 closeTextSize = closeFont.MeasureString(closeText);
        _spriteBatch.DrawString(closeFont, closeText, 
            new Vector2(closeButtonX + closeButtonSize / 2 - closeTextSize.X / 2, 
                       closeButtonY + closeButtonSize / 2 - closeTextSize.Y / 2), 
            Color.White, 0, Vector2.Zero, 1.5f, SpriteEffects.None, 0);
        
        // 绘制标题按钮区域
        int buttonWidth = 200;
        int buttonHeight = 45;
        int buttonSpacing = 10;
        int buttonStartX = 20;
        int buttonStartY = 80;
        
        for (int i = 0; i < _encyclopediaTitles.Count; i++)
        {
            int buttonX = buttonStartX;
            int buttonY = buttonStartY + i * (buttonHeight + buttonSpacing);
            
            // 绘制按钮背景
            Color buttonColor = _selectedEncyclopediaTitle == _encyclopediaTitles[i] ? Color.DarkGreen : new Color(80, 80, 80, 255);
            _spriteBatch.Draw(_pixel, new Rectangle(buttonX, buttonY, buttonWidth, buttonHeight), buttonColor);
            
            // 绘制按钮文字（带白色轮廓）
            string titleText = _encyclopediaTitles[i];
            SpriteFont titleFont = GetFontForText(titleText);
            Vector2 titleTextSize = titleFont.MeasureString(titleText);
            Vector2 textPosition = new Vector2(buttonX + (buttonWidth - titleTextSize.X) / 2, 
                                               buttonY + (buttonHeight - titleTextSize.Y) / 2);
            
            // 绘制白色轮廓
            _spriteBatch.DrawString(titleFont, titleText, textPosition + new Vector2(-1, -1), Color.White);
            _spriteBatch.DrawString(titleFont, titleText, textPosition + new Vector2(1, -1), Color.White);
            _spriteBatch.DrawString(titleFont, titleText, textPosition + new Vector2(-1, 1), Color.White);
            _spriteBatch.DrawString(titleFont, titleText, textPosition + new Vector2(1, 1), Color.White);
            
            // 绘制黑色文字
            _spriteBatch.DrawString(titleFont, titleText, textPosition, Color.Black);
        }
        
        // 如果选择了标题，绘制文本内容区域
        if (_selectedEncyclopediaTitle != null)
        {
            int contentX = buttonStartX + buttonWidth + 30;
            int contentY = 80;
            int contentWidth = windowWidth - contentX - 40;
            int contentHeight = windowHeight - contentY - 40;
            
            // 绘制内容区域背景
            _spriteBatch.Draw(_pixel, new Rectangle(contentX, contentY, contentWidth, contentHeight), new Color(80, 80, 80, 255));
            
            // 绘制内容标题
            string contentTitle = _selectedEncyclopediaTitle;
            SpriteFont contentTitleFont = GetFontForText(contentTitle);
            Vector2 contentTitleSize = contentTitleFont.MeasureString(contentTitle);
            _spriteBatch.DrawString(contentTitleFont, contentTitle, 
                new Vector2(contentX + 20, contentY + 10), Color.White, 0, Vector2.Zero, 1.5f, SpriteEffects.None, 0);
            
            // 绘制内容文本（使用裁剪区域）
            int textStartY = contentY + 60;
            int textHeight = contentHeight - 80;
            Rectangle scissorRect = new Rectangle(contentX, textStartY, contentWidth, textHeight);
            Rectangle originalScissorRect = GraphicsDevice.ScissorRectangle;
            
            // 开始新的spriteBatch批次，启用裁剪
            _spriteBatch.End();
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, new RasterizerState { ScissorTestEnable = true });
            GraphicsDevice.ScissorRectangle = scissorRect;
            
            // 从内容字典中获取文本
            string contentText = _encyclopediaContent.ContainsKey(_selectedEncyclopediaTitle) 
                ? _encyclopediaContent[_selectedEncyclopediaTitle] 
                : "暂无内容";
            SpriteFont textFont = IsChineseText(contentText) ? _chineseFont : _font;
            DrawMultiLineTextWithConditionFormat(textFont, contentText, 
                new Vector2(contentX + 20, textStartY - (int)_encyclopediaScrollOffset), Color.White, contentWidth - 40);
            
            // 计算内容总高度（用于滚动条）
            int totalContentHeight = CalculateTextHeight(textFont, contentText, contentWidth - 40, 24);
            
            // 恢复原始裁剪区域
            _spriteBatch.End();
            GraphicsDevice.ScissorRectangle = originalScissorRect;
            _spriteBatch.Begin();
            
            // 绘制滚动条
            int scrollBarWidth = 10;
            int scrollBarX = contentX + contentWidth - scrollBarWidth;
            int scrollBarY = textStartY;
            int scrollBarHeight = textHeight;
            
            // 绘制滚动条背景
            _spriteBatch.Draw(_pixel, new Rectangle(scrollBarX, scrollBarY, scrollBarWidth, scrollBarHeight), Color.Gray);
            
            // 计算并绘制滑块
            float maxScrollOffset = Math.Max(0, totalContentHeight - textHeight);
            float scrollRatio = maxScrollOffset > 0 ? _encyclopediaScrollOffset / maxScrollOffset : 0;
            int sliderHeight = totalContentHeight > 0 ? (int)(scrollBarHeight * (textHeight / (float)Math.Max(textHeight, totalContentHeight))) : scrollBarHeight;
            sliderHeight = Math.Max(20, sliderHeight);
            int sliderY = scrollBarY + (int)(scrollRatio * (scrollBarHeight - sliderHeight));
            _spriteBatch.Draw(_pixel, new Rectangle(scrollBarX, sliderY, scrollBarWidth, sliderHeight), Color.White);
        }
    }
    
    private void DrawSkillDetail()
    {
        try
        {
            int windowWidth = _graphics.PreferredBackBufferWidth;
            int windowHeight = _graphics.PreferredBackBufferHeight;
            int leftThirdWidth = windowWidth / 3;
            int rightTwoThirdsWidth = windowWidth - leftThirdWidth;
            int middleHeight = windowHeight / 2;
            
            // 计算透明度
            float detailAlpha = 1.0f - _fadeAlpha;
            if (detailAlpha < 0.0f) detailAlpha = 0.0f;
            if (detailAlpha > 1.0f) detailAlpha = 1.0f;
            Color whiteWithAlpha = new Color(1.0f, 1.0f, 1.0f, detailAlpha);
            
            // 绘制技能图标
            int iconSize = leftThirdWidth - 40;
            int iconX = 20;
            int iconY = middleHeight - iconSize;
            
            Color slotColor = _selectedSkillSlot.GetTypeColor() * detailAlpha;
            _spriteBatch.Draw(_pixel, new Rectangle(iconX, iconY, iconSize, iconSize), slotColor);
            
            // 绘制技能详情
            int detailX = leftThirdWidth + 20;
            int detailY = 40;
            int lineHeight = 40;
            
            // 技能等级信息
            string levelText = string.Empty;
            // 尝试获取技能所属的角色
            Character character = null;
            if (_battleSystem != null)
            {
                character = _battleSystem.GetCharacterByActionSlot(_selectedSkillSlot);
            }
            
            // 技能名称（大字号）
            string skillName = _selectedSkillSlot.GetSkillName();
            if (_selectedSkillSlot != null)
            {
                if (character != null)
                {
                    string displayName = _selectedSkillSlot.GetSkillDisplayName(character);
                    if (!string.IsNullOrEmpty(displayName))
                    {
                        skillName = displayName;
                    }
                }
            }
            if (string.IsNullOrEmpty(skillName))
            {
                skillName = _selectedSkillSlot.GetTypeName();
            }
            SpriteFont nameFont = GetFontForText(skillName);
            _spriteBatch.DrawString(nameFont, skillName, new Vector2(detailX, detailY), whiteWithAlpha, 0f, 
                Vector2.Zero, 1.5f, SpriteEffects.None, 0f);
            
            if (character != null)
            {
                if (_selectedSkillSlot.Type == ActionType.Attack)
                {
                    levelText = $"攻击等级{character.AttackLevel}";
                }
                else if (_selectedSkillSlot.Type == ActionType.Defend || _selectedSkillSlot.Type == ActionType.Dodge)
                {
                    levelText = $"防御等级{character.DefenseLevel}";
                }
            }
            
            if (!string.IsNullOrEmpty(levelText))
            {
                SpriteFont levelFont = GetFontForText(levelText);
                Vector2 skillNameSize = nameFont.MeasureString(skillName);
                // 向右平移150像素
                _spriteBatch.DrawString(levelFont, levelText, new Vector2(detailX + skillNameSize.X + 170, detailY), whiteWithAlpha, 0f, 
                    Vector2.Zero, 1.5f, SpriteEffects.None, 0f);
            }
            
            detailY += lineHeight;
            
            // 技能基础点数+硬币点数+硬币数量个硬币图标
            string valueText = $"{_selectedSkillSlot.BaseValue} + {_selectedSkillSlot.CoinValue}";
            SpriteFont valueFont = GetFontForText(valueText);
            _spriteBatch.DrawString(valueFont, valueText, new Vector2(detailX, detailY), whiteWithAlpha, 0f, 
                Vector2.Zero, 1.2f, SpriteEffects.None, 0f);
            
            // 绘制硬币图标
            int coinSize = 20;
            int coinSpacing = 5;
            int coinStartX = detailX + (int)valueFont.MeasureString(valueText).X + 20; // 向右移动一些避免重合
            for (int i = 0; i < _selectedSkillSlot.CoinCount; i++)
            {
                _spriteBatch.Draw(_pixel, new Rectangle(coinStartX + i * (coinSize + coinSpacing), detailY + 5, coinSize, coinSize), whiteWithAlpha); // 改为白色
            }
            detailY += lineHeight;
            
            // 技能的攻击类型与伤害类型（如果有）
            if (_selectedSkillSlot.Type == ActionType.Attack || _selectedSkillSlot.Type == ActionType.Counter)
            {
                string attackTypeText = _selectedSkillSlot.GetAttackTypeName();
                string damageTypeText = _selectedSkillSlot.DamageType.ToString();
                
                // 根据当前选择的技能获取正确的伤害类型
                if (character != null && _selectedSkillSlot.SelectedSkill.HasValue && _selectedSkillSlot.Type == ActionType.Attack)
                {
                    BaseSkill selectedSkill = character.GetSkillByActionType(ActionType.Attack, _selectedSkillSlot.SelectedSkill.Value);
                    if (selectedSkill != null)
                    {
                        damageTypeText = GetDamageTypeChinese(selectedSkill.DamageType);
                    }
                }
                else
                {
                    // 使用中文显示伤害类型
                    damageTypeText = GetDamageTypeChinese(_selectedSkillSlot.DamageType);
                }
                
                string typeText = $"攻击类型: {attackTypeText}, 伤害类型: {damageTypeText}";
                SpriteFont typeFont = GetFontForText(typeText);
                _spriteBatch.DrawString(typeFont, typeText, new Vector2(detailX, detailY), whiteWithAlpha, 0f, 
                    Vector2.Zero, 1.0f, SpriteEffects.None, 0f);
                detailY += lineHeight;
            }
            
            // 技能的额外效果（如果有）
            string effectText = GetSkillEffectText(_selectedSkillSlot);
            if (!string.IsNullOrEmpty(effectText))
            {
                // 仅在没有手动换行符的情况下，处理"[条件]效果"格式
                if (!effectText.Contains("\n"))
                {
                    // 处理"[条件]效果"格式，在每个"["前添加换行符
                    effectText = effectText.Replace("[", "\n[");
                    // 移除开头的换行符
                    if (effectText.StartsWith("\n"))
                    {
                        effectText = effectText.Substring(1);
                    }
                }
                
                SpriteFont effectFont = GetFontForText(effectText);
                // 使用带有条件格式的自动换行绘制
                DrawMultiLineTextWithConditionFormat(effectFont, effectText, new Vector2(detailX, detailY), whiteWithAlpha, rightTwoThirdsWidth - 40);
            }
            
            // 在左下角添加返回提示
            string returnText = "点击此处或按Backspace回到上一级";
            SpriteFont returnFont = GetFontForText(returnText);
            Vector2 returnTextPos = new Vector2(20, windowHeight - 40);
            _spriteBatch.DrawString(returnFont, returnText, returnTextPos, whiteWithAlpha, 0f, 
                Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
        }
        catch (Exception ex)
        {
            // 发生异常时，记录错误但不使游戏闪退
            Console.WriteLine($"Error drawing skill detail: {ex.Message}");
            // 可以添加一个简单的错误提示
            int windowWidth = _graphics.PreferredBackBufferWidth;
            int windowHeight = _graphics.PreferredBackBufferHeight;
            float detailAlpha = 1.0f - _fadeAlpha;
            if (detailAlpha < 0.0f) detailAlpha = 0.0f;
            if (detailAlpha > 1.0f) detailAlpha = 1.0f;
            Color whiteWithAlpha = new Color(1.0f, 1.0f, 1.0f, detailAlpha);
            
            string errorText = "技能详情加载失败";
            SpriteFont errorFont = GetFontForText(errorText);
            Vector2 errorTextPos = new Vector2(windowWidth / 2 - errorFont.MeasureString(errorText).X / 2, windowHeight / 2);
            _spriteBatch.DrawString(errorFont, errorText, errorTextPos, whiteWithAlpha, 0f, 
                Vector2.Zero, 1.0f, SpriteEffects.None, 0f);
            
            // 添加返回操作指引
            string returnText = "点击此处或按Backspace回到上一级";
            SpriteFont returnFont = GetFontForText(returnText);
            Vector2 returnTextPos = new Vector2(20, windowHeight - 40);
            _spriteBatch.DrawString(returnFont, returnText, returnTextPos, whiteWithAlpha, 0f, 
                Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
        }
    }
    
    private string GetSkillEffectText(ActionSlot slot)
    {
        try
        {
            if (slot != null && !string.IsNullOrEmpty(slot.ExtraEffects))
            {
                return "额外效果:\n" + slot.ExtraEffects;
            }
            return "";
        }
        catch (Exception ex)
        {
            // 发生异常时，记录错误但不使游戏闪退
            Console.WriteLine($"Error getting skill effect text: {ex.Message}");
            return "";
        }
    }

    private void DrawCharacters()
    {
        int iconSize = 80;
        int offset = 53; // 血条、血量、士气值下移的偏移量（原45，向下偏移8后为53）
        int buffIconSize = 24; // 约为行动槽下方硬币图标的2倍大小
        
        // 绘制己方角色
        for (int i = 0; i < _battleSystem.Players.Count; i++)
        {
            Character player = _battleSystem.Players[i];
            Vector2 playerPos;
            
            // 根据角色索引计算位置
            if (i == 0) // 第一名玩家
            {
                playerPos = new Vector2(20, 60); // 中心点(60, 100)
            }
            else if (i == 1) // 第二名玩家
            {
                playerPos = new Vector2(120, 190); // 中心点(160, 230)，从第一名起点移动(100,130)
            }
            else if (i == 2) // 第三名玩家
            {
                playerPos = new Vector2(20, 320); // 中心点(60, 360)，从第一名起点移动(0,260)
            }
            else if (i == 3) // 第四名玩家
            {
                playerPos = new Vector2(120, 450); // 中心点(160, 490)，从第二名起点移动(0,260)
            }
            else // 第五名玩家
            {
                playerPos = new Vector2(20, 580); // 中心点(60, 620)，从第三名起点移动(0,260)
            }
            
            // 绘制角色图标
            _spriteBatch.Draw(_pixel, new Rectangle((int)playerPos.X, (int)playerPos.Y, iconSize, iconSize), Color.Green);
            
            // 绘制角色名称
            Vector2 playerNamePos = new Vector2(playerPos.X + 40, playerPos.Y + 90);
            SpriteFont playerNameFont = GetFontForText(player.Name);
            _spriteBatch.DrawString(playerNameFont, player.Name, playerNamePos, Color.White, 0f, 
                new Vector2(playerNameFont.MeasureString(player.Name).X / 2, 0), 1f, SpriteEffects.None, 0f);
            
            // 绘制血量条和护盾条（下移到名称下方，再下移53像素）
            float playerHealthPercentage = (float)player.CurrentHealth / player.MaxHealth;
            int playerShield = GetPlayerShield(player);
            
            // 保存角色血条位置
            Vector2 playerHealthBarPos = new Vector2(playerPos.X, playerPos.Y + 70 + offset);
            _characterHealthBarPositions[player] = playerHealthBarPos;
            
            // 优化后的血条和护盾条绘制：总长度保持在healthBarWidth范围内
            DrawHealthAndShieldBar(playerHealthBarPos, 100, 10, 
                player.CurrentHealth, player.MaxHealth, playerShield);
            
            // 绘制血量文本（血条和血量之间的间距降低4像素，从15变为11）
            Vector2 playerHealthTextPos = new Vector2(playerPos.X, playerPos.Y + 81 + offset);
            string playerHealthText = $"{player.CurrentHealth}/{player.MaxHealth}";
            SpriteFont healthFont = GetFontForText(playerHealthText);
            _spriteBatch.DrawString(healthFont, playerHealthText, playerHealthTextPos, Color.White, 0f, 
                new Vector2(0, 0), 0.8f, SpriteEffects.None, 0f);
            
            // 绘制护盾值文本，紧贴总血量数字右侧，间隔5像素
            if (playerShield > 0)
            {
                Vector2 playerShieldTextPos = new Vector2(playerPos.X + healthFont.MeasureString(playerHealthText).X + 5, playerPos.Y + 81 + offset);
                string playerShieldText = $"{playerShield}";
                SpriteFont shieldFont = GetFontForText(playerShieldText);
                _spriteBatch.DrawString(shieldFont, playerShieldText, playerShieldTextPos, Color.Blue, 0f, 
                    new Vector2(0, 0), 0.8f, SpriteEffects.None, 0f);
            }
            
            // 显示士气值（对所有玩家角色显示）
            Vector2 playerMoraleTextPos = new Vector2(playerPos.X, playerPos.Y + 101 + offset); // 增加与血量条的距离
            string playerMoraleText = $"士气: {player.Morale}";
            SpriteFont moraleFont = GetFontForText(playerMoraleText);
            _spriteBatch.DrawString(moraleFont, playerMoraleText, playerMoraleTextPos, Color.Yellow, 0f, 
                new Vector2(0, 0), 0.8f, SpriteEffects.None, 0f);
            
            // 绘制buff图标（在角色头像正上方，横向排列）
            DrawBuffs(player, new Vector2(playerPos.X, playerPos.Y - buffIconSize - 5), buffIconSize);
        }
        
        // 绘制敌方角色
        for (int i = 0; i < _battleSystem.Enemies.Count; i++)
        {
            Character enemy = _battleSystem.Enemies[i];
            Vector2 enemyPos;
            
            // 根据角色索引计算位置
            if (i == 0) // 第一名敌方
            {
                enemyPos = new Vector2(_graphics.PreferredBackBufferWidth - 200, 60); // 中心点(1440, 100)，向左移动100像素
            }
            else if (i == 1) // 第二名敌方
            {
                enemyPos = new Vector2(_graphics.PreferredBackBufferWidth - 100, 190); // 中心点(1540, 230)，从第一名起点移动(100,130)
            }
            else if (i == 2) // 第三名敌方
            {
                enemyPos = new Vector2(_graphics.PreferredBackBufferWidth - 200, 320); // 中心点(1440, 360)，从第一名起点移动(0,260)
            }
            else if (i == 3) // 第四名敌方
            {
                enemyPos = new Vector2(_graphics.PreferredBackBufferWidth - 100, 450); // 中心点(1540, 490)，从第二名起点移动(0,260)
            }
            else // 第五名敌方
            {
                enemyPos = new Vector2(_graphics.PreferredBackBufferWidth - 200, 580); // 中心点(1440, 620)，从第三名起点移动(0,260)
            }
            
            // 绘制角色图标
            _spriteBatch.Draw(_pixel, new Rectangle((int)enemyPos.X, (int)enemyPos.Y, iconSize, iconSize), Color.Red);
            
            // 绘制角色名称
            Vector2 enemyNamePos = new Vector2(enemyPos.X + 40, enemyPos.Y + 90);
            SpriteFont enemyNameFont = GetFontForText(enemy.Name);
            _spriteBatch.DrawString(enemyNameFont, enemy.Name, enemyNamePos, Color.White, 0f, 
                new Vector2(enemyNameFont.MeasureString(enemy.Name).X / 2, 0), 1f, SpriteEffects.None, 0f);
            
            // 绘制血量条和护盾条（下移到名称下方，再下移53像素）
            float enemyHealthPercentage = (float)enemy.CurrentHealth / enemy.MaxHealth;
            int enemyShield = GetEnemyShield(enemy);
            
            // 保存角色血条位置
            Vector2 enemyHealthBarPos = new Vector2(enemyPos.X - 20, enemyPos.Y + 70 + offset);
            _characterHealthBarPositions[enemy] = enemyHealthBarPos;
            
            // 优化后的血条和护盾条绘制：总长度保持在healthBarWidth范围内
            DrawHealthAndShieldBar(enemyHealthBarPos, 
                100, 10, enemy.CurrentHealth, enemy.MaxHealth, enemyShield);
            
            // 绘制血量文本（血条和血量之间的间距降低4像素，从15变为11）
            Vector2 enemyHealthTextPos = new Vector2(enemyPos.X - 20, enemyPos.Y + 81 + offset);
            string enemyHealthText = $"{enemy.CurrentHealth}/{enemy.MaxHealth}";
            SpriteFont healthFont = GetFontForText(enemyHealthText);
            _spriteBatch.DrawString(healthFont, enemyHealthText, enemyHealthTextPos, Color.White, 0f, 
                new Vector2(0, 0), 0.8f, SpriteEffects.None, 0f);
            
            // 绘制护盾值文本，紧贴总血量数字右侧，间隔5像素
            if (enemyShield > 0)
            {
                Vector2 enemyShieldTextPos = new Vector2(enemyPos.X - 20 + healthFont.MeasureString(enemyHealthText).X + 5, 
                    enemyPos.Y + 81 + offset);
                string enemyShieldText = $"{enemyShield}";
                SpriteFont shieldFont = GetFontForText(enemyShieldText);
                _spriteBatch.DrawString(shieldFont, enemyShieldText, enemyShieldTextPos, Color.Blue, 0f, 
                    new Vector2(0, 0), 0.8f, SpriteEffects.None, 0f);
            }
            
            // 显示士气值（对所有角色显示）
            Vector2 enemyMoraleTextPos = new Vector2(enemyPos.X - 20, enemyPos.Y + 101 + offset);
            string enemyMoraleText = $"士气: {enemy.Morale}";
            SpriteFont enemyMoraleFont = GetFontForText(enemyMoraleText);
            _spriteBatch.DrawString(enemyMoraleFont, enemyMoraleText, enemyMoraleTextPos, Color.Yellow, 0f, 
                new Vector2(0, 0), 0.8f, SpriteEffects.None, 0f);
            
            // 绘制buff图标（在角色头像正上方，横向排列）
            DrawBuffs(enemy, new Vector2(enemyPos.X, enemyPos.Y - buffIconSize - 5), buffIconSize);
        }
    }
    
    private void DrawBuffs(Character character, Vector2 position, int iconSize)
    {
        // 从BuffHandler获取角色的buff列表
        List<Buff> buffs = new List<Buff>();
        
        // 从_battleSystem.BuffHandler获取实际的buff列表
        if (_battleSystem != null && _battleSystem.BuffHandler != null)
        {
            var baseBuffs = _battleSystem.BuffHandler.GetBuffs(character);
            foreach (var baseBuff in baseBuffs)
            {
                buffs.Add(new Buff(baseBuff.Name, baseBuff.IconColor, baseBuff.Strength, baseBuff.RemainingTurns));
            }
        }
        else
        {
            // 当BuffHandler不可用时，使用默认buff（仅用于测试）
            if (character.Name == "夏侯惇")
            {
                buffs.Add(new Buff("不屈", Color.LightBlue, 1, 1));
                // 添加一些额外的buff用于测试
                buffs.Add(new Buff("忍耐", Color.LightGreen, 2, 3));
                buffs.Add(new Buff("沉默", Color.DarkRed, 1, 2));
                buffs.Add(new Buff("刚烈", Color.Blue, 1, 1));
                buffs.Add(new Buff("加速", Color.Yellow, 2, 2));
                buffs.Add(new Buff("力量", Color.Orange, 3, 1));
            }
        }
        
        int spacing = 5;
        int maxPerRow = 3; // 最多3列
        int maxRows = 2; // 最多2行
        int totalDisplayable = maxPerRow * maxRows;
        int displayed = 0;
        bool showEllipsis = false;
        
        // 检查是否需要显示省略号
        if (buffs.Count > totalDisplayable)
        {
            showEllipsis = true;
        }
        
        // 从下到上绘制
        for (int row = 0; row < maxRows; row++)
        {
            for (int col = 0; col < maxPerRow; col++)
            {
                int index = row * maxPerRow + col;
                if (index < buffs.Count && (showEllipsis ? index < totalDisplayable - 1 : index < totalDisplayable))
                {
                    Buff buff = buffs[index];
                    // 调整位置计算，使第二行显示在第一行上方
                    Vector2 buffPos = new Vector2(position.X + col * (iconSize + spacing), position.Y - row * (iconSize + spacing));
                    
                    // 绘制buff图标
                    _spriteBatch.Draw(_pixel, new Rectangle((int)buffPos.X, (int)buffPos.Y, iconSize, iconSize), buff.Color);
                    
                    // 绘制回合数数字（右下角，仅在RemainingTurns不为null时显示）
                    if (buff.RemainingTurns.HasValue)
                    {
                        string turnsText = buff.RemainingTurns.ToString();
                        SpriteFont turnsFont = GetFontForText(turnsText);
                        _spriteBatch.DrawString(turnsFont, turnsText, new Vector2(buffPos.X + iconSize - 12, buffPos.Y + iconSize - 12), Color.White, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                    }
                    
                    // 绘制强度数字（左下角，仅在强度>0时显示）
                    if (buff.Strength > 0)
                    {
                        string strengthText = buff.Strength.ToString();
                        SpriteFont strengthFont = GetFontForText(strengthText);
                        _spriteBatch.DrawString(strengthFont, strengthText, new Vector2(buffPos.X + 2, buffPos.Y + iconSize - 12), Color.White, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                    }
                    
                    displayed++;
                }
            }
        }
        
        // 绘制省略号图标
        if (showEllipsis)
        {
            // 调整省略号位置，使其与状态图标的布局保持一致
            Vector2 ellipsisPos = new Vector2(position.X + (maxPerRow - 1) * (iconSize + spacing), position.Y);
            
            // 绘制黑色背景
            _spriteBatch.Draw(_pixel, new Rectangle((int)ellipsisPos.X, (int)ellipsisPos.Y, iconSize, iconSize), Color.Black);
            
            // 绘制白色省略号
            string ellipsisText = "...";
            SpriteFont ellipsisFont = GetFontForText(ellipsisText);
            Vector2 textSize = ellipsisFont.MeasureString(ellipsisText);
            Vector2 textPos = new Vector2(
                ellipsisPos.X + (iconSize - textSize.X) / 2,
                ellipsisPos.Y + (iconSize - textSize.Y) / 2
            );
            _spriteBatch.DrawString(ellipsisFont, ellipsisText, textPos, Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
        }
    }
    
    // 临时Buff类，用于示例
    private class Buff
    {
        public string Name { get; set; }
        public Microsoft.Xna.Framework.Color Color { get; set; }
        public int? RemainingTurns { get; set; }
        public int Strength { get; set; }
        
        public Buff(string name, Microsoft.Xna.Framework.Color color, int strength, int? remainingTurns)
        {
            Name = name;
            Color = color;
            Strength = strength;
            RemainingTurns = remainingTurns;
        }
    }
    
    // 临时Status类，用于示例
    private class Status
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Microsoft.Xna.Framework.Color Color { get; set; }
        public int Strength { get; set; }
        public int? RemainingTurns { get; set; }
        
        public Status(string name, string description, Microsoft.Xna.Framework.Color color, int strength, int? remainingTurns)
        {
            Name = name;
            Description = description;
            Color = color;
            Strength = strength;
            RemainingTurns = remainingTurns;
        }
    }

    private void DrawHealthBar(Vector2 position, int width, int height, int current, int max)
    {
        float healthPercentage = (float)current / max;
        Color healthColor = Color.Green;
        if (healthPercentage < 0.3f)
            healthColor = Color.Red;
        else if (healthPercentage < 0.6f)
            healthColor = Color.Yellow;
        
        _spriteBatch.Draw(_pixel, new Rectangle((int)position.X, (int)position.Y, width, height), Color.Gray);
        _spriteBatch.Draw(_pixel, new Rectangle((int)position.X, (int)position.Y, (int)(width * healthPercentage), height), healthColor);
    }

    private void DrawShieldBar(Vector2 position, int width, int height, int current, int max)
    {
        if (width > 0)
        {
            _spriteBatch.Draw(_pixel, new Rectangle((int)position.X, (int)position.Y, width, height), Color.Blue * 0.7f);
        }
    }

    private void DrawHealthAndShieldBar(Vector2 position, int totalWidth, int height, int currentHealth, int maxHealth, int currentShield)
    {
        
        // 绘制血条背景
        _spriteBatch.Draw(_pixel, new Rectangle((int)position.X, (int)position.Y, totalWidth, height), Color.Gray);
        
        // 绘制血条
        float healthPercentage = (float)currentHealth / maxHealth;
        Color healthColor = Color.Green;
        if (healthPercentage < 0.3f)
            healthColor = Color.Red;
        else if (healthPercentage < 0.6f)
            healthColor = Color.Yellow;
        
        int healthBarLength = (int)(totalWidth * healthPercentage);
        if (healthBarLength > 0)
        {
            _spriteBatch.Draw(_pixel, new Rectangle((int)position.X, (int)position.Y, healthBarLength, height), healthColor);
        }
        
        // 绘制护盾边框（天蓝色边框，与蓝色箭头颜色一致）
        if (currentShield > 0)
        {
            // 计算护盾覆盖的血条比例
            float shieldPercentage = (float)currentShield / maxHealth;
            int shieldWidth;
            int borderThickness = 2; // 边框厚度降低为2
            
            if (shieldPercentage >= 1.0f)
            {
                // 护盾≥100%最大生命时，包裹整个血条
                shieldWidth = totalWidth;
                
                // 绘制边框（上、下、左、右）
                // 上边框
                _spriteBatch.Draw(_pixel, new Rectangle((int)position.X, (int)position.Y, shieldWidth, borderThickness), Color.Cyan);
                // 下边框
                _spriteBatch.Draw(_pixel, new Rectangle((int)position.X, (int)position.Y + height - borderThickness, shieldWidth, borderThickness), Color.Cyan);
                // 左边框
                _spriteBatch.Draw(_pixel, new Rectangle((int)position.X, (int)position.Y, borderThickness, height), Color.Cyan);
                // 右边框
                _spriteBatch.Draw(_pixel, new Rectangle((int)position.X + shieldWidth - borderThickness, (int)position.Y, borderThickness, height), Color.Cyan);
            }
            else
            {
                // 护盾<100%最大生命时，从左边缘开始覆盖相当于（护盾值/最大血量）%的血条
                shieldWidth = (int)(totalWidth * shieldPercentage);
                
                // 绘制边框（只绘制左、上、下边框，不绘制右边框）
                // 上边框
                _spriteBatch.Draw(_pixel, new Rectangle((int)position.X, (int)position.Y, shieldWidth, borderThickness), Color.Cyan);
                // 下边框
                _spriteBatch.Draw(_pixel, new Rectangle((int)position.X, (int)position.Y + height - borderThickness, shieldWidth, borderThickness), Color.Cyan);
                // 左边框
                _spriteBatch.Draw(_pixel, new Rectangle((int)position.X, (int)position.Y, borderThickness, height), Color.Cyan);
            }
        }
    }

    private int GetPlayerShield(Character character)
    {
        if (_battleSystem != null && _battleSystem._characterShields.ContainsKey(character))
        {
            int shield = _battleSystem._characterShields[character];
            return shield;
        }
        return 0;
    }

    private int GetEnemyShield(Character character)
    {
        if (_battleSystem != null && _battleSystem._characterShields.ContainsKey(character))
        {
            int shield = _battleSystem._characterShields[character];
            return shield;
        }
        return 0;
    }

    private int GetMaxPossibleShield()
    {
        // 假设最大护盾值为攻击力的2倍
        if (_battleSystem.Players.Count > 0)
        {
            return _battleSystem.Players[0].AttackPower * 2;
        }
        return 0;
    }

    private void DrawActionSlots()
    {
        int slotWidth = 90; // 增大行动槽宽度
        int slotHeight = 70; // 增大行动槽高度
        int infoBarHeight = 25; // 技能槽上方小区域高度
        int spacing = 15; // 增大间距
        int rowSpacing = 40; // 增大行间距
        int slotsPerRow = 4; // 修改为最多4个/行
        int iconSize = 80;
        int slotOffset = 50; // 行动槽与头像的间距
        
        // 清空行动槽位置缓存
        _actionSlotLeftMidpoints.Clear();
        _actionSlotRightMidpoints.Clear();
        
        // 为每个己方角色绘制行动槽
        int playerSlotIndex = 0;
        for (int characterIndex = 0; characterIndex < _battleSystem.Players.Count; characterIndex++)
        {
            // 计算每个角色的行动槽数量
            int slotsPerCharacter = _battleSystem.PlayerSlots.Count / _battleSystem.Players.Count;
            int remainingSlots = _battleSystem.PlayerSlots.Count % _battleSystem.Players.Count;
            int slotsForThisCharacter = slotsPerCharacter + (characterIndex < remainingSlots ? 1 : 0);
            
            // 根据角色索引计算行动槽起始位置
            Vector2 playerSlotStart;
            int moveRight = 90; // 整体向右移动90像素
            if (characterIndex == 0) // 第一名玩家
            {
                playerSlotStart = new Vector2(20 + iconSize + slotOffset + moveRight, 60 + iconSize / 2 - (slotHeight - infoBarHeight) / 2);
            }
            else if (characterIndex == 1) // 第二名玩家
            {
                playerSlotStart = new Vector2(120 + iconSize + slotOffset + moveRight, 190 + iconSize / 2 - (slotHeight - infoBarHeight) / 2);
            }
            else if (characterIndex == 2) // 第三名玩家
            {
                playerSlotStart = new Vector2(20 + iconSize + slotOffset + moveRight, 320 + iconSize / 2 - (slotHeight - infoBarHeight) / 2);
            }
            else if (characterIndex == 3) // 第四名玩家
            {
                playerSlotStart = new Vector2(120 + iconSize + slotOffset + moveRight, 450 + iconSize / 2 - (slotHeight - infoBarHeight) / 2); // 从第二名起点移动(0,260)
            }
            else // 第五名玩家
            {
                playerSlotStart = new Vector2(20 + iconSize + slotOffset + moveRight, 580 + iconSize / 2 - (slotHeight - infoBarHeight) / 2); // 从第三名起点移动(0,260)
            }
            
            for (int i = 0; i < slotsForThisCharacter && playerSlotIndex < _battleSystem.PlayerSlots.Count; i++)
            {
                ActionSlot playerSlot = _battleSystem.PlayerSlots[playerSlotIndex];
                
                int row = i / slotsPerRow;
                int col = i % slotsPerRow;
                
                Vector2 playerSlotPos = new Vector2(
                    playerSlotStart.X + (slotWidth + spacing) * col,
                    playerSlotStart.Y + (slotHeight + rowSpacing) * row
                );
                
                // 记录行动槽的左右中点位置
                _actionSlotLeftMidpoints[playerSlot] = new Vector2(playerSlotPos.X, playerSlotPos.Y + slotHeight / 2);
                _actionSlotRightMidpoints[playerSlot] = new Vector2(playerSlotPos.X + slotWidth, playerSlotPos.Y + slotHeight / 2);
                
                DrawActionSlot(playerSlot, playerSlotPos, slotWidth, slotHeight, playerSlotIndex == _battleSystem.CurrentPlayerSlot && _battleSystem.CurrentPhase == BattlePhase.PlayerSelection);
                
                // 绘制硬币
                DrawCoins(playerSlot, playerSlotPos.X, playerSlotPos.Y + slotHeight + 5, slotWidth);
                
                playerSlotIndex++;
            }
        }
        
        // 为每个敌方角色绘制行动槽
        int enemySlotIndex = 0;
        for (int characterIndex = 0; characterIndex < _battleSystem.Enemies.Count; characterIndex++)
        {
            // 计算每个角色的行动槽数量
            int slotsPerCharacter = _battleSystem.EnemySlots.Count / _battleSystem.Enemies.Count;
            int remainingSlots = _battleSystem.EnemySlots.Count % _battleSystem.Enemies.Count;
            int slotsForThisCharacter = slotsPerCharacter + (characterIndex < remainingSlots ? 1 : 0);
            
            // 根据角色索引计算行动槽起始位置
            Vector2 enemySlotStart;
            int lastRowSlotsCount = slotsForThisCharacter;
            if (lastRowSlotsCount > slotsPerRow)
            {
                lastRowSlotsCount = lastRowSlotsCount % slotsPerRow;
                if (lastRowSlotsCount == 0)
                    lastRowSlotsCount = slotsPerRow;
            }
            float totalWidthForLastRow = lastRowSlotsCount * slotWidth + (lastRowSlotsCount - 1) * spacing;
            
            if (characterIndex == 0) // 第一名敌方
            {
                enemySlotStart = new Vector2(_graphics.PreferredBackBufferWidth - 200 - iconSize - slotOffset - totalWidthForLastRow, 60 + iconSize / 2 - (slotHeight - infoBarHeight) / 2);
            }
            else if (characterIndex == 1) // 第二名敌方
            {
                enemySlotStart = new Vector2(_graphics.PreferredBackBufferWidth - 100 - iconSize - slotOffset - totalWidthForLastRow, 190 + iconSize / 2 - (slotHeight - infoBarHeight) / 2);
            }
            else if (characterIndex == 2) // 第三名敌方
            {
                enemySlotStart = new Vector2(_graphics.PreferredBackBufferWidth - 200 - iconSize - slotOffset - totalWidthForLastRow, 320 + iconSize / 2 - (slotHeight - infoBarHeight) / 2);
            }
            else if (characterIndex == 3) // 第四名敌方
            {
                enemySlotStart = new Vector2(_graphics.PreferredBackBufferWidth - 100 - iconSize - slotOffset - totalWidthForLastRow, 450 + iconSize / 2 - (slotHeight - infoBarHeight) / 2);
            }
            else // 第五名敌方
            {
                enemySlotStart = new Vector2(_graphics.PreferredBackBufferWidth - 200 - iconSize - slotOffset - totalWidthForLastRow, 580 + iconSize / 2 - (slotHeight - infoBarHeight) / 2);
            }
            // 计算行数
            int numRows = (slotsForThisCharacter + slotsPerRow - 1) / slotsPerRow;
            
            for (int i = 0; i < slotsForThisCharacter && enemySlotIndex < _battleSystem.EnemySlots.Count; i++)
            {
                ActionSlot enemySlot = _battleSystem.EnemySlots[enemySlotIndex];
                
                int row = i / slotsPerRow;
                int col = i % slotsPerRow;
                
                // 计算当前行有多少个槽
                int slotsInCurrentRow;
                if (row < numRows - 1)
                {
                    slotsInCurrentRow = slotsPerRow;
                }
                else
                {
                    slotsInCurrentRow = lastRowSlotsCount;
                }
                
                // 计算当前行的总宽度
                float currentRowWidth = slotsInCurrentRow * slotWidth + (slotsInCurrentRow - 1) * spacing;
                
                // 计算当前行的起始X位置，使其右对齐
                float rowStartX;
                if (characterIndex == 0) // 第一名敌方
                {
                    rowStartX = _graphics.PreferredBackBufferWidth - 200 - iconSize - slotOffset - currentRowWidth;
                }
                else if (characterIndex == 1) // 第二名敌方
                {
                    rowStartX = _graphics.PreferredBackBufferWidth - 100 - iconSize - slotOffset - currentRowWidth;
                }
                else if (characterIndex == 2) // 第三名敌方
                {
                    rowStartX = _graphics.PreferredBackBufferWidth - 200 - iconSize - slotOffset - currentRowWidth;
                }
                else if (characterIndex == 3) // 第四名敌方
                {
                    rowStartX = _graphics.PreferredBackBufferWidth - 100 - iconSize - slotOffset - currentRowWidth;
                }
                else // 第五名敌方
                {
                    rowStartX = _graphics.PreferredBackBufferWidth - 200 - iconSize - slotOffset - currentRowWidth;
                }
                
                Vector2 enemySlotPos = new Vector2(
                    rowStartX + (slotWidth + spacing) * col,
                    enemySlotStart.Y + (slotHeight + rowSpacing) * row
                );
                
                // 记录行动槽的左右中点位置
                _actionSlotLeftMidpoints[enemySlot] = new Vector2(enemySlotPos.X, enemySlotPos.Y + slotHeight / 2);
                _actionSlotRightMidpoints[enemySlot] = new Vector2(enemySlotPos.X + slotWidth, enemySlotPos.Y + slotHeight / 2);
                
                DrawActionSlot(enemySlot, enemySlotPos, slotWidth, slotHeight, false);
                
                // 绘制硬币
                DrawCoins(enemySlot, enemySlotPos.X, enemySlotPos.Y + slotHeight + 5, slotWidth);
                
                enemySlotIndex++;
            }
        }
    }

    private void DrawActionSlot(ActionSlot slot, Vector2 position, int width, int height, bool isCurrent)
    {
        try
        {
            Color borderColor = isCurrent ? Color.White : Color.Gray;
            Color fillColor;
            if (slot.IsDestroyed)
            {
                fillColor = Color.DarkGray;
            }
            else if (slot.Type == ActionType.None)
            {
                // 未选取技能时，底色为更深的深灰色
                fillColor = Color.FromNonPremultiplied(30, 30, 30, 255);
            }
            else
            {
                // 已选取技能后，保持原样
                fillColor = slot.GetTypeColor() * 0.7f;
            }
            int infoBarHeight = 25; // 技能槽上方小区域高度
            int borderThickness = 2;
            
            // 检查是否需要绘制黄色边框（手动选择目标模式）
            bool drawYellowBorder = false;
            if (_battleSystem.IsInManualSelectionMode())
            {
                var sourceSlot = _battleSystem.GetManualSelectionSource();
                if (sourceSlot == slot)
                {
                    drawYellowBorder = true;
                }
                else if (!slot.IsAlly)
                {
                    // 敌方行动槽在手动选择模式下也可能显示黄色边框（作为目标候选）
                    drawYellowBorder = true;
                }
            }
            
            // 绘制黄色边框（如果需要）
            if (drawYellowBorder)
            {
                float yellowAlpha = GetYellowBorderAlpha();
                Color yellowBorderColor = Color.Yellow * yellowAlpha;
                int yellowBorderThickness = 4;
                
                // 绘制黄色边框（稍微扩大一点）
                _spriteBatch.Draw(_pixel, 
                    new Rectangle((int)position.X - yellowBorderThickness, (int)position.Y - infoBarHeight - yellowBorderThickness, 
                        width + yellowBorderThickness * 2, yellowBorderThickness), 
                    yellowBorderColor);
                _spriteBatch.Draw(_pixel, 
                    new Rectangle((int)position.X - yellowBorderThickness, (int)position.Y + height, 
                        width + yellowBorderThickness * 2, yellowBorderThickness), 
                    yellowBorderColor);
                _spriteBatch.Draw(_pixel, 
                    new Rectangle((int)position.X - yellowBorderThickness, (int)position.Y - infoBarHeight - yellowBorderThickness, 
                        yellowBorderThickness, height + infoBarHeight + yellowBorderThickness * 2), 
                    yellowBorderColor);
                _spriteBatch.Draw(_pixel, 
                    new Rectangle((int)position.X + width, (int)position.Y - infoBarHeight - yellowBorderThickness, 
                        yellowBorderThickness, height + infoBarHeight + yellowBorderThickness * 2), 
                    yellowBorderColor);
            }
            
            // 绘制行动槽上方的备选技能槽
            try
            {
                // 绘制备选技能槽背景 - 黑色底色
                _spriteBatch.Draw(_pixel, new Rectangle((int)position.X, (int)position.Y - infoBarHeight, width, infoBarHeight), Color.Black);
                _spriteBatch.Draw(_pixel, new Rectangle((int)position.X, (int)position.Y - infoBarHeight, width, 1), Color.White);
                _spriteBatch.Draw(_pixel, new Rectangle((int)position.X, (int)position.Y - 1, width, 1), Color.White);
                
                // 获取当前行动槽所属的角色
                Character currentCharacter = null;
                if (_battleSystem != null)
                {
                    currentCharacter = _battleSystem.GetCharacterByActionSlot(slot);
                }
                
                // 从左往右依次显示：速度值数字和备选技能名称
                string speedText = slot.Speed.ToString();
                string skillNameText = slot.GetNextSkillDisplayName(currentCharacter, _battleSystem?.BuffHandler);
                
                // 计算绘制位置
                float padding = 5f;
                Vector2 speedPos = new Vector2(position.X + padding, position.Y - infoBarHeight + 5);
                Vector2 skillNamePos;
                
                // 获取字体
                SpriteFont textFont = _chineseFont != null ? _chineseFont : _font;
                
                // 先测量速度值宽度
                float speedWidth = 0f;
                try
                {
                    speedWidth = textFont.MeasureString(speedText).X;
                }
                catch (Exception)
                {
                }
                
                // 计算技能名称的起始位置
                skillNamePos = new Vector2(position.X + padding + speedWidth + 8f, position.Y - infoBarHeight + 5);
                
                // 计算可用宽度
                float availableWidth = width - padding * 2 - speedWidth - 8f;
                
                // 计算动态字号（最低0.4，最高0.65）
                float baseFontSize = 0.65f;
                float minFontSize = 0.4f;
                float skillNameWidth = 0f;
                
                try
                {
                    skillNameWidth = textFont.MeasureString(skillNameText).X * baseFontSize;
                }
                catch (Exception)
                {
                }
                
                float fontSize = baseFontSize;
                if (skillNameWidth > availableWidth && availableWidth > 0 && skillNameWidth > 0)
                {
                    // 文本太长，降低字号
                    float scale = availableWidth / skillNameWidth;
                    fontSize = Math.Max(minFontSize, baseFontSize * scale);
                }
                
                // 获取备选技能的颜色
                Color skillColor = Color.White;
                if (slot.NextSkill.HasValue && currentCharacter != null)
                {
                    BaseSkill nextSkill = currentCharacter.GetSkillByActionType(ActionType.Attack, slot.NextSkill.Value);
                    if (nextSkill != null)
                    {
                        switch (nextSkill.DamageType)
                        {
                            case DamageType.Physical:
                                skillColor = Color.Orange;
                                break;
                            case DamageType.Magic:
                                skillColor = Color.Cyan;
                                break;
                            case DamageType.True:
                                skillColor = Color.White;
                                break;
                        }
                    }
                }
                
                // 绘制速度值（黄色）
                try
                {
                    if (_chineseFont != null)
                    {
                        _spriteBatch.DrawString(_chineseFont, speedText, speedPos, Color.Yellow, 0f, 
                            Vector2.Zero, baseFontSize, SpriteEffects.None, 0f);
                    }
                    else
                    {
                        _spriteBatch.DrawString(_font, speedText, speedPos, Color.Yellow, 0f, 
                            Vector2.Zero, baseFontSize, SpriteEffects.None, 0f);
                    }
                }
                catch (Exception)
                {
                }
                
                // 绘制技能名称
                try
                {
                    if (_chineseFont != null)
                    {
                        _spriteBatch.DrawString(_chineseFont, skillNameText, skillNamePos, skillColor, 0f, 
                            Vector2.Zero, fontSize, SpriteEffects.None, 0f);
                    }
                    else
                    {
                        _spriteBatch.DrawString(_font, skillNameText, skillNamePos, skillColor, 0f, 
                            Vector2.Zero, fontSize, SpriteEffects.None, 0f);
                    }
                }
                catch (Exception)
                {
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error drawing next skill text: {ex.Message}");
            }
            
            // 绘制行动槽
            _spriteBatch.Draw(_pixel, new Rectangle((int)position.X, (int)position.Y, width, height), fillColor);
            _spriteBatch.Draw(_pixel, new Rectangle((int)position.X, (int)position.Y, width, 2), borderColor);
            _spriteBatch.Draw(_pixel, new Rectangle((int)position.X, (int)position.Y + height - 2, width, 2), borderColor);
            _spriteBatch.Draw(_pixel, new Rectangle((int)position.X, (int)position.Y, 2, height), borderColor);
            _spriteBatch.Draw(_pixel, new Rectangle((int)position.X + width - 2, (int)position.Y, 2, height), borderColor);
            
            if (slot.Type != ActionType.None)
            {
                try
                {
                    string actionText = GetActionTypeName(slot.Type);
                    string valueText;
                    string skillText;
                    
                    // 获取当前行动槽所属的角色
                    Character currentCharacter = null;
                    if (_battleSystem != null)
                    {
                        currentCharacter = _battleSystem.GetCharacterByActionSlot(slot);
                    }
                    
                    skillText = slot.GetSkillDisplayName(currentCharacter, _battleSystem?.BuffHandler);
                    
                    if (slot.IsDestroyed)
                    {
                        actionText = "X";
                        valueText = "";
                        skillText = "";
                    }
                    else
                    {
                        // 检查是否已投掷硬币（检查Coins数组中是否有非0值）
                        bool coinsFlipped = false;
                        if (slot.Coins != null && slot.Coins.Length > 0)
                        {
                            foreach (int coin in slot.Coins)
                            {
                                if (coin != 0)
                                {
                                    coinsFlipped = true;
                                    break;
                                }
                            }
                        }
                        
                        if (coinsFlipped)
                        {
                            // 已投掷硬币，显示实际最终点数
                            int currentValue = slot.BaseValue + slot.GetCurrentCoinValue();
                            valueText = $"{currentValue}";
                        }
                        else
                        {
                            // 未投掷硬币，显示可能的点数范围
                            int minValue = slot.BaseValue; // 最小点数：基础值 + 0个硬币
                            int maxValue = slot.BaseValue + slot.CoinValue * slot.CoinCount; // 最大点数：基础值 + 所有硬币最大值
                            valueText = $"{minValue}|{maxValue}";
                        }
                    }
                
                // 行动槽上部分：技能名称
                Vector2 actionTextPos = new Vector2(position.X + width / 2, position.Y + 12);
                Vector2 skillTextPos = new Vector2(position.X + width / 2, position.Y + 24);
                
                // 行动槽下部分：攻击方式和最终值
                Vector2 attackTypePos = new Vector2(position.X + 10, position.Y + 38);
                Vector2 valueTextPos = new Vector2(position.X + width / 2, position.Y + 38); // 减小上下间距
                
                SpriteFont actionFont = GetFontForText(actionText);
                SpriteFont skillFont = GetFontForText(skillText);
                SpriteFont valueFont = GetFontForText(valueText);
                
                // 减小行动种类的字号
                _spriteBatch.DrawString(actionFont, actionText, actionTextPos, Color.White, 0f, 
                    new Vector2(actionFont.MeasureString(actionText).X / 2, 0), 0.55f, SpriteEffects.None, 0f);
                
                // 所有技能类型都显示技能名称
                if (!string.IsNullOrEmpty(skillText))
                {
                    // 根据当前选择的技能获取正确的颜色
                    Color skillColor = slot.GetSkillColor();
                    if (slot.Type == ActionType.Attack && currentCharacter != null && slot.SelectedSkill.HasValue)
                    {
                        BaseSkill selectedSkill = currentCharacter.GetSkillByActionType(ActionType.Attack, slot.SelectedSkill.Value);
                        if (selectedSkill != null)
                        {
                            switch (selectedSkill.DamageType)
                            {
                                case DamageType.Physical:
                                    skillColor = Color.Orange; // 物理伤害使用橙黄色
                                    break;
                                case DamageType.Magic:
                                    skillColor = Color.Cyan; // 魔法伤害使用青色
                                    break;
                                case DamageType.True:
                                    skillColor = Color.White; // 真实伤害使用白色
                                    break;
                            }
                        }
                    }
                    _spriteBatch.DrawString(skillFont, skillText, skillTextPos, skillColor, 0f, 
                        new Vector2(skillFont.MeasureString(skillText).X / 2, 0), 0.7f, SpriteEffects.None, 0f);
                }
                
                // 显示攻击方式（仅攻击技能）
                if (slot.Type == ActionType.Attack)
                {
                    string attackTypeText = slot.GetAttackTypeName();
                    SpriteFont attackTypeFont = GetFontForText(attackTypeText);
                    // 计算位置，让攻击类型和点数上下限均匀排布，且位置平齐
                    int attackTypeX = (int)(position.X + width / 4);
                    Vector2 adjustedAttackTypePos = new Vector2(attackTypeX, position.Y + 40);
                    _spriteBatch.DrawString(attackTypeFont, attackTypeText, adjustedAttackTypePos, Color.White, 0f, 
                        new Vector2(attackTypeFont.MeasureString(attackTypeText).X / 2, 0), 0.5f, SpriteEffects.None, 0f);
                }
                
                // 降低最小最大点数的字号
                // 计算位置，让攻击类型和点数上下限均匀排布
                int valueTextX = (int)(position.X + 3 * width / 4);
                Vector2 adjustedValueTextPos = new Vector2(valueTextX, position.Y + 38);
                _spriteBatch.DrawString(valueFont, valueText, adjustedValueTextPos, Color.White, 0f, 
                    new Vector2(valueFont.MeasureString(valueText).X / 2, 0), 0.6f, SpriteEffects.None, 0f);
                }
                catch (Exception ex)
                {
                    // 发生异常时记录错误但不使游戏闪退
                    Console.WriteLine($"Error drawing action slot content: {ex.Message}");
                }
            }
            else
            {
                try
                {
                    string slotNumber = slot.Index.ToString();
                    string skillText = slot.GetSkillName();
                    
                    // 行动槽序号向上偏移
                    Vector2 slotNumberPos = new Vector2(position.X + width / 2, position.Y + 8);
                    // 技能名称向下偏移，增加与序号的距离
                    Vector2 skillTextPos = new Vector2(position.X + width / 2, position.Y + 35);
                    
                    SpriteFont numberFont = GetFontForText(slotNumber);
                    SpriteFont skillFont = GetFontForText(skillText);
                    
                    _spriteBatch.DrawString(numberFont, slotNumber, slotNumberPos, Color.White, 0f, 
                        new Vector2(numberFont.MeasureString(slotNumber).X / 2, 0), 1f, SpriteEffects.None, 0f);
                    
                    // 获取当前行动槽所属的角色
                    Character currentCharacter = null;
                    if (_battleSystem != null)
                    {
                        currentCharacter = _battleSystem.GetCharacterByActionSlot(slot);
                    }
                    
                    if (!string.IsNullOrEmpty(skillText))
                    {
                        // 根据当前选择的技能获取正确的颜色
                        Color skillColor = slot.GetSkillColor();
                        if (currentCharacter != null && slot.SelectedSkill.HasValue)
                        {
                            BaseSkill selectedSkill = currentCharacter.GetSkillByActionType(ActionType.Attack, slot.SelectedSkill.Value);
                            if (selectedSkill != null)
                            {
                                switch (selectedSkill.DamageType)
                                {
                                    case DamageType.Physical:
                                        skillColor = Color.Orange; // 物理伤害使用橙黄色
                                        break;
                                    case DamageType.Magic:
                                        skillColor = Color.Cyan; // 魔法伤害使用青色
                                        break;
                                    case DamageType.True:
                                        skillColor = Color.White; // 真实伤害使用白色
                                        break;
                                }
                            }
                        }
                        _spriteBatch.DrawString(skillFont, skillText, skillTextPos, skillColor, 0f, 
                            new Vector2(skillFont.MeasureString(skillText).X / 2, 0), 0.7f, SpriteEffects.None, 0f);
                        
                        // 显示攻击方式（如果是攻击技能）
                        if (slot.AttackType != AttackType.Slash || !string.IsNullOrEmpty(skillText))
                        {
                            string attackTypeText = slot.GetAttackTypeName();
                            if (!string.IsNullOrEmpty(attackTypeText))
                            {
                                SpriteFont attackTypeFont = GetFontForText(attackTypeText);
                                int attackTypeX = (int)(position.X + width / 4);
                                Vector2 adjustedAttackTypePos = new Vector2(attackTypeX, position.Y + 55);
                                _spriteBatch.DrawString(attackTypeFont, attackTypeText, adjustedAttackTypePos, Color.White, 0f, 
                                    new Vector2(attackTypeFont.MeasureString(attackTypeText).X / 2, 0), 0.5f, SpriteEffects.None, 0f);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 发生异常时记录错误但不使游戏闪退
                    Console.WriteLine($"Error drawing empty action slot: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            // 发生异常时记录错误但不使游戏闪退
            Console.WriteLine($"Error drawing action slot: {ex.Message}");
        }
    }

    private string GetActionTypeName(ActionType type)
    {
        return type switch
        {
            ActionType.Attack => "攻击",
            ActionType.Defend => "防御",
            ActionType.Heal => "治疗",
            ActionType.Dodge => "闪避",
            ActionType.Counter => "反击",
            _ => "无"
        };
    }

    private void DrawBattleMessage()
    {
        Vector2 messagePos = new Vector2(_graphics.PreferredBackBufferWidth / 2, 750);
        SpriteFont messageFont = GetFontForText(_battleSystem.BattleMessage);
        _spriteBatch.DrawString(messageFont, _battleSystem.BattleMessage, messagePos, Color.White, 0f, 
            new Vector2(messageFont.MeasureString(_battleSystem.BattleMessage).X / 2, 0), 1f, SpriteEffects.None, 0f);
    }
    
    private void DrawPauseIndicator()
    {
        if (_battleSystem.IsPaused)
        {
            int windowWidth = _graphics.PreferredBackBufferWidth;
            int windowHeight = _graphics.PreferredBackBufferHeight;
            
            // 绘制半透明背景遮罩
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, windowWidth, windowHeight), new Color(0, 0, 0, 128));
            
            // 绘制暂停文字
            string pauseText = "已暂停 - 按空格键继续";
            SpriteFont pauseFont = IsChineseText(pauseText) ? _chineseFont : _font;
            Vector2 pauseTextSize = pauseFont.MeasureString(pauseText);
            Vector2 pausePosition = new Vector2(windowWidth / 2, windowHeight / 2);
            
            // 绘制白色轮廓
            _spriteBatch.DrawString(pauseFont, pauseText, pausePosition + new Vector2(-2, -2), Color.White, 0f, 
                new Vector2(pauseTextSize.X / 2, pauseTextSize.Y / 2), 2.0f, SpriteEffects.None, 0f);
            _spriteBatch.DrawString(pauseFont, pauseText, pausePosition + new Vector2(2, -2), Color.White, 0f, 
                new Vector2(pauseTextSize.X / 2, pauseTextSize.Y / 2), 2.0f, SpriteEffects.None, 0f);
            _spriteBatch.DrawString(pauseFont, pauseText, pausePosition + new Vector2(-2, 2), Color.White, 0f, 
                new Vector2(pauseTextSize.X / 2, pauseTextSize.Y / 2), 2.0f, SpriteEffects.None, 0f);
            _spriteBatch.DrawString(pauseFont, pauseText, pausePosition + new Vector2(2, 2), Color.White, 0f, 
                new Vector2(pauseTextSize.X / 2, pauseTextSize.Y / 2), 2.0f, SpriteEffects.None, 0f);
            
            // 绘制黄色文字
            _spriteBatch.DrawString(pauseFont, pauseText, pausePosition, Color.Yellow, 0f, 
                new Vector2(pauseTextSize.X / 2, pauseTextSize.Y / 2), 2.0f, SpriteEffects.None, 0f);
        }
    }

    private void DrawBattleLog()
    {
        // 日志区域的参数
        int logAreaLeftMargin = 5; // 窗口左边缘距离5像素
        int logAreaRightMargin = 5; // 滚动条左边缘距离5像素
        int scrollBarWidth = 8;
        int skillButtonStartY = 800; // 技能按钮开始的Y位置
        int logAreaY = skillButtonStartY;
        int logAreaHeight = _graphics.PreferredBackBufferHeight - logAreaY;
        
        // 滚动条位置 - 在按键部分的左侧
        int scrollBarX = _graphics.PreferredBackBufferWidth - 200; // 放在按键部分左侧，按键在800开始
        int logAreaWidth = scrollBarX - logAreaLeftMargin - logAreaRightMargin;
        int logAreaX = logAreaLeftMargin;
        int lineHeight = 18;
        int lineSpacing = lineHeight; // 每两条日志中间间隔一行
        
        // 绘制滚动条 - 深灰色为底色，白色为滑块
        _spriteBatch.Draw(_pixel, new Rectangle(scrollBarX, logAreaY, scrollBarWidth, logAreaHeight), Color.DarkGray);
        
        // 合并两个日志列表：先显示上一回合的日志，再显示当前回合的日志
        List<string> allLogs = new List<string>();
        // 上一回合的日志（倒序显示，最新的在最上面）
        for (int i = _battleSystem.PreviousBattleLog.Count - 1; i >= 0; i--)
        {
            allLogs.Add(_battleSystem.PreviousBattleLog[i]);
        }
        // 当前回合的日志（倒序显示，最新的在最上面）
        for (int i = _battleSystem.BattleLog.Count - 1; i >= 0; i--)
        {
            allLogs.Add(_battleSystem.BattleLog[i]);
        }
        
        // 计算所有日志的总高度（需要考虑自动换行）
        float totalLogHeight = 0;
        List<float> logLineOffsets = new List<float>(); // 每个日志的起始偏移
        float currentOffset = 0;
        
        for (int i = 0; i < allLogs.Count; i++)
        {
            logLineOffsets.Add(currentOffset);
            string logEntry = allLogs[i];
            string cleanedLogEntry = CleanLogEntry(logEntry);
            
            // 计算自动换行后的行数
            SpriteFont font = IsChineseText(cleanedLogEntry) ? _chineseFont : _font;
            Vector2 textSize = font.MeasureString(cleanedLogEntry) * 0.65f;
            int numLines = (int)Math.Ceiling(textSize.X / logAreaWidth);
            if (numLines == 0) numLines = 1;
            
            float logHeight = numLines * lineHeight;
            totalLogHeight += logHeight;
            currentOffset += logHeight + lineSpacing; // 添加日志高度和行间距
        }
        
        // 计算最大滚动偏移
        float maxBattleLogScrollOffset = Math.Max(0, totalLogHeight - logAreaHeight);
        
        // 计算滚动条滑块位置
        float scrollRatio = maxBattleLogScrollOffset > 0 ? _battleLogScrollOffset / maxBattleLogScrollOffset : 0;
        float heightRatio = totalLogHeight > 0 ? logAreaHeight / (float)Math.Max(logAreaHeight, totalLogHeight) : 1;
        int sliderHeight = Math.Max(20, (int)(logAreaHeight * heightRatio));
        int sliderY = logAreaY + (int)(scrollRatio * (logAreaHeight - sliderHeight));
        
        // 绘制滚动条滑块 - 白色
        _spriteBatch.Draw(_pixel, new Rectangle(scrollBarX, sliderY, scrollBarWidth, sliderHeight), Color.White);
        
        // 绘制日志内容区域的裁剪
        Rectangle logScissorRect = new Rectangle(logAreaX, logAreaY, logAreaWidth, logAreaHeight);
        Rectangle originalScissorRect = _spriteBatch.GraphicsDevice.ScissorRectangle;
        _spriteBatch.GraphicsDevice.ScissorRectangle = logScissorRect;
        
        // 开始一个新的spriteBatch批次，启用裁剪
        _spriteBatch.End();
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, new RasterizerState { ScissorTestEnable = true });
        
        // 绘制日志内容（从最新到最旧，带自动换行）
        float drawOffset = -_battleLogScrollOffset;
        for (int i = 0; i < allLogs.Count; i++)
        {
            try
            {
                string logEntry = allLogs[i];
                string cleanedLogEntry = CleanLogEntry(logEntry);
                float logStartY = logAreaY + drawOffset + logLineOffsets[i];
                
                // 使用字符逐个绘制的方式实现自动换行
                SpriteFont font = IsChineseText(cleanedLogEntry) ? _chineseFont : _font;
                float scale = 0.65f;
                float currentX = logAreaX;
                float currentY = logStartY;
                float spaceWidth = font.MeasureString(" ").X * scale;
                
                // 将文本按单词/字符分割
                List<string> words = new List<string>();
                string currentWord = "";
                foreach (char c in cleanedLogEntry)
                {
                    if (c == ' ' || c == '，' || c == ',' || c == '。' || c == '.')
                    {
                        if (!string.IsNullOrEmpty(currentWord))
                        {
                            words.Add(currentWord);
                            currentWord = "";
                        }
                        words.Add(c.ToString());
                    }
                    else
                    {
                        currentWord += c;
                    }
                }
                if (!string.IsNullOrEmpty(currentWord))
                {
                    words.Add(currentWord);
                }
                
                // 逐词换行绘制
                foreach (string word in words)
                {
                    Vector2 wordSize = font.MeasureString(word) * scale;
                    if (currentX + wordSize.X > logAreaX + logAreaWidth)
                    {
                        // 换行
                        currentX = logAreaX;
                        currentY += lineHeight;
                    }
                    
                    try
                    {
                        _spriteBatch.DrawString(font, word, 
                            new Vector2(currentX, currentY), Color.LightGray, 0f,
                            new Vector2(0, 0), scale, SpriteEffects.None, 0f);
                    }
                    catch (Exception)
                    {
                        // 如果中文字体失败，尝试使用英文字体
                        try
                        {
                            _spriteBatch.DrawString(_font, word, 
                                new Vector2(currentX, currentY), Color.LightGray, 0f,
                                new Vector2(0, 0), scale, SpriteEffects.None, 0f);
                        }
                        catch (Exception)
                        {
                            // 忽略异常
                        }
                    }
                    
                    currentX += wordSize.X;
                }
            }
            catch (Exception)
            {
                // 捕获异常，防止游戏因无法解析的字符而闪退
            }
        }
        
        // 恢复原始裁剪矩形
        _spriteBatch.End();
        _spriteBatch.GraphicsDevice.ScissorRectangle = originalScissorRect;
        _spriteBatch.Begin();
    }
    
    private string CleanLogEntry(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (char c in text)
        {
            // 只保留：
            // 1. 基本中文 (0x4E00-0x9FFF)
            // 2. ASCII 可打印字符 (0x0020-0x007E)
            // 3. 用英文逗号代替中文逗号
            if (c == '，')
            {
                sb.Append(',');
            }
            else if (c == '（')
            {
                sb.Append('(');
            }
            else if (c == '）')
            {
                sb.Append(')');
            }
            else if ((c >= 0x4E00 && c <= 0x9FFF) || 
                     (c >= 0x0020 && c <= 0x007E))
            {
                sb.Append(c);
            }
            // 其他字符直接跳过
        }
        return sb.ToString();
    }

    private void DrawControls()
    {
        Vector2 controlsPos = new Vector2(_graphics.PreferredBackBufferWidth / 2, 700);
        
        if (_battleSystem.BattleEnded)
        {
            string restartText = "按 R 键重新开始";
            SpriteFont restartFont = GetFontForText(restartText);
            _spriteBatch.DrawString(restartFont, restartText, controlsPos, Color.Orange, 0f,
                new Vector2(restartFont.MeasureString(restartText).X / 2, 0), 1f, SpriteEffects.None, 0f);
        }
        else if (_battleSystem.CurrentPhase == BattlePhase.PlayerSelection)
        {
            // 绘制技能选择按钮
            DrawSkillSelectionButtons();
            
            // 显示回退提示
            if (_battleSystem.CurrentPlayerSlot > 0)
            {
                string undoText = "按Backspace键回退最近一次选择的行动槽";
                Vector2 undoPos = new Vector2(_graphics.PreferredBackBufferWidth / 2, 730);
                SpriteFont undoFont = GetFontForText(undoText);
                _spriteBatch.DrawString(undoFont, undoText, undoPos, Color.LightGray, 0f,
                    new Vector2(undoFont.MeasureString(undoText).X / 2, 0), 0.8f, SpriteEffects.None, 0f);
            }
        }
        else if (_battleSystem.CurrentPhase == BattlePhase.Resolution)
        {
            string resolutionText = "正在解析行动...";
            SpriteFont resolutionFont = GetFontForText(resolutionText);
            _spriteBatch.DrawString(resolutionFont, resolutionText, controlsPos, Color.Cyan, 0f,
                new Vector2(resolutionFont.MeasureString(resolutionText).X / 2, 0), 1f, SpriteEffects.None, 0f);
        }
    }

    private void DrawSkillSelectionButtons()
    {
        int buttonWidth = 120; // 增加按钮宽度以容纳更多信息
        int buttonHeight = 60;
        int spacing = 20;
        int startY = 800; // 下调技能选择按钮的位置
        
        // 获取当前选中的行动槽
        ActionSlot currentSlot = _battleSystem.PlayerSlots[_battleSystem.CurrentPlayerSlot];
        
        // 获取当前行动槽所属的角色
        Character currentCharacter = null;
        if (_battleSystem != null && _battleSystem.PlayerSlots.Count > _battleSystem.CurrentPlayerSlot)
        {
            // 尝试获取当前行动槽所属的角色
            // 根据行动槽索引分配角色，确保每个角色对应固定的行动槽范围
            int playerSlotIndex = _battleSystem.CurrentPlayerSlot;
            int slotCounter = 0;
            
            foreach (var player in _battleSystem.Players)
            {
                // 计算每个角色的行动槽数量
                int slotsPerCharacter = _battleSystem.PlayerSlots.Count / _battleSystem.Players.Count;
                int remainingSlots = _battleSystem.PlayerSlots.Count % _battleSystem.Players.Count;
                int slotsForThisCharacter = slotsPerCharacter + (slotCounter < remainingSlots ? 1 : 0);
                
                if (playerSlotIndex >= slotCounter * slotsPerCharacter && playerSlotIndex < slotCounter * slotsPerCharacter + slotsForThisCharacter)
                {
                    currentCharacter = player;
                    break;
                }
                
                slotCounter++;
            }
            
            // 如果没有找到角色，使用默认角色
            if (currentCharacter == null && _battleSystem.Players.Count > 0)
            {
                currentCharacter = _battleSystem.Players[0];
            }
        }
        
        // 攻击技能参数
        int attackBaseValue = 0;
        int attackCoinValue = 0;
        int attackCoinCount = 0;
        string attackSkillName = "攻击";
        BaseSkill attackSkill = null;
        
        // 根据当前槽位的技能设置攻击技能参数
        if (currentCharacter != null && currentSlot.SelectedSkill.HasValue)
        {
            attackSkill = currentCharacter.GetSkillByActionType(ActionType.Attack, currentSlot.SelectedSkill);
            if (attackSkill != null)
            {
                currentCharacter.CalculateSkillValues(attackSkill);
                attackBaseValue = attackSkill.BaseValue;
                attackCoinValue = attackSkill.CoinValue;
                attackCoinCount = attackSkill.CoinCount;
                attackSkillName = attackSkill.Name;
            }
        }
        
        // 备选技能参数
        int altBaseValue = 0;
        int altCoinValue = 0;
        int altCoinCount = 0;
        bool hasAltSkill = false;
        string altSkillName = "备选";
        BaseSkill altSkill = null;
        if (currentCharacter != null && currentSlot.NextSkill.HasValue)
        {
            altSkill = currentCharacter.GetSkillByActionType(ActionType.Attack, currentSlot.NextSkill);
            if (altSkill != null)
            {
                currentCharacter.CalculateSkillValues(altSkill);
                altBaseValue = altSkill.BaseValue;
                altCoinValue = altSkill.CoinValue;
                altCoinCount = altSkill.CoinCount;
                altSkillName = altSkill.Name;
                hasAltSkill = true;
            }
        }
        
        // 防御技能参数
        BaseSkill defendSkill = null;
        int defendBaseValue = 0;
        int defendCoinValue = 0;
        int defendCoinCount = 0;
        string defendSkillName = "防御";
        if (currentCharacter != null)
        {
            defendSkill = currentCharacter.GetSkillByActionType(ActionType.Defend);
            if (defendSkill != null)
            {
                currentCharacter.CalculateSkillValues(defendSkill);
                defendBaseValue = defendSkill.BaseValue;
                defendCoinValue = defendSkill.CoinValue;
                defendCoinCount = defendSkill.CoinCount;
                defendSkillName = defendSkill.Name;
            }
        }
        
        // 治疗技能参数
        BaseSkill healSkill = null;
        int healBaseValue = 0;
        int healCoinValue = 0;
        int healCoinCount = 0;
        string healSkillName = "治疗";
        if (currentCharacter != null)
        {
            healSkill = currentCharacter.GetSkillByActionType(ActionType.Heal);
            if (healSkill != null)
            {
                currentCharacter.CalculateSkillValues(healSkill);
                healBaseValue = healSkill.BaseValue;
                healCoinValue = healSkill.CoinValue;
                healCoinCount = healSkill.CoinCount;
                healSkillName = healSkill.Name;
            }
        }
        
        // 闪避技能参数
        BaseSkill dodgeSkill = null;
        int dodgeBaseValue = 0;
        int dodgeCoinValue = 0;
        int dodgeCoinCount = 0;
        string dodgeSkillName = "闪避";
        if (currentCharacter != null)
        {
            dodgeSkill = currentCharacter.GetSkillByActionType(ActionType.Dodge);
            if (dodgeSkill != null)
            {
                currentCharacter.CalculateSkillValues(dodgeSkill);
                dodgeBaseValue = dodgeSkill.BaseValue;
                dodgeCoinValue = dodgeSkill.CoinValue;
                dodgeCoinCount = dodgeSkill.CoinCount;
                dodgeSkillName = dodgeSkill.Name;
            }
        }
        
        // 反击技能参数
        BaseSkill counterSkill = null;
        int counterBaseValue = 0;
        int counterCoinValue = 0;
        int counterCoinCount = 0;
        string counterSkillName = "反击";
        if (currentCharacter != null)
        {
            counterSkill = currentCharacter.GetSkillByActionType(ActionType.Counter);
            if (counterSkill != null)
            {
                currentCharacter.CalculateSkillValues(counterSkill);
                counterBaseValue = counterSkill.BaseValue;
                counterCoinValue = counterSkill.CoinValue;
                counterCoinCount = counterSkill.CoinCount;
                counterSkillName = counterSkill.Name;
            }
        }
        
        // 收集可用的技能按钮
        List<SkillButtonInfo> availableButtons = new List<SkillButtonInfo>();
        
        // 攻击按钮
        availableButtons.Add(new SkillButtonInfo("A", attackSkillName, ActionType.Attack, attackBaseValue, attackCoinValue, attackCoinCount));
        
        // 备选按钮
        if (hasAltSkill)
        {
            availableButtons.Add(new SkillButtonInfo("B", altSkillName, ActionType.Attack, altBaseValue, altCoinValue, altCoinCount));
        }
        
        // 防御按钮
        if (defendSkill != null && defendSkill.CanBeSelected)
        {
            availableButtons.Add(new SkillButtonInfo("D", defendSkillName, ActionType.Defend, defendBaseValue, defendCoinValue, defendCoinCount));
        }
        
        // 闪避按钮
        if (dodgeSkill != null && dodgeSkill.CanBeSelected)
        {
            availableButtons.Add(new SkillButtonInfo("S", dodgeSkillName, ActionType.Dodge, dodgeBaseValue, dodgeCoinValue, dodgeCoinCount));
        }
        
        // 治疗按钮
        if (healSkill != null && healSkill.CanBeSelected)
        {
            availableButtons.Add(new SkillButtonInfo("H", healSkillName, ActionType.Heal, healBaseValue, healCoinValue, healCoinCount));
        }
        
        // 反击按钮
        if (counterSkill != null && counterSkill.CanBeSelected)
        {
            availableButtons.Add(new SkillButtonInfo("C", counterSkillName, ActionType.Counter, counterBaseValue, counterCoinValue, counterCoinCount));
        }
        
        // 计算起始X坐标，确保按钮居中对齐
        int totalWidth = availableButtons.Count * buttonWidth + (availableButtons.Count - 1) * spacing;
        int startX = (_graphics.PreferredBackBufferWidth - totalWidth) / 2;
        
        // 绘制可用的技能按钮
        for (int i = 0; i < availableButtons.Count; i++)
        {
            SkillButtonInfo buttonInfo = availableButtons[i];
            int x = startX + i * (buttonWidth + spacing);
            DrawSkillButton(x, startY, buttonWidth, buttonHeight, buttonInfo.Key, buttonInfo.Name, buttonInfo.ActionType, buttonInfo.BaseValue, buttonInfo.CoinValue, buttonInfo.CoinCount);
        }
    }
    
    private class SkillButtonInfo
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public ActionType ActionType { get; set; }
        public int BaseValue { get; set; }
        public int CoinValue { get; set; }
        public int CoinCount { get; set; }
        
        public SkillButtonInfo(string key, string name, ActionType actionType, int baseValue, int coinValue, int coinCount)
        {
            Key = key;
            Name = name;
            ActionType = actionType;
            BaseValue = baseValue;
            CoinValue = coinValue;
            CoinCount = coinCount;
        }
    }

    private void DrawSkillButton(int x, int y, int width, int height, string key, string name, ActionType actionType, int baseValue, int coinValue, int coinCount)
    {
        // 绘制按钮背景
        _spriteBatch.Draw(_pixel, new Rectangle(x, y, width, height), Color.DarkGray);
        
        // 绘制按钮边框
        _spriteBatch.Draw(_pixel, new Rectangle(x, y, width, 2), Color.White);
        _spriteBatch.Draw(_pixel, new Rectangle(x, y + height - 2, width, 2), Color.White);
        _spriteBatch.Draw(_pixel, new Rectangle(x, y, 2, height), Color.White);
        _spriteBatch.Draw(_pixel, new Rectangle(x + width - 2, y, 2, height), Color.White);
        
        // 绘制按键
        Vector2 keyPos = new Vector2(x + 10, y + 5);
        SpriteFont keyFont = GetFontForText(key);
        _spriteBatch.DrawString(keyFont, key, keyPos, Color.Yellow, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
        
        // 绘制技能名称（向上移动，与快捷键文本平齐）
        Vector2 namePos = new Vector2(x + width / 2, y + 20);
        SpriteFont nameFont = GetFontForText(name);
        // 动态调整字号以确保技能名称能正确显示在按钮范围内
        float maxNameWidth = width - 60; // 预留一些空间给按钮边框和快捷键
        float nameScale = 0.8f; // 默认字号
        float measuredNameWidth = nameFont.MeasureString(name).X * nameScale;
        
        // 如果文字宽度超过了最大宽度，缩小字号
        if (measuredNameWidth > maxNameWidth)
        {
            nameScale = maxNameWidth / (nameFont.MeasureString(name).X);
            // 设置字号下限，不要太小
            if (nameScale < 0.4f)
                nameScale = 0.4f;
        }
        
        _spriteBatch.DrawString(nameFont, name, namePos, Color.White, 0f, 
            new Vector2(nameFont.MeasureString(name).X / 2, 0), nameScale, SpriteEffects.None, 0f);
        
        // 绘制硬币变动值（格式：基础值+变动值）
        string coinValueText = $"{baseValue}+{coinValue}";
        Vector2 coinValuePos = new Vector2(x + 30, y + 2);
        SpriteFont coinValueFont = GetFontForText(coinValueText);
        _spriteBatch.DrawString(coinValueFont, coinValueText, coinValuePos, Color.Yellow, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        
        // 绘制硬币数量（小方块），间距为5，确保最右侧硬币在框内，与上边缘距离8
        int coinSize = 6;
        int coinSpacing = 5;
        int rightMargin = 8;
        int topMargin = 8;
        int totalCoinWidth = coinCount * coinSize + (coinCount - 1) * coinSpacing;
        int coinStartX = x + width - totalCoinWidth - rightMargin;
        for (int i = 0; i < coinCount; i++)
        {
            int coinX = coinStartX + i * (coinSize + coinSpacing);
            _spriteBatch.Draw(_pixel, new Rectangle(coinX, y + topMargin, coinSize, coinSize), Color.White);
        }
        
        // 绘制攻击方式（仅攻击技能）
        if (actionType == ActionType.Attack)
        {
            string attackTypeText = "斩击";
            SpriteFont attackTypeFont = GetFontForText(attackTypeText);
            // 计算位置，让攻击类型和点数上下限均匀排布，且位置平齐
            int attackTypeX = x + width / 4;
            Vector2 attackTypePos = new Vector2(attackTypeX, y + 40);
            _spriteBatch.DrawString(attackTypeFont, attackTypeText, attackTypePos, Color.White, 0f, 
                new Vector2(attackTypeFont.MeasureString(attackTypeText).X / 2, 0), 0.6f, SpriteEffects.None, 0f);
        }
        
        // 绘制最小和最大值
        int minValue = baseValue;
        int maxValue = baseValue + (coinCount * coinValue);
        string rangeText = $"{minValue}|{maxValue}";
        // 计算位置，让攻击类型和点数上下限均匀排布
        int rangeX = x + 3 * width / 4;
        Vector2 rangePos = new Vector2(rangeX, y + 38);
        SpriteFont rangeFont = GetFontForText(rangeText);
        _spriteBatch.DrawString(rangeFont, rangeText, rangePos, Color.White, 0f, 
            new Vector2(rangeFont.MeasureString(rangeText).X / 2, 0), 0.7f, SpriteEffects.None, 0f);
    }

    private void DrawCoins(ActionSlot slot, float x, float y, int slotWidth)
    {
        // 已执行完成的技能（被销毁或已完成的技能槽）不显示硬币图标
        if (!slot.IsDestroyed && !slot.IsCompleted && slot.Coins != null && slot.Coins.Length > 0)
        {
            int coinSize = 8;
            int coinSpacing = 10;
            int startX = (int)(x + (slotWidth - (slot.Coins.Length * (coinSize + coinSpacing) - coinSpacing)) / 2);
            
            for (int i = 0; i < slot.Coins.Length; i++)
            {
                Color coinColor;
                switch (slot.Coins[i])
                {
                    case 1: // 正面
                        coinColor = Color.White;
                        break;
                    case -1: // 反面
                        coinColor = Color.Black;
                        break;
                    default: // 未投
                        coinColor = Color.Gray;
                        break;
                }
                
                // 绘制硬币（圆形）
                int coinX = startX + i * (coinSize + coinSpacing);
                _spriteBatch.Draw(_pixel, new Rectangle(coinX, (int)y, coinSize, coinSize), coinColor);
            }
        }
    }
    
    // ==================== 目标系统可视化相关方法 ====================
    
    // 绘制箭头
    private void DrawArrow(Vector2 start, Vector2 end, Color color, float alpha)
    {
        if (_pixel == null)
            return;
        
        // 计算箭头方向
        Vector2 direction = end - start;
        float length = direction.Length();
        if (length < 1f)
            return;
        
        direction.Normalize();
        
        // 箭头大小（稍微放大）
        float arrowHeadLength = 20f;
        float arrowHeadWidth = 12f;
        
        // 绘制箭身（线条），提前结束避免覆盖箭头
        Vector2 lineEnd = end - direction * arrowHeadLength * 0.3f;
        DrawLine(start, lineEnd, color * alpha, 3f);
        
        // 绘制箭头
        Vector2 arrowHeadBase = end - direction * arrowHeadLength;
        Vector2 perpendicular = new Vector2(-direction.Y, direction.X);
        
        Vector2 arrowPoint1 = arrowHeadBase + perpendicular * arrowHeadWidth / 2f;
        Vector2 arrowPoint2 = arrowHeadBase - perpendicular * arrowHeadWidth / 2f;
        
        DrawTriangle(end, arrowPoint1, arrowPoint2, color * alpha);
    }
    
    // 绘制线条
    private void DrawLine(Vector2 start, Vector2 end, Color color, float thickness)
    {
        if (_pixel == null)
            return;
        
        Vector2 edge = end - start;
        float angle = (float)Math.Atan2(edge.Y, edge.X);
        
        _spriteBatch.Draw(_pixel, 
            new Rectangle((int)start.X, (int)start.Y, (int)edge.Length(), (int)thickness),
            null,
            color,
            angle,
            new Vector2(0, thickness / 2f),
            SpriteEffects.None,
            0);
    }
    
    // 绘制三角形
    private void DrawTriangle(Vector2 p1, Vector2 p2, Vector2 p3, Color color)
    {
        if (_pixel == null)
            return;
        
        // 计算三角形的边界
        float minX = Math.Min(Math.Min(p1.X, p2.X), p3.X);
        float maxX = Math.Max(Math.Max(p1.X, p2.X), p3.X);
        float minY = Math.Min(Math.Min(p1.Y, p2.Y), p3.Y);
        float maxY = Math.Max(Math.Max(p1.Y, p2.Y), p3.Y);
        
        // 填充三角形（光栅化）
        for (float y = minY; y <= maxY; y++)
        {
            for (float x = minX; x <= maxX; x++)
            {
                Vector2 point = new Vector2(x, y);
                if (IsPointInTriangle(point, p1, p2, p3))
                {
                    _spriteBatch.Draw(_pixel, new Rectangle((int)x, (int)y, 1, 1), color);
                }
            }
        }
    }
    
    // 判断点是否在三角形内
    private bool IsPointInTriangle(Vector2 p, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        float d1 = Sign(p, p1, p2);
        float d2 = Sign(p, p2, p3);
        float d3 = Sign(p, p3, p1);
        
        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
        
        return !(hasNeg && hasPos);
    }
    
    // 计算叉积
    private float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);
    }
    
    // 绘制所有箭头
    private void DrawAllArrows()
    {
        var allSlots = _battleSystem.PlayerSlots.Concat(_battleSystem.EnemySlots).ToList();
        
        // 判断哪些箭头应该显示
        HashSet<ActionSlot> arrowsToShow = new HashSet<ActionSlot>();
        Dictionary<ActionSlot, float> slotAlphas = new Dictionary<ActionSlot, float>();
        
        if (_battleSystem.CurrentPhase == BattlePhase.PlayerSelection || 
            _battleSystem.CurrentPhase == BattlePhase.EnemySelection)
        {
            // 玩家选择阶段的显示逻辑
            bool showAllArrows = _hoveredActionSlot == null;
            
            if (showAllArrows)
            {
                // 显示所有有目标的箭头，0.5透明度
                foreach (var slot in allSlots)
                {
                    if (slot.TargetSlot != null)
                    {
                        arrowsToShow.Add(slot);
                        slotAlphas[slot] = 0.5f;
                    }
                }
            }
            else
            {
                // 只显示与悬停行动槽相关的箭头，1.0透明度
                // 1. 悬停行动槽发出的箭头
                if (_hoveredActionSlot.TargetSlot != null)
                {
                    arrowsToShow.Add(_hoveredActionSlot);
                    slotAlphas[_hoveredActionSlot] = 1.0f;
                }
                
                // 2. 指向悬停行动槽的箭头
                foreach (var slot in allSlots)
                {
                    if (slot.TargetSlot == _hoveredActionSlot)
                    {
                        arrowsToShow.Add(slot);
                        slotAlphas[slot] = 1.0f;
                    }
                }
            }
        }
        else if (_battleSystem.CurrentPhase == BattlePhase.Resolution)
        {
            // 战斗解析阶段的显示逻辑
            var executionOrder = _battleSystem.ExecutionOrder;
            int currentStep = _battleSystem.CurrentResolutionStep;
            
            // 1. 当前正在执行的行动槽
            if (currentStep < executionOrder.Count)
            {
                ActionSlot currentSlot = executionOrder[currentStep];
                if (currentSlot.TargetSlot != null && !currentSlot.IsDestroyed && !currentSlot.IsCompleted)
                {
                    arrowsToShow.Add(currentSlot);
                    slotAlphas[currentSlot] = 1.0f; // 完全不透明
                    
                    // 如果有配对关系，也显示配对的箭头
                    if (currentSlot.TargetSlot.TargetSlot == currentSlot && !currentSlot.IsUnilateralAttack)
                    {
                        ActionSlot pairSlot = currentSlot.TargetSlot;
                        if (pairSlot.TargetSlot != null && !pairSlot.IsDestroyed && !pairSlot.IsCompleted)
                        {
                            arrowsToShow.Add(pairSlot);
                            slotAlphas[pairSlot] = 1.0f; // 完全不透明
                        }
                    }
                }
            }
            
            // 2. 下一个将要执行的行动槽
            int nextStep = currentStep + 1;
            while (nextStep < executionOrder.Count)
            {
                ActionSlot nextSlot = executionOrder[nextStep];
                if (!nextSlot.IsDestroyed && !nextSlot.IsCompleted)
                {
                    if (nextSlot.TargetSlot != null)
                    {
                        arrowsToShow.Add(nextSlot);
                        slotAlphas[nextSlot] = 1.0f / 3.0f; // 1/3透明度
                        
                        // 如果有配对关系，也显示配对的箭头
                        if (nextSlot.TargetSlot.TargetSlot == nextSlot && !nextSlot.IsUnilateralAttack)
                        {
                            ActionSlot pairSlot = nextSlot.TargetSlot;
                            if (pairSlot.TargetSlot != null && !pairSlot.IsDestroyed && !pairSlot.IsCompleted && 
                                !arrowsToShow.Contains(pairSlot))
                            {
                                arrowsToShow.Add(pairSlot);
                                slotAlphas[pairSlot] = 1.0f / 3.0f; // 1/3透明度
                            }
                        }
                    }
                    break; // 找到第一个下一个行动槽就停止
                }
                nextStep++;
            }
        }
        
        // 绘制箭头
        HashSet<Tuple<ActionSlot, ActionSlot>> drawnPairs = new HashSet<Tuple<ActionSlot, ActionSlot>>();
        
        foreach (var slot in arrowsToShow)
        {
            if (slot.TargetSlot == null)
                continue;
            
            // 获取起始和结束位置
            Vector2 start, end;
            if (slot.IsAlly)
            {
                // 我方行动槽：从右边缘中点出发
                if (!_actionSlotRightMidpoints.ContainsKey(slot))
                    continue;
                start = _actionSlotRightMidpoints[slot];
            }
            else
            {
                // 敌方行动槽：从左边缘中点出发
                if (!_actionSlotLeftMidpoints.ContainsKey(slot))
                    continue;
                start = _actionSlotLeftMidpoints[slot];
            }
            
            if (slot.TargetSlot.IsAlly)
            {
                // 目标是我方：指向右边缘中点
                if (!_actionSlotRightMidpoints.ContainsKey(slot.TargetSlot))
                    continue;
                end = _actionSlotRightMidpoints[slot.TargetSlot];
            }
            else
            {
                // 目标是敌方：指向左边缘中点
                if (!_actionSlotLeftMidpoints.ContainsKey(slot.TargetSlot))
                    continue;
                end = _actionSlotLeftMidpoints[slot.TargetSlot];
            }
            
            // 检查是否是交汇的箭头（互瞄）
            bool isMutual = slot.TargetSlot.TargetSlot == slot;
            
            // 确定箭头颜色：只有当双方互瞄时才是红色
            Color arrowColor = isMutual ? Color.Red : Color.Cyan;
            
            // 获取透明度
            float alpha = slotAlphas.ContainsKey(slot) ? slotAlphas[slot] : 0.5f;
            
            // 检查是否已经绘制过这对箭头（避免重复绘制互瞄箭头）
            var pair = new Tuple<ActionSlot, ActionSlot>(slot, slot.TargetSlot);
            var reversePair = new Tuple<ActionSlot, ActionSlot>(slot.TargetSlot, slot);
            
            if (isMutual && (drawnPairs.Contains(pair) || drawnPairs.Contains(reversePair)))
            {
                continue; // 已经绘制过这对了，跳过
            }
            
            if (isMutual)
            {
                // 交汇箭头：在中点处交汇
                Vector2 midPoint = (start + end) / 2f;
                
                // 绘制从起点到中点的箭头
                DrawArrow(start, midPoint, arrowColor, alpha);
                
                // 获取配对的箭头的起始位置
                Vector2 pairStart;
                if (slot.TargetSlot.IsAlly)
                {
                    if (!_actionSlotRightMidpoints.ContainsKey(slot.TargetSlot))
                        continue;
                    pairStart = _actionSlotRightMidpoints[slot.TargetSlot];
                }
                else
                {
                    if (!_actionSlotLeftMidpoints.ContainsKey(slot.TargetSlot))
                        continue;
                    pairStart = _actionSlotLeftMidpoints[slot.TargetSlot];
                }
                
                // 绘制从配对起点到中点的箭头
                float pairAlpha = slotAlphas.ContainsKey(slot.TargetSlot) ? slotAlphas[slot.TargetSlot] : alpha;
                DrawArrow(pairStart, midPoint, arrowColor, pairAlpha);
                
                // 标记这对箭头已经绘制过
                drawnPairs.Add(pair);
                drawnPairs.Add(reversePair);
            }
            else
            {
                // 普通箭头：直接画到目标
                DrawArrow(start, end, arrowColor, alpha);
            }
        }
    }
    
    // 获取黄色边框的透明度（亮-灭切换）
    private float GetYellowBorderAlpha()
    {
        return (MathF.Sin(_yellowBorderTimer * MathF.PI * 2f / YELLOW_BORDER_CYCLE) + 1f) / 2f;
    }
    
    // 处理战斗日志滚动条
    private void CheckBattleLogScrollBar(MouseState currentMouseState)
    {
        if (_battleSystem == null || _currentGameState != GameState.Battle)
        {
            return;
        }
        
        // 日志区域的参数
        int logAreaLeftMargin = 5; // 窗口左边缘距离5像素
        int logAreaRightMargin = 5; // 滚动条左边缘距离5像素
        int scrollBarWidth = 8;
        int skillButtonStartY = 800; // 技能按钮开始的Y位置
        int logAreaY = skillButtonStartY;
        int logAreaHeight = _graphics.PreferredBackBufferHeight - logAreaY;
        
        // 滚动条位置 - 在按键部分的左侧
        int scrollBarX = _graphics.PreferredBackBufferWidth - 200; // 放在按键部分左侧，按键在800开始
        int logAreaWidth = scrollBarX - logAreaLeftMargin - logAreaRightMargin;
        int lineHeight = 18;
        int lineSpacing = lineHeight * 2; // 每两条日志中间间隔一行
        
        // 计算所有日志的总高度（需要考虑自动换行）
        float totalLogHeight = 0;
        float currentOffset = 0;
        
        for (int i = 0; i < _battleSystem.BattleLog.Count; i++)
        {
            string logEntry = _battleSystem.BattleLog[_battleSystem.BattleLog.Count - 1 - i];
            string cleanedLogEntry = CleanLogEntry(logEntry);
            
            // 计算自动换行后的行数
            SpriteFont font = IsChineseText(cleanedLogEntry) ? _chineseFont : _font;
            Vector2 textSize = font.MeasureString(cleanedLogEntry) * 0.65f;
            int numLines = (int)Math.Ceiling(textSize.X / logAreaWidth);
            if (numLines == 0) numLines = 1;
            
            float logHeight = numLines * lineHeight;
            totalLogHeight += logHeight;
            currentOffset += logHeight + lineSpacing; // 添加日志高度和行间距
        }
        
        // 计算最大滚动偏移
        float maxBattleLogScrollOffset = Math.Max(0, totalLogHeight - logAreaHeight);
        
        // 处理鼠标按下事件
        if (currentMouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
        {
            Rectangle logScrollBarRect = new Rectangle(scrollBarX, logAreaY, scrollBarWidth, logAreaHeight);
            if (logScrollBarRect.Contains(currentMouseState.Position))
            {
                _isDraggingBattleLogScrollBar = true;
            }
        }
        // 处理鼠标释放事件
        else if (currentMouseState.LeftButton == ButtonState.Released && _previousMouseState.LeftButton == ButtonState.Pressed)
        {
            _isDraggingBattleLogScrollBar = false;
        }
        // 处理鼠标拖动事件
        else if (currentMouseState.LeftButton == ButtonState.Pressed && _isDraggingBattleLogScrollBar)
        {
            float dragScrollRatio = (currentMouseState.Y - logAreaY) / (float)logAreaHeight;
            _battleLogScrollOffset = dragScrollRatio * maxBattleLogScrollOffset;
            _battleLogScrollOffset = Math.Max(0, Math.Min(maxBattleLogScrollOffset, _battleLogScrollOffset));
        }
    }
}
