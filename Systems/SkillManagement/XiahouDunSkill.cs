using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Characters;
using TurnBasedRPG.Systems;
using TurnBasedRPG.Buffs.Buff;
using TurnBasedRPG.Buffs.Debuff;
using TurnBasedRPG.Characters.Skills.夏侯惇;

namespace TurnBasedRPG.Systems.SkillManagement;

public class XiahouDunSkill : BattleSystem
{
    public XiahouDunSkill(BattleSystem battleSystem) : base()
    {
        if (battleSystem != null)
        {
            Players = battleSystem.Players;
            Enemies = battleSystem.Enemies;
            PlayerSlots = battleSystem.PlayerSlots;
            EnemySlots = battleSystem.EnemySlots;
            _slotToCharacterMap = battleSystem._slotToCharacterMap;
            _characterShields = battleSystem._characterShields;
            BattleLog = battleSystem.BattleLog;
            Statistics = battleSystem.Statistics;
            _buffHandler = battleSystem._buffHandler;
        }
    }
    
    // 榄忔固阵鐨勫叿浣撳疄鐜版柟娉?
    public void HandleXiahouDunSkillEffects(Character xiahouDun, ActionSlot slot, Character target, BuffHandler buffHandler, List<Character> allCharacters)
    {
        if (xiahouDun == null || slot == null || target == null)
        {
            return;
        }
        
        // 澶勭悊横斩鎶€鑳藉懡涓椂鐨勬晥鏋滐細浣跨洰鏍囪幏寰?绾脆弱]锛屾寔缁?鍥炲悎
        if (slot.SkillName == "横斩")
        {
            buffHandler.AddBuff(target, new 脆弱(2, 2));

        }
        
        // 澶勭悊御甲鸣镝鎶€鑳芥敾鍑诲悗鏁堟灉锛氫娇涓昏鐩爣涓庢墍鏈夋绾х洰鏍囪幏寰梉围城]锛屾寔缁?鍥炲悎
        if (slot.SkillName == "御甲鸣镝" && slot.IsLastCoin)
        {
            // 缁欎富瑕佺洰鏍囨坊鍔犲洿鍩庣姸鎬?
            buffHandler.AddBuff(target, new 围城(2, 1));

            
            // 缁欐绾х洰鏍囨坊鍔犲洿鍩庣姸鎬?
            List<Character> secondaryTargets = GetSecondaryTargets(xiahouDun, target);
            if (secondaryTargets != null && secondaryTargets.Count > 0)
            {
                foreach (var secondaryTarget in secondaryTargets)
                {
                    if (secondaryTarget != null && !secondaryTarget.ShouldDie())
                    {
                        buffHandler.AddBuff(secondaryTarget, new 围城(2, 1));

                    }
                }
            }
        }
        
        // 澶勭悊铁壁战吼鎶€鑳芥渶鍚庝竴鏋氱‖甯佸懡涓椂鐨勬晥鏋?        if (slot.SkillName == "铁壁战吼" && slot.IsLastCoin)
        {
            // 璁＄畻棰濆浼ゅ锛?5%鐨勭湡瀹炰激瀹筹級
            int additionalDamage = (int)(slot.TotalDamage * 0.25f);
            
            // 鎵惧埌鏁屾柟闅忔満鐩爣
            List<Character> enemies = new List<Character>();
            if (target.IsAlly)
            {
                // 濡傛灉鐩爣鏄繁鏂癸紝閭ｄ箞鏁屾柟鏄疎nemies
                enemies.AddRange(Enemies);
            }
            else
            {
                // 濡傛灉鐩爣鏄晫鏂癸紝閭ｄ箞鏁屾柟鏄疨layers
                enemies.AddRange(Players);
            }
            
            // 鎺掗櫎褰撳墠鐩爣
            enemies.Remove(target);
            
            if (enemies.Count > 0)
            {
                // 闅忔満閫夋嫨涓€涓晫鏂圭洰鏍?
                Random randomEnemySelector = new Random();
                Character randomEnemy = enemies[randomEnemySelector.Next(enemies.Count)];
                
                // 瀵归殢鏈虹洰鏍囬€犳垚棰濆浼ゅ
                if (additionalDamage > 0)
                {
                    // 浣跨敤ApplyDamage鏂规硶澶勭悊鐪熷疄浼ゅ
                    ApplyDamage(additionalDamage, randomEnemy, slot, isDirectDamage: true);

                }
                
                // 浣胯嚜韬幏寰楃瓑鍚屼簬璇ユ棰濆浼ゅ200%鐨勬姢鐩惧€?
                int shieldAmount = (int)(additionalDamage * 2.0f);
                if (shieldAmount > 0)
                {
                    // 鎵惧埌鏀诲嚮鑰?
                    Character tiebiAttacker = xiahouDun;
                    
                    if (tiebiAttacker != null)
                    {
                        AddShield(tiebiAttacker, shieldAmount);
                    }
                }
                
                // 浣夸富瑕佺洰鏍囦笌闄勫姞浼ゅ鍛戒腑鐨勭洰鏍囪幏寰?绾虚弱]锛屾寔缁?鍥炲悎
                buffHandler.AddBuff(target, new 虚弱(2, 3));
                if (randomEnemy != null)
                {
                    buffHandler.AddBuff(randomEnemy, new 虚弱(2, 3));
                }

            }
        }
        
        // 澶勭悊窃国者侯鎶€鑳芥敾鍑诲悗鏁堟灉锛氫娇鎵€鏈夌洰鏍囪幏寰?绾虚弱]锛屾寔缁?鍥炲悎
        if (slot.SkillName == "窃国者侯" && slot.IsLastCoin)
        {
            // 缁欎富瑕佺洰鏍囨坊鍔犺櫄寮辩姸鎬?            buffHandler.AddBuff(target, new 虚弱(2, 2));

            
            // 缁欐绾х洰鏍囨坊鍔犺櫄寮辩姸鎬?
            List<Character> secondaryTargets = GetSecondaryTargets(xiahouDun, target);
            if (secondaryTargets != null && secondaryTargets.Count > 0)
            {
                foreach (var secondaryTarget in secondaryTargets)
                {
                    if (secondaryTarget != null && !secondaryTarget.ShouldDie())
                    {
                        buffHandler.AddBuff(secondaryTarget, new 虚弱(2, 2));

                    }
                }
            }
        }
    }
    
    private List<Character> GetSecondaryTargets(Character attacker, Character primaryTarget)
    {
        List<Character> secondaryTargets = new List<Character>();
        
        // 杩欓噷鍙互鏍规嵁娓告垙瑙勫垯瀹炵幇鑾峰彇娆＄骇鐩爣鐨勯€昏緫
        // 渚嬪锛岃幏鍙栭櫎涓昏鐩爣澶栫殑鍏朵粬鏁屾柟鍗曚綅
        
        List<Character> enemies = new List<Character>();
        if (attacker.IsAlly)
        {
            enemies.AddRange(Enemies);
        }
        else
        {
            enemies.AddRange(Players);
        }
        
        foreach (var enemy in enemies)
        {
            if (enemy != primaryTarget && enemy.CurrentHealth > 0)
            {
                secondaryTargets.Add(enemy);
            }
        }
        
        return secondaryTargets;
    }
}