using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TurnBasedRPG.Characters;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Tutorials;

// 教程阶段枚举
public enum TutorialStage
{
    NotStarted,
    Intro,
    Step1,
    Step2,
    Step3,
    Step4,
    Step5,
    Completed
}

// 高亮区域类型
public enum HighlightAreaType
{
    None,
    EncyclopediaButton,
    CharacterDetailButton,
    SkillDetailButton,
    ActionSlot,
    CharacterIcon,
    SkillButton,
    Custom
}

// 高亮区域信息
public class HighlightArea
{
    public HighlightAreaType Type { get; set; }
    public Rectangle Bounds { get; set; }
    public string HintText { get; set; }
    public bool WaitForClick { get; set; }
    public Action? OnClick { get; set; }
}

// 教程步骤
public class TutorialStep
{
    public string HintText { get; set; } = "1111";
    public HighlightArea? Highlight { get; set; }
    public Action? OnStart { get; set; }
    public Func<bool>? CheckComplete { get; set; }
}

public abstract class BaseTutorialLevel
{
    public int LevelNumber { get; protected set; }
    public string LevelName { get; protected set; } = "教程关卡";
    public TutorialStage CurrentStage { get; protected set; } = TutorialStage.NotStarted;
    public int CurrentStepIndex { get; protected set; } = 0;
    public List<TutorialStep> Steps { get; protected set; } = new List<TutorialStep>();
    public bool IsCompleted { get; protected set; } = false;
    
    protected SpriteBatch? _spriteBatch;
    protected SpriteFont? _font;
    protected SpriteFont? _chineseFont;
    protected Texture2D? _pixel;
    protected GraphicsDevice? _graphicsDevice;
    protected BattleSystem? _battleSystem;
    protected Game1? _game;

    protected BaseTutorialLevel(int levelNumber, string levelName)
    {
        LevelNumber = levelNumber;
        LevelName = levelName;
    }

    public virtual void Initialize(SpriteBatch spriteBatch, SpriteFont font, SpriteFont chineseFont, 
                                    Texture2D pixel, GraphicsDevice graphicsDevice, Game1 game)
    {
        _spriteBatch = spriteBatch;
        _font = font;
        _chineseFont = chineseFont;
        _pixel = pixel;
        _graphicsDevice = graphicsDevice;
        _game = game;
        InitializeSteps();
    }

    public virtual void SetBattleSystem(BattleSystem battleSystem)
    {
        _battleSystem = battleSystem;
    }

    protected abstract void InitializeSteps();

    public virtual void Start()
    {
        CurrentStage = TutorialStage.Intro;
        CurrentStepIndex = 0;
        IsCompleted = false;
        StartCurrentStep();
    }

    protected virtual void StartCurrentStep()
    {
        if (CurrentStepIndex < Steps.Count)
        {
            var step = Steps[CurrentStepIndex];
            step.OnStart?.Invoke();
        }
    }

    public virtual void Update(GameTime gameTime, MouseState currentMouse, MouseState previousMouse, 
                              KeyboardState currentKeyboard, KeyboardState previousKeyboard)
    {
        if (IsCompleted) return;
        
        if (CurrentStepIndex < Steps.Count)
        {
            var step = Steps[CurrentStepIndex];
            
            // 检查步骤是否完成
            if (step.CheckComplete != null && step.CheckComplete())
            {
                AdvanceToNextStep();
            }
        }
    }

    protected virtual void AdvanceToNextStep()
    {
        CurrentStepIndex++;
        if (CurrentStepIndex >= Steps.Count)
        {
            CompleteLevel();
        }
        else
        {
            StartCurrentStep();
        }
    }

    protected virtual void CompleteLevel()
    {
        IsCompleted = true;
        CurrentStage = TutorialStage.Completed;
    }

    public virtual void Draw()
    {
        if (_spriteBatch == null || _pixel == null) return;
        
        if (CurrentStepIndex < Steps.Count)
        {
            var step = Steps[CurrentStepIndex];
            
            // 绘制高亮和提示
            if (step.Highlight != null)
            {
                DrawHighlightAndHint(step.Highlight, step.HintText);
            }
        }
    }

    protected virtual void DrawHighlightAndHint(HighlightArea highlight, string hintText)
    {
        if (_spriteBatch == null || _pixel == null || _graphicsDevice == null) return;
        
        int windowWidth = _graphicsDevice.Viewport.Width;
        int windowHeight = _graphicsDevice.Viewport.Height;
        
        // 绘制半透明黑色遮罩（除了高亮区域）
        DrawMaskExceptArea(highlight.Bounds);
        
        // 绘制高亮区域边框
        _spriteBatch.Draw(_pixel, new Rectangle(highlight.Bounds.X - 2, highlight.Bounds.Y - 2, 
            highlight.Bounds.Width + 4, highlight.Bounds.Height + 4), Color.Yellow);
        
        // 确定提示文字的位置
        Vector2 hintPosition;
        SpriteFont hintFont = GetFontForText(hintText);
        Vector2 hintSize = hintFont.MeasureString(hintText) * 1.2f;
        
        // 如果高亮区域在屏幕底端，文字放在上方；否则放在下方
        if (highlight.Bounds.Bottom > windowHeight * 0.7f)
        {
            hintPosition = new Vector2(highlight.Bounds.Center.X, highlight.Bounds.Top - 20);
        }
        else
        {
            hintPosition = new Vector2(highlight.Bounds.Center.X, highlight.Bounds.Bottom + 20);
        }
        
        // 绘制白底红边的提示文字
        DrawHintText(hintText, hintPosition);
    }

    protected virtual void DrawMaskExceptArea(Rectangle exceptArea)
    {
        if (_spriteBatch == null || _pixel == null || _graphicsDevice == null) return;
        
        int windowWidth = _graphicsDevice.Viewport.Width;
        int windowHeight = _graphicsDevice.Viewport.Height;
        
        // 绘制四个区域的遮罩
        // 顶部
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, windowWidth, exceptArea.Top), 
            new Color(0, 0, 0, 180));
        // 底部
        _spriteBatch.Draw(_pixel, new Rectangle(0, exceptArea.Bottom, windowWidth, 
            windowHeight - exceptArea.Bottom), new Color(0, 0, 0, 180));
        // 左侧
        _spriteBatch.Draw(_pixel, new Rectangle(0, exceptArea.Top, exceptArea.Left, 
            exceptArea.Height), new Color(0, 0, 0, 180));
        // 右侧
        _spriteBatch.Draw(_pixel, new Rectangle(exceptArea.Right, exceptArea.Top, 
            windowWidth - exceptArea.Right, exceptArea.Height), new Color(0, 0, 0, 180));
    }

    protected virtual void DrawHintText(string text, Vector2 position)
    {
        if (_spriteBatch == null || _font == null || _chineseFont == null) return;
        
        SpriteFont hintFont = GetFontForText(text);
        Vector2 textSize = hintFont.MeasureString(text);
        
        // 计算背景大小（带 padding）
        int padding = 10;
        Vector2 backgroundSize = textSize * 1.2f + new Vector2(padding * 2, padding * 2);
        Vector2 backgroundPosition = position - backgroundSize / 2;
        
        // 绘制白色背景
        _spriteBatch.Draw(_pixel, new Rectangle((int)backgroundPosition.X, (int)backgroundPosition.Y, 
            (int)backgroundSize.X, (int)backgroundSize.Y), Color.White);
        
        // 绘制红色边框
        int borderThickness = 2;
        // 上边框
        _spriteBatch.Draw(_pixel, new Rectangle((int)backgroundPosition.X, (int)backgroundPosition.Y, 
            (int)backgroundSize.X, borderThickness), Color.Red);
        // 下边框
        _spriteBatch.Draw(_pixel, new Rectangle((int)backgroundPosition.X, 
            (int)(backgroundPosition.Y + backgroundSize.Y - borderThickness), 
            (int)backgroundSize.X, borderThickness), Color.Red);
        // 左边框
        _spriteBatch.Draw(_pixel, new Rectangle((int)backgroundPosition.X, (int)backgroundPosition.Y, 
            borderThickness, (int)backgroundSize.Y), Color.Red);
        // 右边框
        _spriteBatch.Draw(_pixel, new Rectangle(
            (int)(backgroundPosition.X + backgroundSize.X - borderThickness), 
            (int)backgroundPosition.Y, borderThickness, (int)backgroundSize.Y), Color.Red);
        
        // 绘制文字（稍大字号）
        Vector2 textPosition = position - (textSize * 1.2f) / 2;
        _spriteBatch.DrawString(hintFont, text, textPosition, Color.Black, 0f, 
            Vector2.Zero, 1.2f, SpriteEffects.None, 0f);
    }

    protected SpriteFont GetFontForText(string text)
    {
        // 简单的字体检测 - 和Game1.cs保持一致
        return IsChineseText(text) ? _chineseFont : _font;
    }

    protected bool IsChineseText(string text)
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

    // 获取战斗中的角色列表
    public virtual List<Character> GetPlayerCharacters()
    {
        return _battleSystem?.Players ?? new List<Character>();
    }

    public virtual List<Character> GetEnemyCharacters()
    {
        return _battleSystem?.Enemies ?? new List<Character>();
    }
}
