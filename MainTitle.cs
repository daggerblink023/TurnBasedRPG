using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TurnBasedRPG.Characters;

namespace TurnBasedRPG;

public class MainTitle
{
    private SpriteBatch _spriteBatch;
    private SpriteFont _font;
    private SpriteFont _chineseFont;
    private Texture2D _pixel;
    private MouseState _previousMouseState;
    
    private float _allyScrollOffset = 0.0f; // 己方角色滚动条偏移量
    private float _enemyScrollOffset = 0.0f; // 敌方角色滚动条偏移量
    private bool _isDraggingAllyScrollBar = false; // 是否正在拖动己方滚动条
    private bool _isDraggingEnemyScrollBar = false; // 是否正在拖动敌方滚动条
    private List<Character> _selectedAllies = new List<Character>(); // 已选择的己方角色列表
    private List<Character> _selectedEnemies = new List<Character>(); // 已选择的敌方角色列表
    
    // 行动槽数量选择相关变量
    private int _selectedSlotCount = 5; // 默认5个行动槽
    private float _slotScrollOffset = 0.0f; // 行动槽选择滚动条偏移量
    private bool _isDraggingSlotScrollBar = false; // 是否正在拖动行动槽滚动条
    
    public List<Character> SelectedAllies => _selectedAllies;
    public List<Character> SelectedEnemies => _selectedEnemies;
    public int SelectedSlotCount => _selectedSlotCount;
    
    public MainTitle(SpriteBatch spriteBatch, SpriteFont font, SpriteFont chineseFont, Texture2D pixel)
    {
        _spriteBatch = spriteBatch;
        _font = font;
        _chineseFont = chineseFont;
        _pixel = pixel;
    }
    
    public void Draw(int windowWidth, int windowHeight)
    {
        // 绘制标题（加大字号）
        string title = "幻想三国志";
        SpriteFont titleFont = GetFontForText(title);
        Vector2 titleSize = titleFont.MeasureString(title);
        // 确保标题水平居中，与双方角色选择窗口中间的缝隙对齐
        // 使用titleSize / 2作为原点偏移，这样XNA会自动处理缩放后的居中
        Vector2 titlePosition = new Vector2(windowWidth / 2, 40);
        _spriteBatch.DrawString(titleFont, title, titlePosition, Color.White, 0, titleSize / 2, 2.0f, SpriteEffects.None, 0);
        
        // 绘制副标题
        string subtitle = "请选择角色开始战斗";
        SpriteFont subtitleFont = GetFontForText(subtitle);
        Vector2 subtitleSize = subtitleFont.MeasureString(subtitle);
        Vector2 subtitlePosition = new Vector2((windowWidth - subtitleSize.X) / 2, 140);
        _spriteBatch.DrawString(subtitleFont, subtitle, subtitlePosition, Color.Gray);
        
        // 定义布局参数
        int halfWidth = windowWidth / 2;
        int columnPadding = 20;
        int scrollAreaWidth = halfWidth - columnPadding * 2;
        int scrollAreaHeight = 350;
        int scrollAreaY = 180;
        int iconSize = 80;
        int iconSpacing = 100;
        int scrollBarWidth = 10;
        
        // 绘制己方角色区域
        int allyScrollAreaX = columnPadding;
        
        // 绘制己方角色标题
        string allyTitle = "己方角色";
        SpriteFont allyTitleFont = GetFontForText(allyTitle);
        Vector2 allyTitleSize = allyTitleFont.MeasureString(allyTitle);
        Vector2 allyTitlePosition = new Vector2(allyScrollAreaX + (scrollAreaWidth - allyTitleSize.X) / 2, scrollAreaY - 40);
        _spriteBatch.DrawString(allyTitleFont, allyTitle, allyTitlePosition, Color.White);
        
        // 绘制己方角色滚动区域背景
        _spriteBatch.Draw(_pixel, new Rectangle(allyScrollAreaX, scrollAreaY, scrollAreaWidth, scrollAreaHeight), Color.DarkGray);
        
        // 绘制己方角色图标和名称（带滚动）
        Rectangle allyScissorRect = new Rectangle(allyScrollAreaX, scrollAreaY, scrollAreaWidth - scrollBarWidth, scrollAreaHeight);
        Rectangle originalScissorRect = _spriteBatch.GraphicsDevice.ScissorRectangle;
        _spriteBatch.GraphicsDevice.ScissorRectangle = allyScissorRect;
        
        // 开始一个新的spriteBatch批次，启用裁剪
        _spriteBatch.End();
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, new RasterizerState { ScissorTestEnable = true });
        
        // 己方角色数据
        var allyCharacterData = new List<(string Name, Func<Character> Create)> {
            ("示例角色1", () => new Characters.Allies.示例角色1()),
            ("夏侯惇", () => new Characters.Allies.夏侯惇()),
            ("曹仁", () => new Characters.Allies.曹仁()),
            ("司马懿", () => new Characters.Allies.司马懿()),
            ("曹丕", () => new Characters.Allies.曹丕()),
            ("张辽", () => new Characters.Allies.张辽()),
            ("曹操", () => new Characters.Allies.曹操())
        };
        
        // 排序己方角色：示例角色固定在顶部，然后按势力优先级，同一势力按名称排序
        var sortedAllyCharacters = allyCharacterData
            .OrderBy(c => c.Name.StartsWith("示例") ? 0 : 1) // 示例角色在顶部
            .ThenBy(c => {
                var chara = c.Create();
                switch (chara.Faction)
                {
                    case Faction.魏: return 0;
                    case Faction.蜀: return 1;
                    case Faction.吴: return 2;
                    case Faction.群: return 3;
                    default: return 4;
                }
            })
            .ThenBy(c => c.Name)
            .ToList();
        
        // 计算每个阵营的角色，并按阵营分组
        var groupedAllyCharacters = new Dictionary<Faction, List<(string Name, Func<Character> Create)>>();
        groupedAllyCharacters[Faction.无] = new List<(string, Func<Character>)>();
        groupedAllyCharacters[Faction.魏] = new List<(string, Func<Character>)>();
        groupedAllyCharacters[Faction.蜀] = new List<(string, Func<Character>)>();
        groupedAllyCharacters[Faction.吴] = new List<(string, Func<Character>)>();
        groupedAllyCharacters[Faction.群] = new List<(string, Func<Character>)>();
        
        foreach (var charaData in sortedAllyCharacters)
        {
            var chara = charaData.Create();
            groupedAllyCharacters[chara.Faction].Add(charaData);
        }
        
        // 按照阵营顺序绘制，每行三个角色，同时计算总高度
        float currentY = scrollAreaY + 20;
        float allyTotalHeight = currentY;
        int charsPerRow = 3;
        float rowSpacing = 100; // 两行之间的间距
        float factionTitleHeight = 25; // 阵营标题的高度
        float factionSpacing = 20; // 两个阵营之间的间距
        
        foreach (var faction in new[] { Faction.无, Faction.魏, Faction.蜀, Faction.吴, Faction.群 })
        {
            var characters = groupedAllyCharacters[faction];
            if (characters.Count == 0)
            {
                continue;
            }
            
            // 绘制阵营标题
            if (faction != Faction.无)
            {
                string factionTitle = faction.ToString();
                SpriteFont factionTitleFont = GetFontForText(factionTitle);
                Vector2 factionTitleSize = factionTitleFont.MeasureString(factionTitle);
                _spriteBatch.DrawString(factionTitleFont, factionTitle, 
                    new Vector2(allyScrollAreaX + 20, currentY - (int)_allyScrollOffset), 
                    Color.White, 0, Vector2.Zero, 1.0f, SpriteEffects.None, 0);
                currentY += factionTitleHeight;
                allyTotalHeight += factionTitleHeight;
            }
            
            // 绘制该阵营的角色
            for (int i = 0; i < characters.Count; i++)
            {
                int row = i / charsPerRow;
                int col = i % charsPerRow;
                
                // 计算内容区域：左边缘的右侧20像素，滚动条左边缘的左侧20像素
                int contentLeft = allyScrollAreaX + 20;
                int contentRight = allyScrollAreaX + scrollAreaWidth - scrollBarWidth - 20;
                int contentWidth = contentRight - contentLeft;
                
                // 三等分内容区域
                int segmentWidth = contentWidth / 3;
                int iconX = contentLeft + col * segmentWidth;
                int iconY = (int)(currentY + row * rowSpacing - _allyScrollOffset);
                
                // 获取角色信息
                var (name, createFunc) = characters[i];
                Character currentCharacter = createFunc();
                
                // 确定头像颜色
                Color iconColor;
                switch (currentCharacter.Faction)
                {
                    case Faction.魏:
                        iconColor = Color.Blue;
                        break;
                    case Faction.蜀:
                        iconColor = Color.Green;
                        break;
                    case Faction.吴:
                        iconColor = Color.Red;
                        break;
                    case Faction.群:
                        iconColor = Color.Purple;
                        break;
                    default:
                        iconColor = Color.Black;
                        break;
                }
                
                // 绘制角色图标
                _spriteBatch.Draw(_pixel, new Rectangle(iconX, iconY, iconSize, iconSize), iconColor);
                
                // 检查该角色是否被选中
                int selectedIndex = _selectedAllies.FindIndex(c => c.Name == currentCharacter.Name);
                if (selectedIndex >= 0)
                {
                    // 绘制白色描边
                    _spriteBatch.Draw(_pixel, new Rectangle(iconX - 2, iconY - 2, iconSize + 4, 2), Color.White);
                    _spriteBatch.Draw(_pixel, new Rectangle(iconX - 2, iconY + iconSize, iconSize + 4, 2), Color.White);
                    _spriteBatch.Draw(_pixel, new Rectangle(iconX - 2, iconY - 2, 2, iconSize + 4), Color.White);
                    _spriteBatch.Draw(_pixel, new Rectangle(iconX + iconSize, iconY - 2, 2, iconSize + 4), Color.White);
                    
                    // 绘制选择序号
                    string indexText = (selectedIndex + 1).ToString();
                    SpriteFont indexFont = GetFontForText(indexText);
                    Vector2 indexSize = indexFont.MeasureString(indexText);
                    _spriteBatch.DrawString(indexFont, indexText, new Vector2(iconX + (iconSize - indexSize.X) / 2, iconY + (iconSize - indexSize.Y) / 2), Color.White);
                }
                
                // 绘制角色名称（在头像右侧）
                string characterName = name;
                SpriteFont nameFont = GetFontForText(characterName);
                Vector2 nameSize = nameFont.MeasureString(characterName);
                _spriteBatch.DrawString(nameFont, characterName, new Vector2(iconX + iconSize + 10, iconY + (iconSize - nameSize.Y) / 2), Color.White);
            }
            
            // 更新当前Y位置
            int numRows = (characters.Count + charsPerRow - 1) / charsPerRow;
            currentY += numRows * rowSpacing + factionSpacing;
            allyTotalHeight += numRows * rowSpacing + factionSpacing;
        }
        
        // 结束spriteBatch批次并恢复原始裁剪区域
        _spriteBatch.End();
        _spriteBatch.GraphicsDevice.ScissorRectangle = originalScissorRect;
        _spriteBatch.Begin();
        
        // 绘制己方角色滚动条 - 深灰色为底色，白色为滑块
        int allyScrollBarX = allyScrollAreaX + scrollAreaWidth - scrollBarWidth;
        int allyScrollBarY = scrollAreaY;
        int allyScrollBarHeight = scrollAreaHeight;
        _spriteBatch.Draw(_pixel, new Rectangle(allyScrollBarX, allyScrollBarY, scrollBarWidth, allyScrollBarHeight), Color.DarkGray);
        
        // 计算己方滚动条滑块位置
        float maxAllyScrollOffset = Math.Max(0, allyTotalHeight - scrollAreaY - scrollAreaHeight);
        float allyScrollRatio = maxAllyScrollOffset > 0 ? _allyScrollOffset / maxAllyScrollOffset : 0;
        float allyHeightRatio = scrollAreaHeight / (float)Math.Max(scrollAreaHeight, allyTotalHeight - scrollAreaY);
        int allySliderHeight = Math.Max(20, (int)(allyScrollBarHeight * allyHeightRatio));
        int allySliderY = allyScrollBarY + (int)(allyScrollRatio * (allyScrollBarHeight - allySliderHeight));
        _spriteBatch.Draw(_pixel, new Rectangle(allyScrollBarX, allySliderY, scrollBarWidth, allySliderHeight), Color.White);
        
        // 绘制敌方角色区域
        int enemyScrollAreaX = halfWidth + columnPadding;
        
        // 绘制敌方角色标题
        string enemyTitle = "敌方角色";
        SpriteFont enemyTitleFont = GetFontForText(enemyTitle);
        Vector2 enemyTitleSize = enemyTitleFont.MeasureString(enemyTitle);
        Vector2 enemyTitlePosition = new Vector2(enemyScrollAreaX + (scrollAreaWidth - enemyTitleSize.X) / 2, scrollAreaY - 40);
        _spriteBatch.DrawString(enemyTitleFont, enemyTitle, enemyTitlePosition, Color.White);
        
        // 绘制敌方角色滚动区域背景
        _spriteBatch.Draw(_pixel, new Rectangle(enemyScrollAreaX, scrollAreaY, scrollAreaWidth, scrollAreaHeight), Color.DarkGray);
        
        // 绘制敌方角色图标和名称（带滚动）
        Rectangle enemyScissorRect = new Rectangle(enemyScrollAreaX, scrollAreaY, scrollAreaWidth - scrollBarWidth, scrollAreaHeight);
        _spriteBatch.GraphicsDevice.ScissorRectangle = enemyScissorRect;
        
        // 开始一个新的spriteBatch批次，启用裁剪
        _spriteBatch.End();
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, new RasterizerState { ScissorTestEnable = true });
        
        // 敌方角色数据
        var enemyCharacterData = new List<(string Name, Func<Character> Create)> {
            ("示例敌怪1", () => new Characters.Enemies.示例敌怪1()),
            ("夏侯惇", () => new Characters.Allies.夏侯惇(true, false)),
            ("曹仁", () => new Characters.Allies.曹仁(true, false)),
            ("司马懿", () => new Characters.Allies.司马懿(true, false)),
            ("曹丕", () => new Characters.Allies.曹丕(true, false)),
            ("张辽", () => new Characters.Allies.张辽(true, false)),
            ("曹操", () => new Characters.Allies.曹操(true, false))
        };
        
        // 排序敌方角色：示例敌怪固定在顶部，然后按势力优先级，同一势力按名称排序
        var sortedEnemyCharacters = enemyCharacterData
            .OrderBy(c => c.Name.StartsWith("示例") ? 0 : 1) // 示例敌怪在顶部
            .ThenBy(c => {
                var chara = c.Create();
                switch (chara.Faction)
                {
                    case Faction.魏: return 0;
                    case Faction.蜀: return 1;
                    case Faction.吴: return 2;
                    case Faction.群: return 3;
                    default: return 4;
                }
            })
            .ThenBy(c => c.Name)
            .ToList();
        
        // 计算每个阵营的角色，并按阵营分组
        var groupedEnemyCharacters = new Dictionary<Faction, List<(string Name, Func<Character> Create)>>();
        groupedEnemyCharacters[Faction.无] = new List<(string, Func<Character>)>();
        groupedEnemyCharacters[Faction.魏] = new List<(string, Func<Character>)>();
        groupedEnemyCharacters[Faction.蜀] = new List<(string, Func<Character>)>();
        groupedEnemyCharacters[Faction.吴] = new List<(string, Func<Character>)>();
        groupedEnemyCharacters[Faction.群] = new List<(string, Func<Character>)>();
        
        foreach (var charaData in sortedEnemyCharacters)
        {
            var chara = charaData.Create();
            groupedEnemyCharacters[chara.Faction].Add(charaData);
        }
        
        // 按照阵营顺序绘制，每行三个角色，同时计算总高度
        float enemyCurrentY = scrollAreaY + 20;
        float enemyTotalHeight = enemyCurrentY;
        int enemyCharsPerRow = 3;
        float enemyRowSpacing = 100; // 两行之间的间距
        float enemyFactionTitleHeight = 25; // 阵营标题的高度
        float enemyFactionSpacing = 20; // 两个阵营之间的间距
        
        foreach (var faction in new[] { Faction.无, Faction.魏, Faction.蜀, Faction.吴, Faction.群 })
        {
            var characters = groupedEnemyCharacters[faction];
            if (characters.Count == 0)
            {
                continue;
            }
            
            // 绘制阵营标题
            if (faction != Faction.无)
            {
                string factionTitle = faction.ToString();
                SpriteFont factionTitleFont = GetFontForText(factionTitle);
                Vector2 factionTitleSize = factionTitleFont.MeasureString(factionTitle);
                _spriteBatch.DrawString(factionTitleFont, factionTitle, 
                    new Vector2(enemyScrollAreaX + 20, enemyCurrentY - (int)_enemyScrollOffset), 
                    Color.White, 0, Vector2.Zero, 1.0f, SpriteEffects.None, 0);
                enemyCurrentY += enemyFactionTitleHeight;
                enemyTotalHeight += enemyFactionTitleHeight;
            }
            
            // 绘制该阵营的角色
            for (int i = 0; i < characters.Count; i++)
            {
                int row = i / enemyCharsPerRow;
                int col = i % enemyCharsPerRow;
                
                // 计算内容区域：左边缘的右侧20像素，滚动条左边缘的左侧20像素
                int contentLeft = enemyScrollAreaX + 20;
                int contentRight = enemyScrollAreaX + scrollAreaWidth - scrollBarWidth - 20;
                int contentWidth = contentRight - contentLeft;
                
                // 三等分内容区域
                int segmentWidth = contentWidth / 3;
                int iconX = contentLeft + col * segmentWidth;
                int iconY = (int)(enemyCurrentY + row * enemyRowSpacing - _enemyScrollOffset);
                
                // 获取角色信息
                var (name, createFunc) = characters[i];
                Character currentCharacter = createFunc();
                
                // 确定头像颜色
                Color iconColor;
                switch (currentCharacter.Faction)
                {
                    case Faction.魏:
                        iconColor = Color.Blue;
                        break;
                    case Faction.蜀:
                        iconColor = Color.Green;
                        break;
                    case Faction.吴:
                        iconColor = Color.Red;
                        break;
                    case Faction.群:
                        iconColor = Color.Purple;
                        break;
                    default:
                        iconColor = Color.Black;
                        break;
                }
                
                // 绘制角色图标
                _spriteBatch.Draw(_pixel, new Rectangle(iconX, iconY, iconSize, iconSize), iconColor);
                
                // 检查该角色是否被选中
                int selectedIndex = _selectedEnemies.FindIndex(c => c.Name == currentCharacter.Name);
                if (selectedIndex >= 0)
                {
                    // 绘制白色描边
                    _spriteBatch.Draw(_pixel, new Rectangle(iconX - 2, iconY - 2, iconSize + 4, 2), Color.White);
                    _spriteBatch.Draw(_pixel, new Rectangle(iconX - 2, iconY + iconSize, iconSize + 4, 2), Color.White);
                    _spriteBatch.Draw(_pixel, new Rectangle(iconX - 2, iconY - 2, 2, iconSize + 4), Color.White);
                    _spriteBatch.Draw(_pixel, new Rectangle(iconX + iconSize, iconY - 2, 2, iconSize + 4), Color.White);
                    
                    // 绘制选择序号
                    string indexText = (selectedIndex + 1).ToString();
                    SpriteFont indexFont = GetFontForText(indexText);
                    Vector2 indexSize = indexFont.MeasureString(indexText);
                    _spriteBatch.DrawString(indexFont, indexText, new Vector2(iconX + (iconSize - indexSize.X) / 2, iconY + (iconSize - indexSize.Y) / 2), Color.White);
                }
                
                // 绘制角色名称（在头像右侧）
                string characterName = name;
                SpriteFont nameFont = GetFontForText(characterName);
                Vector2 nameSize = nameFont.MeasureString(characterName);
                _spriteBatch.DrawString(nameFont, characterName, new Vector2(iconX + iconSize + 10, iconY + (iconSize - nameSize.Y) / 2), Color.White);
            }
            
            // 更新当前Y位置
            int numRows = (characters.Count + enemyCharsPerRow - 1) / enemyCharsPerRow;
            enemyCurrentY += numRows * enemyRowSpacing + enemyFactionSpacing;
            enemyTotalHeight += numRows * enemyRowSpacing + enemyFactionSpacing;
        }
        
        // 结束spriteBatch批次并恢复原始裁剪区域
        _spriteBatch.End();
        _spriteBatch.GraphicsDevice.ScissorRectangle = originalScissorRect;
        _spriteBatch.Begin();
        
        // 绘制敌方角色滚动条 - 深灰色为底色，白色为滑块
        int enemyScrollBarX = enemyScrollAreaX + scrollAreaWidth - scrollBarWidth;
        int enemyScrollBarY = scrollAreaY;
        int enemyScrollBarHeight = scrollAreaHeight;
        _spriteBatch.Draw(_pixel, new Rectangle(enemyScrollBarX, enemyScrollBarY, scrollBarWidth, enemyScrollBarHeight), Color.DarkGray);
        
        // 计算敌方滚动条滑块位置
        float maxEnemyScrollOffset = Math.Max(0, enemyTotalHeight - scrollAreaY - scrollAreaHeight);
        float enemyScrollRatio = maxEnemyScrollOffset > 0 ? _enemyScrollOffset / maxEnemyScrollOffset : 0;
        float enemyHeightRatio = scrollAreaHeight / (float)Math.Max(scrollAreaHeight, enemyTotalHeight - scrollAreaY);
        int enemySliderHeight = Math.Max(20, (int)(enemyScrollBarHeight * enemyHeightRatio));
        int enemySliderY = enemyScrollBarY + (int)(enemyScrollRatio * (enemyScrollBarHeight - enemySliderHeight));
        _spriteBatch.Draw(_pixel, new Rectangle(enemyScrollBarX, enemySliderY, scrollBarWidth, enemySliderHeight), Color.White);
        
        // 开始战斗按钮
        int buttonWidth = 200;
        int buttonHeight = 50;
        _spriteBatch.Draw(_pixel, new Rectangle((windowWidth - buttonWidth) / 2, scrollAreaY + scrollAreaHeight + 40, buttonWidth, buttonHeight), Color.Green);
        string startText = "开始战斗";
        SpriteFont startFont = GetFontForText(startText);
        Vector2 startTextSize = startFont.MeasureString(startText);
        _spriteBatch.DrawString(startFont, startText, new Vector2((windowWidth - buttonWidth) / 2 + (buttonWidth - startTextSize.X) / 2, scrollAreaY + scrollAreaHeight + 40 + (buttonHeight - startTextSize.Y) / 2), Color.White);
        
        // 行动槽数量选择
        string slotTitle = "行动槽数量";
        SpriteFont slotTitleFont = GetFontForText(slotTitle);
        Vector2 slotTitleSize = slotTitleFont.MeasureString(slotTitle);
        Vector2 slotTitlePosition = new Vector2((windowWidth - slotTitleSize.X) / 2, scrollAreaY + scrollAreaHeight + buttonHeight + 60);
        _spriteBatch.DrawString(slotTitleFont, slotTitle, slotTitlePosition, Color.White);
        
        // 行动槽选择滚动区域
        int slotScrollAreaWidth = 600;
        int slotScrollAreaHeight = 80;
        int slotScrollAreaX = (windowWidth - slotScrollAreaWidth) / 2;
        int slotScrollAreaY = (int)(slotTitlePosition.Y + slotTitleSize.Y + 20);
        _spriteBatch.Draw(_pixel, new Rectangle(slotScrollAreaX, slotScrollAreaY, slotScrollAreaWidth, slotScrollAreaHeight), Color.DarkGray);
        
        // 绘制行动槽选择按钮（带滚动）
        Rectangle slotScissorRect = new Rectangle(slotScrollAreaX, slotScrollAreaY, slotScrollAreaWidth - 10, slotScrollAreaHeight);
        Rectangle originalScissorRectForSlots = _spriteBatch.GraphicsDevice.ScissorRectangle;
        _spriteBatch.GraphicsDevice.ScissorRectangle = slotScissorRect;
        
        // 开始一个新的spriteBatch批次，启用裁剪
        _spriteBatch.End();
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, new RasterizerState { ScissorTestEnable = true });
        
        // 行动槽数量选项
        int[] slotOptions = { 5, 10, 15, 20 };
        int slotButtonWidth = 120;
        int slotButtonHeight = 50;
        int slotButtonSpacing = 20;
        
        // 计算最多可以选择的行动槽数量
        int selectedAllyCount = _selectedAllies.Count > 0 ? _selectedAllies.Count : 1; // 未选择时视为1名
        int selectedEnemyCount = _selectedEnemies.Count > 0 ? _selectedEnemies.Count : 1; // 未选择时视为1名
        int minCharacterCount = Math.Min(selectedAllyCount, selectedEnemyCount);
        
        int maxSlotCount;
        if (selectedAllyCount == 1 && selectedEnemyCount == 1)
        {
            // 双方都只选取1名角色时，行动槽的选取数量不受限制
            maxSlotCount = 20; // 默认最大20
        }
        else
        {
            // 至少有一方选取了不低于2名角色时，最多只能选择（人数最少一方的人数x4）个行动槽
            maxSlotCount = minCharacterCount * 4;
        }
        
        for (int i = 0; i < slotOptions.Length; i++)
        {
            int buttonX = slotScrollAreaX + 20 + i * (slotButtonWidth + slotButtonSpacing) - (int)_slotScrollOffset;
            int buttonY = slotScrollAreaY + 15;
            
            // 检查是否超过最大限制
            bool isDisabled = slotOptions[i] > maxSlotCount;
            
            // 绘制按钮背景
            if (slotOptions[i] == _selectedSlotCount)
            {
                // 选中状态
                _spriteBatch.Draw(_pixel, new Rectangle(buttonX, buttonY, slotButtonWidth, slotButtonHeight), Color.DarkGreen);
            }
            else if (isDisabled)
            {
                // 禁用状态
                _spriteBatch.Draw(_pixel, new Rectangle(buttonX, buttonY, slotButtonWidth, slotButtonHeight), Color.DarkGray);
            }
            else
            {
                // 未选中状态
                _spriteBatch.Draw(_pixel, new Rectangle(buttonX, buttonY, slotButtonWidth, slotButtonHeight), Color.Black);
            }
            
            // 绘制按钮文字
            string buttonText = $"{slotOptions[i]}个行动槽";
            SpriteFont buttonFont = GetFontForText(buttonText);
            Vector2 buttonTextSize = buttonFont.MeasureString(buttonText);
            Color textColor = isDisabled ? Color.Gray : Color.White;
            _spriteBatch.DrawString(buttonFont, buttonText, new Vector2(buttonX + (slotButtonWidth - buttonTextSize.X) / 2, buttonY + (slotButtonHeight - buttonTextSize.Y) / 2), textColor);
        }        
        // 结束spriteBatch批次并恢复原始裁剪区域
        _spriteBatch.End();
        _spriteBatch.GraphicsDevice.ScissorRectangle = originalScissorRectForSlots;
        _spriteBatch.Begin();
        
        // 绘制行动槽选择滚动条
        int slotScrollBarX = slotScrollAreaX + slotScrollAreaWidth - 10;
        int slotScrollBarY = slotScrollAreaY;
        int slotScrollBarHeight = slotScrollAreaHeight;
        _spriteBatch.Draw(_pixel, new Rectangle(slotScrollBarX, slotScrollBarY, 10, slotScrollBarHeight), Color.Gray);
        
        // 计算行动槽滚动条滑块位置
        float maxSlotScrollOffset = Math.Max(0, (slotOptions.Length * (slotButtonWidth + slotButtonSpacing)) - slotScrollAreaWidth + 40);
        float slotScrollRatio = maxSlotScrollOffset > 0 ? _slotScrollOffset / maxSlotScrollOffset : 0;
        int maxHeight = Math.Max(slotScrollAreaHeight, slotOptions.Length * (slotButtonWidth + slotButtonSpacing) + 40);
        float heightRatio = slotScrollAreaHeight / (float)maxHeight;
        int slotSliderHeight = Math.Max(20, (int)(slotScrollBarHeight * heightRatio));
        int slotSliderY = slotScrollBarY + (int)(slotScrollRatio * (slotScrollBarHeight - slotSliderHeight));
        _spriteBatch.Draw(_pixel, new Rectangle(slotScrollBarX, slotSliderY, 10, slotSliderHeight), Color.LightGray);
    }
    
    public bool CheckClick(MouseState currentMouseState, int windowWidth, int windowHeight)
    {
        // 定义布局参数
        int halfWidth = windowWidth / 2;
        int columnPadding = 20;
        int scrollAreaWidth = halfWidth - columnPadding * 2;
        int scrollAreaHeight = 350;
        int scrollAreaY = 180;
        int iconSize = 80;
        int iconSpacing = 100;
        int scrollBarWidth = 10;
        
        // 己方角色区域
        int allyScrollAreaX = columnPadding;
        int allyScrollBarX = allyScrollAreaX + scrollAreaWidth - scrollBarWidth;
        
        // 敌方角色区域
        int enemyScrollAreaX = halfWidth + columnPadding;
        int enemyScrollBarX = enemyScrollAreaX + scrollAreaWidth - scrollBarWidth;
        
        // 处理鼠标按下事件
        if (currentMouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
        {
            // 检查己方角色图标点击
            var allyCharacterData = new List<(string Name, Func<Character> Create)> {
                ("示例角色1", () => new Characters.Allies.示例角色1()),
                ("夏侯惇", () => new Characters.Allies.夏侯惇()),
                ("曹仁", () => new Characters.Allies.曹仁()),
                ("司马懿", () => new Characters.Allies.司马懿()),
                ("曹丕", () => new Characters.Allies.曹丕()),
                ("张辽", () => new Characters.Allies.张辽()),
                ("曹操", () => new Characters.Allies.曹操())
            };
            
            // 排序己方角色：示例角色固定在顶部，然后按势力优先级，同一势力按名称排序
            var sortedAllyCharacters = allyCharacterData
                .OrderBy(c => c.Name.StartsWith("示例") ? 0 : 1) // 示例角色在顶部
                .ThenBy(c => {
                    var chara = c.Create();
                    switch (chara.Faction)
                    {
                        case Faction.魏: return 0;
                        case Faction.蜀: return 1;
                        case Faction.吴: return 2;
                        case Faction.群: return 3;
                        default: return 4;
                    }
                })
                .ThenBy(c => c.Name)
                .ToList();
            
            // 计算每个阵营的角色，并按阵营分组
            var groupedAllyCharacters = new Dictionary<Faction, List<(string Name, Func<Character> Create)>>();
            groupedAllyCharacters[Faction.无] = new List<(string, Func<Character>)>();
            groupedAllyCharacters[Faction.魏] = new List<(string, Func<Character>)>();
            groupedAllyCharacters[Faction.蜀] = new List<(string, Func<Character>)>();
            groupedAllyCharacters[Faction.吴] = new List<(string, Func<Character>)>();
            groupedAllyCharacters[Faction.群] = new List<(string, Func<Character>)>();
            
            foreach (var charaData in sortedAllyCharacters)
            {
                var chara = charaData.Create();
                groupedAllyCharacters[chara.Faction].Add(charaData);
            }
            
            // 按照阵营顺序检查，每行三个角色
            float currentY = scrollAreaY + 20;
            int charsPerRow = 3;
            float rowSpacing = 100; // 两行之间的间距
            float factionTitleHeight = 25; // 阵营标题的高度
            float factionSpacing = 20; // 两个阵营之间的间距
            
            foreach (var faction in new[] { Faction.无, Faction.魏, Faction.蜀, Faction.吴, Faction.群 })
            {
                var characters = groupedAllyCharacters[faction];
                if (characters.Count == 0)
                {
                    continue;
                }
                
                // 跳过阵营标题
                if (faction != Faction.无)
                {
                    currentY += factionTitleHeight;
                }
                
                // 检查该阵营的角色
                for (int i = 0; i < characters.Count; i++)
                {
                    int row = i / charsPerRow;
                    int col = i % charsPerRow;
                    
                    // 计算内容区域：左边缘的右侧20像素，滚动条左边缘的左侧20像素
                    int contentLeft = allyScrollAreaX + 20;
                    int contentRight = allyScrollAreaX + scrollAreaWidth - scrollBarWidth - 20;
                    int contentWidth = contentRight - contentLeft;
                    
                    // 三等分内容区域
                    int segmentWidth = contentWidth / 3;
                    int iconX = contentLeft + col * segmentWidth;
                    int iconY = (int)(currentY + row * rowSpacing - _allyScrollOffset);
                    
                    if (IsPointInRectangle(currentMouseState.X, currentMouseState.Y, iconX, iconY, iconSize, iconSize))
                    {
                        // 选择对应的我方角色
                        var (name, createFunc) = characters[i];
                        Character selectedCharacter = createFunc();
                        
                        // 检查是否已经选择了这个角色
                        int existingIndex = _selectedAllies.FindIndex(c => c.Name == selectedCharacter.Name);
                        if (existingIndex >= 0)
                        {
                            // 取消选择
                            _selectedAllies.RemoveAt(existingIndex);
                        }
                        else
                        {
                            // 添加选择，限制选择数量不超过5个
                            if (_selectedAllies.Count < 5)
                            {
                                _selectedAllies.Add(selectedCharacter);
                            }
                        }
                        
                        _previousMouseState = currentMouseState;
                        return false;
                    }
                }
                
                // 更新当前Y位置
                int numRows = (characters.Count + charsPerRow - 1) / charsPerRow;
                currentY += numRows * rowSpacing + factionSpacing;
            }
            
            // 检查敌方角色图标点击
            var enemyCharacterData = new List<(string Name, Func<Character> Create)> {
                ("示例敌怪1", () => new Characters.Enemies.示例敌怪1()),
                ("夏侯惇", () => new Characters.Allies.夏侯惇(true, false)),
                ("曹仁", () => new Characters.Allies.曹仁(true, false)),
                ("司马懿", () => new Characters.Allies.司马懿(true, false)),
                ("曹丕", () => new Characters.Allies.曹丕(true, false)),
                ("张辽", () => new Characters.Allies.张辽(true, false)),
                ("曹操", () => new Characters.Allies.曹操(true, false))
            };
            
            // 排序敌方角色：示例敌怪固定在顶部，然后按势力优先级，同一势力按名称排序
            var sortedEnemyCharacters = enemyCharacterData
                .OrderBy(c => c.Name.StartsWith("示例") ? 0 : 1) // 示例敌怪在顶部
                .ThenBy(c => {
                    var chara = c.Create();
                    switch (chara.Faction)
                    {
                        case Faction.魏: return 0;
                        case Faction.蜀: return 1;
                        case Faction.吴: return 2;
                        case Faction.群: return 3;
                        default: return 4;
                    }
                })
                .ThenBy(c => c.Name)
                .ToList();
            
            // 计算每个阵营的角色，并按阵营分组
            var groupedEnemyCharacters = new Dictionary<Faction, List<(string Name, Func<Character> Create)>>();
            groupedEnemyCharacters[Faction.无] = new List<(string, Func<Character>)>();
            groupedEnemyCharacters[Faction.魏] = new List<(string, Func<Character>)>();
            groupedEnemyCharacters[Faction.蜀] = new List<(string, Func<Character>)>();
            groupedEnemyCharacters[Faction.吴] = new List<(string, Func<Character>)>();
            groupedEnemyCharacters[Faction.群] = new List<(string, Func<Character>)>();
            
            foreach (var charaData in sortedEnemyCharacters)
            {
                var chara = charaData.Create();
                groupedEnemyCharacters[chara.Faction].Add(charaData);
            }
            
            // 按照阵营顺序检查，每行三个角色
            float enemyCurrentY = scrollAreaY + 20;
            int enemyCharsPerRow = 3;
            float enemyRowSpacing = 100; // 两行之间的间距
            float enemyFactionTitleHeight = 25; // 阵营标题的高度
            float enemyFactionSpacing = 20; // 两个阵营之间的间距
            
            foreach (var faction in new[] { Faction.无, Faction.魏, Faction.蜀, Faction.吴, Faction.群 })
            {
                var characters = groupedEnemyCharacters[faction];
                if (characters.Count == 0)
                {
                    continue;
                }
                
                // 跳过阵营标题
                if (faction != Faction.无)
                {
                    enemyCurrentY += enemyFactionTitleHeight;
                }
                
                // 检查该阵营的角色
                for (int i = 0; i < characters.Count; i++)
                {
                    int row = i / enemyCharsPerRow;
                    int col = i % enemyCharsPerRow;
                    
                    // 计算内容区域：左边缘的右侧20像素，滚动条左边缘的左侧20像素
                    int contentLeft = enemyScrollAreaX + 20;
                    int contentRight = enemyScrollAreaX + scrollAreaWidth - scrollBarWidth - 20;
                    int contentWidth = contentRight - contentLeft;
                    
                    // 三等分内容区域
                    int segmentWidth = contentWidth / 3;
                    int iconX = contentLeft + col * segmentWidth;
                    int iconY = (int)(enemyCurrentY + row * enemyRowSpacing - _enemyScrollOffset);
                    
                    if (IsPointInRectangle(currentMouseState.X, currentMouseState.Y, iconX, iconY, iconSize, iconSize))
                    {
                        // 选择对应的敌方角色
                        var (name, createFunc) = characters[i];
                        Character selectedCharacter = createFunc();
                        
                        // 检查是否已经选择了这个角色
                        int existingIndex = _selectedEnemies.FindIndex(c => c.Name == selectedCharacter.Name);
                        if (existingIndex >= 0)
                        {
                            // 取消选择
                            _selectedEnemies.RemoveAt(existingIndex);
                        }
                        else
                        {
                            // 添加选择，限制选择数量不超过5个
                            if (_selectedEnemies.Count < 5)
                            {
                                _selectedEnemies.Add(selectedCharacter);
                            }
                        }
                        
                        _previousMouseState = currentMouseState;
                        return false;
                    }
                }
                
                // 更新当前Y位置
                int numRows = (characters.Count + enemyCharsPerRow - 1) / enemyCharsPerRow;
                enemyCurrentY += numRows * enemyRowSpacing + enemyFactionSpacing;
            }
            
            // 检查己方滚动条点击
            Rectangle allyScrollBarRect = new Rectangle(allyScrollBarX, scrollAreaY, scrollBarWidth, scrollAreaHeight);
            if (allyScrollBarRect.Contains(currentMouseState.Position))
            {
                _isDraggingAllyScrollBar = true;
            }
            
            // 检查敌方滚动条点击
            Rectangle enemyScrollBarRect = new Rectangle(enemyScrollBarX, scrollAreaY, scrollBarWidth, scrollAreaHeight);
            if (enemyScrollBarRect.Contains(currentMouseState.Position))
            {
                _isDraggingEnemyScrollBar = true;
            }
            
            // 开始战斗按钮
            int buttonWidth = 200;
            int buttonHeight = 50;
            if (IsPointInRectangle(currentMouseState.X, currentMouseState.Y, (windowWidth - buttonWidth) / 2, scrollAreaY + scrollAreaHeight + 40, buttonWidth, buttonHeight))
            {
                // 开始战斗
                _previousMouseState = currentMouseState;
                return true;
            }
            
            // 行动槽数量选择
            int slotScrollAreaWidth = 600;
            int slotScrollAreaHeight = 80;
            int slotScrollAreaX = (windowWidth - slotScrollAreaWidth) / 2;
            int slotScrollAreaY = (int)(scrollAreaY + scrollAreaHeight + buttonHeight + 60 + GetFontForText("行动槽数量").MeasureString("行动槽数量").Y + 20);
            
            // 行动槽数量选项
            int[] slotOptions = { 5, 10, 15, 20 };
            int slotButtonWidth = 120;
            int slotButtonHeight = 50;
            int slotButtonSpacing = 20;
            
            // 计算最多可以选择的行动槽数量
            int selectedAllyCount = _selectedAllies.Count > 0 ? _selectedAllies.Count : 1; // 未选择时视为1名
            int selectedEnemyCount = _selectedEnemies.Count > 0 ? _selectedEnemies.Count : 1; // 未选择时视为1名
            int minCharacterCount = Math.Min(selectedAllyCount, selectedEnemyCount);
            
            int maxSlotCount;
            if (selectedAllyCount == 1 && selectedEnemyCount == 1)
            {
                // 双方都只选取1名角色时，行动槽的选取数量不受限制
                maxSlotCount = 20; // 默认最大20
            }
            else
            {
                // 至少有一方选取了不低于2名角色时，最多只能选择（人数最少一方的人数x4）个行动槽
                maxSlotCount = minCharacterCount * 4;
            }
            
            // 检查行动槽按钮点击
            for (int i = 0; i < slotOptions.Length; i++)
            {
                int buttonX = slotScrollAreaX + 20 + i * (slotButtonWidth + slotButtonSpacing) - (int)_slotScrollOffset;
                int buttonY = slotScrollAreaY + 15;
                if (IsPointInRectangle(currentMouseState.X, currentMouseState.Y, buttonX, buttonY, slotButtonWidth, slotButtonHeight))
                {
                    // 检查是否超过最大限制
                    if (slotOptions[i] <= maxSlotCount)
                    {
                        _selectedSlotCount = slotOptions[i];
                    }
                    _previousMouseState = currentMouseState;
                    return false;
                }
            }
            
            // 检查行动槽滚动条点击
            int slotScrollBarX = slotScrollAreaX + slotScrollAreaWidth - 10;
            Rectangle slotScrollBarRect = new Rectangle(slotScrollBarX, slotScrollAreaY, 10, slotScrollAreaHeight);
            if (slotScrollBarRect.Contains(currentMouseState.Position))
            {
                _isDraggingSlotScrollBar = true;
            }
        }
        // 处理鼠标释放事件
        else if (currentMouseState.LeftButton == ButtonState.Released && _previousMouseState.LeftButton == ButtonState.Pressed)
        {
            _isDraggingAllyScrollBar = false;
            _isDraggingEnemyScrollBar = false;
            _isDraggingSlotScrollBar = false;
        }
        // 处理鼠标拖动事件
        else if (currentMouseState.LeftButton == ButtonState.Pressed)
        {
            // 处理己方滚动条拖动
            if (_isDraggingAllyScrollBar)
            {
                // 计算己方角色的总高度
                var allyCharacterData = new List<(string Name, Func<Character> Create)> {
                    ("示例角色1", () => new Characters.Allies.示例角色1()),
                    ("夏侯惇", () => new Characters.Allies.夏侯惇()),
                    ("曹仁", () => new Characters.Allies.曹仁()),
                    ("司马懿", () => new Characters.Allies.司马懿()),
                    ("曹丕", () => new Characters.Allies.曹丕()),
                    ("张辽", () => new Characters.Allies.张辽()),
                    ("曹操", () => new Characters.Allies.曹操())
                };
                
                // 排序己方角色：示例角色固定在顶部，然后按势力优先级，同一势力按名称排序
                var sortedAllyCharacters = allyCharacterData
                    .OrderBy(c => c.Name.StartsWith("示例") ? 0 : 1) // 示例角色在顶部
                    .ThenBy(c => {
                        var chara = c.Create();
                        switch (chara.Faction)
                        {
                            case Faction.魏: return 0;
                            case Faction.蜀: return 1;
                            case Faction.吴: return 2;
                            case Faction.群: return 3;
                            default: return 4;
                        }
                    })
                    .ThenBy(c => c.Name)
                    .ToList();
                
                // 计算每个阵营的角色，并按阵营分组
                var groupedAllyCharacters = new Dictionary<Faction, List<(string Name, Func<Character> Create)>>();
                groupedAllyCharacters[Faction.无] = new List<(string, Func<Character>)>();
                groupedAllyCharacters[Faction.魏] = new List<(string, Func<Character>)>();
                groupedAllyCharacters[Faction.蜀] = new List<(string, Func<Character>)>();
                groupedAllyCharacters[Faction.吴] = new List<(string, Func<Character>)>();
                groupedAllyCharacters[Faction.群] = new List<(string, Func<Character>)>();
                
                foreach (var charaData in sortedAllyCharacters)
                {
                    var chara = charaData.Create();
                    groupedAllyCharacters[chara.Faction].Add(charaData);
                }
                
                // 计算总高度
                float allyTotalHeight = scrollAreaY + 20;
                int charsPerRow = 3;
                float rowSpacing = 100; // 两行之间的间距
                float factionTitleHeight = 25; // 阵营标题的高度
                float factionSpacing = 20; // 两个阵营之间的间距
                
                foreach (var faction in new[] { Faction.无, Faction.魏, Faction.蜀, Faction.吴, Faction.群 })
                {
                    var characters = groupedAllyCharacters[faction];
                    if (characters.Count == 0)
                    {
                        continue;
                    }
                    
                    if (faction != Faction.无)
                    {
                        allyTotalHeight += factionTitleHeight;
                    }
                    
                    int numRows = (characters.Count + charsPerRow - 1) / charsPerRow;
                    allyTotalHeight += numRows * rowSpacing + factionSpacing;
                }
                
                float maxAllyScrollOffset = Math.Max(0, allyTotalHeight - scrollAreaY - scrollAreaHeight);
                float dragScrollRatio = (currentMouseState.Y - scrollAreaY) / (float)scrollAreaHeight;
                _allyScrollOffset = dragScrollRatio * maxAllyScrollOffset;
                _allyScrollOffset = Math.Max(0, Math.Min(maxAllyScrollOffset, _allyScrollOffset));
            }
            
            // 处理敌方滚动条拖动
            if (_isDraggingEnemyScrollBar)
            {
                // 计算敌方角色的总高度
                var enemyCharacterData = new List<(string Name, Func<Character> Create)> {
                    ("示例敌怪1", () => new Characters.Enemies.示例敌怪1()),
                    ("夏侯惇", () => new Characters.Allies.夏侯惇(true, false)),
                    ("曹仁", () => new Characters.Allies.曹仁(true, false)),
                    ("司马懿", () => new Characters.Allies.司马懿(true, false)),
                    ("曹丕", () => new Characters.Allies.曹丕(true, false)),
                    ("张辽", () => new Characters.Allies.张辽(true, false)),
                    ("曹操", () => new Characters.Allies.曹操(true, false))
                };
                
                // 排序敌方角色：示例敌怪固定在顶部，然后按势力优先级，同一势力按名称排序
                var sortedEnemyCharacters = enemyCharacterData
                    .OrderBy(c => c.Name.StartsWith("示例") ? 0 : 1) // 示例敌怪在顶部
                    .ThenBy(c => {
                        var chara = c.Create();
                        switch (chara.Faction)
                        {
                            case Faction.魏: return 0;
                            case Faction.蜀: return 1;
                            case Faction.吴: return 2;
                            case Faction.群: return 3;
                            default: return 4;
                        }
                    })
                    .ThenBy(c => c.Name)
                    .ToList();
                
                // 计算每个阵营的角色，并按阵营分组
                var groupedEnemyCharacters = new Dictionary<Faction, List<(string Name, Func<Character> Create)>>();
                groupedEnemyCharacters[Faction.无] = new List<(string, Func<Character>)>();
                groupedEnemyCharacters[Faction.魏] = new List<(string, Func<Character>)>();
                groupedEnemyCharacters[Faction.蜀] = new List<(string, Func<Character>)>();
                groupedEnemyCharacters[Faction.吴] = new List<(string, Func<Character>)>();
                groupedEnemyCharacters[Faction.群] = new List<(string, Func<Character>)>();
                
                foreach (var charaData in sortedEnemyCharacters)
                {
                    var chara = charaData.Create();
                    groupedEnemyCharacters[chara.Faction].Add(charaData);
                }
                
                // 计算总高度
                float enemyTotalHeight = scrollAreaY + 20;
                int enemyCharsPerRow = 3;
                float enemyRowSpacing = 100; // 两行之间的间距
                float enemyFactionTitleHeight = 25; // 阵营标题的高度
                float enemyFactionSpacing = 20; // 两个阵营之间的间距
                
                foreach (var faction in new[] { Faction.无, Faction.魏, Faction.蜀, Faction.吴, Faction.群 })
                {
                    var characters = groupedEnemyCharacters[faction];
                    if (characters.Count == 0)
                    {
                        continue;
                    }
                    
                    if (faction != Faction.无)
                    {
                        enemyTotalHeight += enemyFactionTitleHeight;
                    }
                    
                    int numRows = (characters.Count + enemyCharsPerRow - 1) / enemyCharsPerRow;
                    enemyTotalHeight += numRows * enemyRowSpacing + enemyFactionSpacing;
                }
                
                float maxEnemyScrollOffset = Math.Max(0, enemyTotalHeight - scrollAreaY - scrollAreaHeight);
                float dragScrollRatio = (currentMouseState.Y - scrollAreaY) / (float)scrollAreaHeight;
                _enemyScrollOffset = dragScrollRatio * maxEnemyScrollOffset;
                _enemyScrollOffset = Math.Max(0, Math.Min(maxEnemyScrollOffset, _enemyScrollOffset));
            }
            
            // 处理行动槽滚动条拖动
            if (_isDraggingSlotScrollBar)
            {
                int slotScrollAreaWidth = 600;
                int slotScrollAreaHeight = 80;
                int slotScrollAreaY = (int)(scrollAreaY + scrollAreaHeight + 50 + 60 + GetFontForText("行动槽数量").MeasureString("行动槽数量").Y + 20);
                int[] slotOptions = { 5, 10, 15, 20 };
                int slotButtonWidth = 120;
                int slotButtonSpacing = 20;
                
                float maxSlotScrollOffset = Math.Max(0, (slotOptions.Length * (slotButtonWidth + slotButtonSpacing)) - slotScrollAreaWidth + 40);
                float dragScrollRatio = (currentMouseState.Y - slotScrollAreaY) / (float)slotScrollAreaHeight;
                _slotScrollOffset = dragScrollRatio * maxSlotScrollOffset;
                _slotScrollOffset = Math.Max(0, Math.Min(maxSlotScrollOffset, _slotScrollOffset));
            }
        }
        
        _previousMouseState = currentMouseState;
        return false;
    }
    
    private bool IsPointInRectangle(int x, int y, int rectX, int rectY, int rectWidth, int rectHeight)
    {
        return x >= rectX && x < rectX + rectWidth && y >= rectY && y < rectY + rectHeight;
    }
    
    private bool IsChineseText(string text)
    {
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
        return IsChineseText(text) ? _chineseFont : _font;
    }
}