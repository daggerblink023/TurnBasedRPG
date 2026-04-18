using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace TurnBasedRPG.Tutorials;

public class TutorialProgress
{
    // 保存文件路径
    private static readonly string SaveFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TurnBasedRPG",
        "tutorial_progress.json"
    );

    // 教程进度数据
    public class TutorialSaveData
    {
        public List<int> CompletedLevels { get; set; } = new List<int>();
        public bool AllLevelsCompleted { get; set; } = false;
    }

    private TutorialSaveData _saveData;

    public TutorialProgress()
    {
        LoadProgress();
    }

    // 加载进度
    private void LoadProgress()
    {
        try
        {
            if (File.Exists(SaveFilePath))
            {
                string json = File.ReadAllText(SaveFilePath);
                _saveData = JsonSerializer.Deserialize<TutorialSaveData>(json);
            }
            else
            {
                _saveData = new TutorialSaveData();
            }
        }
        catch
        {
            _saveData = new TutorialSaveData();
        }
    }

    // 保存进度
    private void SaveProgress()
    {
        try
        {
            // 确保目录存在
            string directory = Path.GetDirectoryName(SaveFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonSerializer.Serialize(_saveData, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(SaveFilePath, json);
        }
        catch
        {
            // 保存失败时不抛出异常
        }
    }

    // 检查关卡是否已完成
    public bool IsLevelCompleted(int levelNumber)
    {
        return _saveData.CompletedLevels.Contains(levelNumber);
    }

    // 检查关卡是否已解锁
    public bool IsLevelUnlocked(int levelNumber)
    {
        // 如果所有关卡都已完成，可以任意选择
        if (_saveData.AllLevelsCompleted)
        {
            return true;
        }

        // 第一关总是解锁的
        if (levelNumber == 1)
        {
            return true;
        }

        // 否则需要前一关已完成
        return IsLevelCompleted(levelNumber - 1);
    }

    // 标记关卡为已完成
    public void MarkLevelCompleted(int levelNumber)
    {
        if (!IsLevelCompleted(levelNumber))
        {
            _saveData.CompletedLevels.Add(levelNumber);
            
            // 检查是否所有关卡都已完成
            if (_saveData.CompletedLevels.Count >= 5)
            {
                _saveData.AllLevelsCompleted = true;
            }
            
            SaveProgress();
        }
    }

    // 重置所有进度
    public void ResetProgress()
    {
        _saveData = new TutorialSaveData();
        SaveProgress();
    }
}
