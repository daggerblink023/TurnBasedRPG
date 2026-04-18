using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TurnBasedRPG.Characters;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Tutorials;

public class TutorialManager
{
    private Dictionary<int, BaseTutorialLevel> _tutorialLevels = new Dictionary<int, BaseTutorialLevel>();
    private BaseTutorialLevel? _currentLevel = null;
    private TutorialProgress _progress;
    private bool _isInTutorial = false;
    private bool _showingLevelSelect = false;
    
    private SpriteBatch? _spriteBatch;
    private SpriteFont? _font;
    private SpriteFont? _chineseFont;
    private Texture2D? _pixel;
    private GraphicsDevice? _graphicsDevice;
    private BattleSystem? _battleSystem;
    private Game1? _game;

    public bool IsInTutorial => _isInTutorial;
    public bool ShowingLevelSelect => _showingLevelSelect;
    public TutorialProgress Progress => _progress;

    public TutorialManager()
    {
        _progress = new TutorialProgress();
    }

    public void Initialize(SpriteBatch spriteBatch, SpriteFont font, SpriteFont chineseFont, 
                          Texture2D pixel, GraphicsDevice graphicsDevice, Game1 game)
    {
        _spriteBatch = spriteBatch;
        _font = font;
        _chineseFont = chineseFont;
        _pixel = pixel;
        _graphicsDevice = graphicsDevice;
        _game = game;
        
        // 注册所有教程关卡（先留空，后续实现）
        RegisterTutorialLevels();
    }

    private void RegisterTutorialLevels()
    {
        // 暂时不注册具体关卡，后续实现
        // _tutorialLevels[1] = new Level1_InterfaceOverview();
        // _tutorialLevels[2] = new Level2_SkillShowdown();
        // _tutorialLevels[3] = new Level3_ShieldAndStatus();
        // _tutorialLevels[4] = new Level4_DiscardMechanic();
        // _tutorialLevels[5] = new Level5_ComprehensivePractice();
    }

    public void SetBattleSystem(BattleSystem battleSystem)
    {
        _battleSystem = battleSystem;
        if (_currentLevel != null)
        {
            _currentLevel.SetBattleSystem(battleSystem);
        }
    }

    public void ShowLevelSelect()
    {
        _showingLevelSelect = true;
        _isInTutorial = false;
        _currentLevel = null;
    }

    public void StartLevel(int levelNumber)
    {
        if (_tutorialLevels.ContainsKey(levelNumber) && _progress.IsLevelUnlocked(levelNumber))
        {
            _currentLevel = _tutorialLevels[levelNumber];
            _currentLevel.Initialize(_spriteBatch, _font, _chineseFont, _pixel, _graphicsDevice, _game);
            _currentLevel.SetBattleSystem(_battleSystem);
            _currentLevel.Start();
            _isInTutorial = true;
            _showingLevelSelect = false;
        }
    }

    public void Update(GameTime gameTime, MouseState currentMouse, MouseState previousMouse, 
                     KeyboardState currentKeyboard, KeyboardState previousKeyboard)
    {
        if (_isInTutorial && _currentLevel != null)
        {
            _currentLevel.Update(gameTime, currentMouse, previousMouse, currentKeyboard, previousKeyboard);
            
            // 检查关卡是否完成
            if (_currentLevel.IsCompleted)
            {
                CompleteLevel(_currentLevel.LevelNumber);
            }
        }
    }

    public void Draw()
    {
        if (_isInTutorial && _currentLevel != null)
        {
            _currentLevel.Draw();
        }
        else if (_showingLevelSelect)
        {
            DrawLevelSelect();
        }
    }

    private void DrawLevelSelect()
    {
        if (_spriteBatch == null || _pixel == null || _graphicsDevice == null || 
            _font == null || _chineseFont == null) return;
        
        int windowWidth = _graphicsDevice.Viewport.Width;
        int windowHeight = _graphicsDevice.Viewport.Height;
        
        // 绘制背景
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, windowWidth, windowHeight), 
            new Color(75, 75, 75, 255));
        
        // 绘制标题（黑底白边）- 只有文字有效果，背景与页面一致
        string title = "教程关卡选择";
        SpriteFont titleFont = GetFontForText(title);
        Vector2 titleSize = titleFont.MeasureString(title);
        Vector2 titlePosition = new Vector2(windowWidth / 2, 80);
        
        // 绘制标题白色轮廓（白边）
        _spriteBatch.DrawString(titleFont, title, titlePosition + new Vector2(-2, -2), Color.White, 0f, 
            titleSize / 2, 2.0f, SpriteEffects.None, 0f);
        _spriteBatch.DrawString(titleFont, title, titlePosition + new Vector2(2, -2), Color.White, 0f, 
            titleSize / 2, 2.0f, SpriteEffects.None, 0f);
        _spriteBatch.DrawString(titleFont, title, titlePosition + new Vector2(-2, 2), Color.White, 0f, 
            titleSize / 2, 2.0f, SpriteEffects.None, 0f);
        _spriteBatch.DrawString(titleFont, title, titlePosition + new Vector2(2, 2), Color.White, 0f, 
            titleSize / 2, 2.0f, SpriteEffects.None, 0f);
        
        // 绘制标题黑色文字（黑底）
        _spriteBatch.DrawString(titleFont, title, titlePosition, Color.Black, 0f, 
            titleSize / 2, 2.0f, SpriteEffects.None, 0f);
        
        // 绘制关卡按钮
        string[] levelNames = {
            "关卡1 战斗界面 百科与角色总览",
            "关卡2 技能拼点与技能详情",
            "关卡3 护盾攻防与状态详情",
            "关卡4 弃牌决断与深度信息查询",
            "关卡5 综合实战与自主信息规划"
        };
        
        int buttonWidth = 500;
        int buttonHeight = 60;
        int buttonSpacing = 20;
        int startY = 180;
        
        for (int i = 0; i < 5; i++)
        {
            int levelNumber = i + 1;
            bool isUnlocked = _progress.IsLevelUnlocked(levelNumber);
            bool isCompleted = _progress.IsLevelCompleted(levelNumber);
            
            int buttonX = (windowWidth - buttonWidth) / 2;
            int buttonY = startY + i * (buttonHeight + buttonSpacing);
            
            // 绘制按钮背景
            Color buttonColor;
            if (!isUnlocked)
            {
                buttonColor = Color.Gray;
            }
            else if (isCompleted)
            {
                buttonColor = new Color(150, 150, 255, 255);
            }
            else
            {
                buttonColor = new Color(75, 75, 255, 255);
            }
            
            _spriteBatch.Draw(_pixel, new Rectangle(buttonX, buttonY, buttonWidth, buttonHeight), buttonColor);
            
            // 绘制边框
            _spriteBatch.Draw(_pixel, new Rectangle(buttonX, buttonY, buttonWidth, 2), Color.White);
            _spriteBatch.Draw(_pixel, new Rectangle(buttonX, buttonY + buttonHeight - 2, buttonWidth, 2), Color.White);
            _spriteBatch.Draw(_pixel, new Rectangle(buttonX, buttonY, 2, buttonHeight), Color.White);
            _spriteBatch.Draw(_pixel, new Rectangle(buttonX + buttonWidth - 2, buttonY, 2, buttonHeight), Color.White);
            
            // 绘制关卡名称
            string levelName = levelNames[i];
            if (!isUnlocked)
            {
                levelName = "未解锁 " + levelName;
            }
            else if (isCompleted)
            {
                levelName = "已完成 " + levelName;
            }
            
            SpriteFont levelFont = GetFontForText(levelName);
            Vector2 levelSize = levelFont.MeasureString(levelName);
            Vector2 levelPosition = new Vector2(buttonX + buttonWidth / 2, buttonY + buttonHeight / 2);
            
            if (!isUnlocked)
            {
                // 未解锁按钮：黑底白边
                // 绘制文字白色轮廓（白边）
                _spriteBatch.DrawString(levelFont, levelName, levelPosition + new Vector2(-1, -1), Color.White, 0f, 
                    levelSize / 2, 1.0f, SpriteEffects.None, 0f);
                _spriteBatch.DrawString(levelFont, levelName, levelPosition + new Vector2(1, -1), Color.White, 0f, 
                    levelSize / 2, 1.0f, SpriteEffects.None, 0f);
                _spriteBatch.DrawString(levelFont, levelName, levelPosition + new Vector2(-1, 1), Color.White, 0f, 
                    levelSize / 2, 1.0f, SpriteEffects.None, 0f);
                _spriteBatch.DrawString(levelFont, levelName, levelPosition + new Vector2(1, 1), Color.White, 0f, 
                    levelSize / 2, 1.0f, SpriteEffects.None, 0f);
                
                // 绘制文字黑色（黑底）
                _spriteBatch.DrawString(levelFont, levelName, levelPosition, Color.Black, 0f, 
                    levelSize / 2, 1.0f, SpriteEffects.None, 0f);
            }
            else
            {
                // 已解锁和已完成按钮：红底白边
                // 绘制文字红色轮廓（红边）
                _spriteBatch.DrawString(levelFont, levelName, levelPosition + new Vector2(-1, -1), Color.Red, 0f, 
                    levelSize / 2, 1.0f, SpriteEffects.None, 0f);
                _spriteBatch.DrawString(levelFont, levelName, levelPosition + new Vector2(1, -1), Color.Red, 0f, 
                    levelSize / 2, 1.0f, SpriteEffects.None, 0f);
                _spriteBatch.DrawString(levelFont, levelName, levelPosition + new Vector2(-1, 1), Color.Red, 0f, 
                    levelSize / 2, 1.0f, SpriteEffects.None, 0f);
                _spriteBatch.DrawString(levelFont, levelName, levelPosition + new Vector2(1, 1), Color.Red, 0f, 
                    levelSize / 2, 1.0f, SpriteEffects.None, 0f);
                
                // 绘制文字白色（白字）
                _spriteBatch.DrawString(levelFont, levelName, levelPosition, Color.White, 0f, 
                    levelSize / 2, 1.0f, SpriteEffects.None, 0f);
            }
        }
        
        // 绘制返回按钮
        int closeButtonSize = 40;
        int closeButtonX = windowWidth - closeButtonSize - 20;
        int closeButtonY = 20;
        _spriteBatch.Draw(_pixel, new Rectangle(closeButtonX, closeButtonY, closeButtonSize, closeButtonSize), Color.Red);
        
        string closeText = "X";
        Vector2 closeTextSize = _font.MeasureString(closeText);
        _spriteBatch.DrawString(_font, closeText, 
            new Vector2(closeButtonX + closeButtonSize / 2 - closeTextSize.X / 2, 
                       closeButtonY + closeButtonSize / 2 - closeTextSize.Y / 2), 
            Color.White, 0, Vector2.Zero, 1.5f, SpriteEffects.None, 0);
    }

    public void CheckLevelSelectClick(MouseState currentMouse, MouseState previousMouse)
    {
        if (!_showingLevelSelect || _graphicsDevice == null) return;
        
        int windowWidth = _graphicsDevice.Viewport.Width;
        
        // 检查返回按钮点击
        int closeButtonSize = 40;
        int closeButtonX = windowWidth - closeButtonSize - 20;
        int closeButtonY = 20;
        
        if (currentMouse.LeftButton == ButtonState.Pressed && previousMouse.LeftButton == ButtonState.Released)
        {
            // 检查返回按钮
            if (IsPointInRectangle(currentMouse.X, currentMouse.Y, closeButtonX, closeButtonY, 
                closeButtonSize, closeButtonSize))
            {
                _showingLevelSelect = false;
                if (_game != null)
                {
                    _game.ReturnToMainMenuFromTutorial();
                }
                return;
            }
            
            // 检查关卡按钮点击
            int buttonWidth = 400;
            int buttonHeight = 60;
            int buttonSpacing = 20;
            int startY = 180;
            
            for (int i = 0; i < 5; i++)
            {
                int levelNumber = i + 1;
                int buttonX = (windowWidth - buttonWidth) / 2;
                int buttonY = startY + i * (buttonHeight + buttonSpacing);
                
                if (IsPointInRectangle(currentMouse.X, currentMouse.Y, buttonX, buttonY, 
                    buttonWidth, buttonHeight))
                {
                    StartLevel(levelNumber);
                    return;
                }
            }
        }
    }

    private bool IsPointInRectangle(int x, int y, int rectX, int rectY, int rectWidth, int rectHeight)
    {
        return x >= rectX && x <= rectX + rectWidth && y >= rectY && y <= rectY + rectHeight;
    }

    private void CompleteLevel(int levelNumber)
    {
        _progress.MarkLevelCompleted(levelNumber);
        _isInTutorial = false;
        _currentLevel = null;
        
        // 返回关卡选择界面
        _showingLevelSelect = true;
    }

    public void ExitTutorial()
    {
        _isInTutorial = false;
        _showingLevelSelect = false;
        _currentLevel = null;
    }

    private SpriteFont GetFontForText(string text)
    {
        // 简单的字体检测 - 和Game1.cs保持一致
        return IsChineseText(text) ? _chineseFont : _font;
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
}
