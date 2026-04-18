using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TurnBasedRPG.Characters;
using TurnBasedRPG.Systems;
using TurnBasedRPG.Buffs.Buff;

namespace TurnBasedRPG.Systems.SkillManagement;

public class SimaYiSkill : BattleSystem
{
    public SimaYiSkill(BattleSystem battleSystem) : base()
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

    public void HandleLangguTrigger(Character target, ActionSlot slot, bool isDirectDamage, int shieldDamage, int healthDamage, bool isShieldBroken, bool isLastCoinHit = false, BuffHandler buffHandler = null, List<Character> allCharacters = null, Character attacker = null)
    {
        if (BuffHandler == null)
        {
            return;
        }
        
        // 鍒濆鍖栨敾鍑昏€?
        bool isEnemyAttack = false;
        
        if (attacker == null && slot != null && _slotToCharacterMap != null && _slotToCharacterMap.ContainsKey(slot))
        {
            attacker = _slotToCharacterMap[slot];
        }

        List<Character> allSimaYis = new List<Character>();
        // 鎵惧埌鎵€鏈夊徃椹嚳锛堝寘鎷弸鏂瑰拰鏁屾柟锛?
        if (allCharacters != null)
        {
            foreach (var chara in allCharacters)
            {
                if (chara is TurnBasedRPG.Characters.Allies.司马懿)
                {
                    allSimaYis.Add(chara);
                }
            }
        }
        
        if (allSimaYis.Count == 0)
        {
            return;
        }
        
        // 澶勭悊姣忎釜鍙搁┈鎳跨殑狼顾瑙﹀彂
        foreach (var simaYi in allSimaYis)
        {
            bool isCounterSkill = IsCounterSkill(slot);
            
            // 鍒ゆ柇鏄惁鏄晫鏂规敾鍑?
            if (attacker != null && attacker.IsAlly != simaYi.IsAlly)
            {
                isEnemyAttack = true;
            }
            else
            {
                isEnemyAttack = false;
            }
            
            // 鏉′欢1锛氳嚜韬鏁屾柟鐨勯潪鍙嶅嚮鎶€鑳藉懡涓?
            bool isSelfHit = (target != null && simaYi != null && target.Name == simaYi.Name && target.IsAlly == simaYi.IsAlly);
            
            // 鏉′欢2锛氬悓闃熼瓘鍥芥灏嗙殑鎶ょ浘琚晫鏂圭殑闈炲弽鍑绘妧鑳藉嚮鐮?
            bool isShieldBrokenWeiAlly = (isShieldBroken && target.Faction == Faction.魏&& target.IsAlly == simaYi.IsAlly);
            
            // 鏉′欢3锛氳嚜韬殑闈炲弽鍑绘妧鑳藉懡涓晫鏂?
            bool isSelfSkillHit = (attacker != null && simaYi != null && attacker.Name == simaYi.Name && attacker.IsAlly == simaYi.IsAlly && !isCounterSkill && target.IsAlly != simaYi.IsAlly);

            // 璋冪敤鍙搁┈鎳跨殑CanTriggerLanggu鏂规硶
            var simaYiObj = simaYi as TurnBasedRPG.Characters.Allies.司马懿;
            if (simaYiObj != null)
            {
                bool shouldTrigger = simaYiObj.CanTriggerLanggu(attacker, isEnemyAttack, isCounterSkill, isSelfHit, isShieldBrokenWeiAlly, isSelfSkillHit, isLastCoinHit);
                
                if (shouldTrigger)
                {
                    TriggerLangguCounter(simaYiObj, attacker, buffHandler, allCharacters);
                }
            }
        }
    }
    
    private void TriggerLangguCounter(TurnBasedRPG.Characters.Allies.司马懿 simaYi, Character attacker, BuffHandler buffHandler, List<Character> allCharacters)
    {
        if (BuffHandler == null)
        {
            return;
        }
        
        // 纭畾鍙嶅嚮鐩爣
        Character counterTarget = null;
        
        // 绗竴姝ワ細灏濊瘯浣跨敤鏈€鍚庝竴娆¤Е鍙戠嫾椤剧殑鏀诲嚮鑰?
        Character lastAttacker = simaYi.GetLastLangguAttacker();
        if (lastAttacker != null && lastAttacker.CurrentHealth > 0)
        {
            counterTarget = lastAttacker;
        }
        
        // 绗簩姝ワ細濡傛灉鐩爣宸叉浜℃垨涓嶅瓨鍦紝浣跨敤浼犲叆鐨勬敾鍑昏€?
        if (counterTarget == null || counterTarget.CurrentHealth <= 0)
        {
            if (attacker != null && attacker.CurrentHealth > 0 && attacker.IsAlly != simaYi.IsAlly)
            {
                // 纭繚鏀诲嚮鑰呬笉鏄徃椹嚳鑷繁
                if (attacker.Name != simaYi.Name || attacker.IsAlly != simaYi.IsAlly)
                {
                    counterTarget = attacker;
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }
        
        // 绗笁姝ワ細濡傛灉鐩爣宸叉浜℃垨涓嶅瓨鍦紝闅忔満閫夋嫨涓€涓瓨娲讳笖鍙閫変腑鐨勬晫鏂瑰崟浣?
        if (counterTarget == null || counterTarget.CurrentHealth <= 0)
        {
            List<Character> enemies = new List<Character>();
            if (simaYi.IsAlly)
            {
                enemies.AddRange(Enemies);
            }
            else
            {
                enemies.AddRange(Players);
            }
            
            // 绛涢€夊瓨娲讳笖鍙閫変腑鐨勬晫浜?
            List<Character> aliveEnemies = enemies.FindAll(e => e.CurrentHealth > 0);
            if (aliveEnemies.Count > 0)
            {
                // 闅忔満閫夋嫨涓€涓瓨娲荤殑鏁屼汉
                Random random = new Random();
                counterTarget = aliveEnemies[random.Next(aliveEnemies.Count)];
            }
            else
            {
                return;
            }
        }
        
        // 澶勭悊鏀诲嚮鍓嶆晥鏋?        // 娑堣€?0%鐨勬姢鐩撅紝鎻愬崌鍩虹鐐规暟
        int currentShield = GetCharacterShield(simaYi);
        int shieldToConsume = (int)(currentShield * 0.5f);
        
        if (shieldToConsume > 0)
        {
            _characterShields[simaYi] = currentShield - shieldToConsume;
        }
        
        // 鍒涘缓狼顾鎶€鑳?
        BaseSkill counterSkill = new TurnBasedRPG.Characters.Skills.司马懿.狼顾();
        simaYi.CalculateSkillValues(counterSkill);
        
        // 姣忔秷鑰?5鐐规姢鐩撅紝鍩虹鐐规暟+1
        int baseValueBonus = shieldToConsume / 15;
        counterSkill.BaseValue += baseValueBonus;
        
        // 鍒涘缓涓存椂琛屽姩妲芥潵澶勭悊鍙嶅嚮
        ActionSlot counterSlot = new ActionSlot(0);
        counterSlot.SetAction(ActionType.Attack, counterSkill);
        
        // 鎶曟幏纭竵
        counterSlot.FlipCoins(simaYi.Morale);
        
        // 娣诲姞涓存椂琛屽姩妲藉埌鏄犲皠涓紝浠ヤ究鎵惧埌鏀诲嚮鑰?
        if (_slotToCharacterMap != null)
        {
            _slotToCharacterMap[counterSlot] = simaYi;
        }
        
        // 璁板綍杩欐鏀诲嚮

        
        // 鐩存帴杩涜瀹屾暣鐨勪激瀹宠绠?        // 璁＄畻褰撳墠鎬荤殑纭竵鐐规暟
        int headsCount = 0;
        for (int i = 0; i < counterSlot.Coins.Length; i++)
        {
            if (counterSlot.Coins[i] == 1)
                headsCount++;
        }
        int finalValue = counterSkill.BaseValue + (headsCount * counterSkill.CoinValue);
        
        // 纭畾鏀婚槻绛夌骇锛堢嫾椤句娇鐢ㄩ槻寰＄瓑绾ц绠楋級
        int skillLevel = simaYi.FinalDefenseLevel;
        
        // 璁＄畻skillLevelMultiplier锛堟敾闃茬瓑绾т慨姝ｄ箻鍖猴級锛堢嫾椤炬槸鍙嶅嚮鎶€鑳斤紝浣跨敤4.5%鍊嶇巼锛?
        double multiplierRate = 0.045;
        
        // 狼顾鏄弽鍑绘妧鑳斤紝浣跨敤鐩爣鐨勯槻寰＄瓑绾ц繘琛岃绠?
        int targetLevelForCalculation = counterTarget.FinalDefenseLevel;
        int levelDifference = skillLevel - targetLevelForCalculation;
        double skillLevelMultiplier = 1.5 + ((double)levelDifference * multiplierRate);
        skillLevelMultiplier = Math.Max(0.2, skillLevelMultiplier);
        
        // 涓€绫诲浼や箻鍖篸amageMultiplier锛?1+鏀诲嚮鑰呬激瀹虫彁鍗?鐩爣浼ゅ鍑忓厤)锛屾渶浣?.2
        float damageMultiplier = (1 + simaYi.DamageIncrease - counterTarget.DamageReduction);
        damageMultiplier = Math.Max(0.2f, damageMultiplier);
        
        // 鑾峰彇浼ゅ绉嶇被鎶楁€?
        float damageTypeResistance = 1.0f;
        switch (counterSkill.DamageType)
        {
            case DamageType.Physical:
                damageTypeResistance = counterTarget.PhysicalVulnerability;
                break;
            case DamageType.Magic:
                damageTypeResistance = counterTarget.MagicVulnerability;
                break;
            case DamageType.True:
                damageTypeResistance = counterTarget.TrueVulnerability;
                break;
        }
        
        // 鑾峰彇鏀诲嚮鏂瑰紡鎶楁€?
        float attackTypeResistance = 1.0f;
        switch (counterSkill.AttackType)
        {
            case AttackType.Slash:
                attackTypeResistance = counterTarget.SlashVulnerability;
                break;
            case AttackType.Blunt:
                attackTypeResistance = counterTarget.BluntVulnerability;
                break;
            case AttackType.Pierce:
                attackTypeResistance = counterTarget.PierceVulnerability;
                break;
            case AttackType.Spell:
                attackTypeResistance = counterTarget.SpellVulnerability;
                break;
        }
        
        // 纭繚鎶楁€у€间笉浣庝簬0.1
        damageTypeResistance = Math.Max(0.1f, damageTypeResistance);
        attackTypeResistance = Math.Max(0.1f, attackTypeResistance);
        
        // 鏈€缁堜激瀹充箻鍖篺inalDamageMultiplier锛?1+鏀诲嚮鑰呮渶缁堜激瀹虫彁鍗?鐩爣鏈€缁堜激瀹冲噺鍏?
        float finalDamageMultiplier = (1 + simaYi.FinalDamageIncrease - counterTarget.FinalDamageReduction);
        
        // 鍙搁┈鎳跨殑鏈€缁堝浼や綆浜?0%鏃讹紝鎻愬崌鑷?0%
        if (simaYi.FinalDamageIncrease < 0.5f)
        {
            finalDamageMultiplier = (1 + 0.5f - counterTarget.FinalDamageReduction);
        }
        
        // 鏆村嚮鍒ゅ畾
        bool isCriticalHit = false;
        float critDamageMultiplier = 1.0f;
        // 狼顾鐨勬敾鍑昏€呮槸司马懿
        Character langguAttacker = simaYi;
        if (langguAttacker != null)
        {
            // 璁＄畻鏆村嚮姒傜巼
            float skillCritRate = counterSkill.CritRate;
            float targetCritResistance = counterTarget.CritResistance;
            float firstStepCritRate = Math.Max(0, skillCritRate - targetCritResistance);
            float finalCritRateStep = counterSkill.FinalCritRate - counterTarget.FinalCritResistance;
            float totalCritRate = Math.Max(0, firstStepCritRate + finalCritRateStep);
            totalCritRate = Math.Min(totalCritRate, 1.0f); // 瓒呭嚭100%瑙嗕负100%
            
            // 鎸夋鐜囧垽瀹氭槸鍚︽毚鍑?
            Random langguRandom = new Random();
            double randomValue = langguRandom.NextDouble();
            if (randomValue < totalCritRate)
            {
                isCriticalHit = true;
            }
            
            // 璁＄畻鏆村嚮浼ゅ涔樺尯
            if (isCriticalHit)
            {
                float skillCritDamage = counterSkill.CritDamage;
                float targetCritDamageResistance = counterTarget.CritDamageResistance;
                critDamageMultiplier = 1 + (skillCritDamage - targetCritDamageResistance);
                critDamageMultiplier = Math.Max(1.0f, critDamageMultiplier); // 涓嶄綆浜?
            }
        }
        
        // 璁＄畻鏈€缁堜激瀹?
        int damage = (int)(finalValue * skillLevelMultiplier * damageMultiplier * finalDamageMultiplier * attackTypeResistance * damageTypeResistance * critDamageMultiplier);
        
        // 搴旂敤浼ゅ锛堢嫾椤炬槸鍙嶅嚮鎶€鑳斤紝涓嶆槸鐩存帴浼ゅ锛?
        ApplyDamage(damage, counterTarget, counterSlot);
        
        // 娣诲姞浼ゅ缁撶畻鏃ュ織
        int shieldBefore = GetCharacterShield(counterTarget);
        int shieldAfter = GetCharacterShield(counterTarget);
        int healthBefore = counterTarget.CurrentHealth;
        int healthAfter = counterTarget.CurrentHealth;
        int shieldDamageTaken = shieldBefore - shieldAfter;
        int healthDamageTaken = healthBefore - healthAfter;
        
        // 璁板綍浼ゅ缁熻
        Statistics.RecordDamage(simaYi, counterSkill.Name, shieldDamageTaken, healthDamageTaken);
        
        if (shieldDamageTaken > 0 && healthDamageTaken > 0)
        {
    
        }
        else if (shieldDamageTaken > 0)
        {
    
        }
        else if (healthDamageTaken > 0)
        {
    
        }
        else
        {
    
        }
        
        // 澶勭悊狼顾鍛戒腑鏃剁殑鏁堟灉
        // 璁板綍狼顾閫犳垚鐨勪激瀹?
        int recordedDamage = shieldDamageTaken + healthDamageTaken;
        
        // 涓哄叏闃熼瓘鍥芥灏嗘彁渚涚浉褰撲簬鏈浼ゅ50%鐨勬姢鐩撅紝鏈€楂樹笉瓒呰繃鍙搁┈鎳挎渶澶х敓鍛界殑15%
        int maxShieldAmount = (int)(simaYi.MaxHealth * 0.15f);
        int shieldAmountForAllies = (int)(recordedDamage * 0.5f);
        shieldAmountForAllies = Math.Min(shieldAmountForAllies, maxShieldAmount);
        
        if (shieldAmountForAllies > 0)
        {
            // 缁欐敾鍑昏€呰嚜宸卞姞鎶ょ浘
            AddShield(langguAttacker, shieldAmountForAllies, "狼顾");
            // 璁板綍鎶ょ浘缁熻
            Statistics.RecordShield(simaYi, "狼顾", shieldAmountForAllies);
            
            // 缁欏悓闃熷叾浣欓瓘鍥芥灏嗗姞鎶ょ浘
            List<Character> allies = new List<Character>();
            if (simaYi.IsAlly)
            {
                allies.AddRange(Players);
            }
            else
            {
                allies.AddRange(Enemies);
            }
            
            foreach (var ally in allies)
            {
                if (ally != langguAttacker && ally.Faction == Faction.魏)
                {
                    AddShield(ally, shieldAmountForAllies, "狼顾");
                    // 璁板綍鎶ょ浘缁熻
                    Statistics.RecordShield(simaYi, "狼顾", shieldAmountForAllies);
                }
            }
        }
        
        // 澶勭悊狼顾鏀诲嚮鍚庢晥鏋?        // 妫€鏌ュ徃椹嚳鐨勯煬鏅︾姸鎬?
        var simaYiBuffs = GetBuffs(simaYi);
        var taoHuiBuff = simaYiBuffs.Find(b => b is 韬晦);
        
        if (taoHuiBuff != null)
        {
            if (taoHuiBuff.Strength < 6)
            {
                // 韬晦寮哄害浣庝簬6锛氳幏寰?绾ч煬鏅?
                AddBuff(simaYi, new 韬晦(3, 1));
            }
            else
            {
                // 韬晦寮哄害涓嶄綆浜?锛氭秷鑰椾竴鍗婂己搴︼紝棰濆鎵ｉ櫎鐩爣鐢熷懡鍊?
                int originalStrength = taoHuiBuff.Strength;
                int strengthToConsume = originalStrength / 2;
                taoHuiBuff.Strength = originalStrength - strengthToConsume;
                
                // 棰濆鎵ｉ櫎鐩爣鐢熷懡鍊硷紝鎵ｉ櫎鍊肩浉褰撲簬浼ゅ璁板綍鍊?
                if (counterTarget.CurrentHealth > 0)
                {
                    // 浣跨敤ApplyDamage澶勭悊棰濆浼ゅ锛岀‘淇濆厛鎵ｉ櫎鎶ょ浘鍐嶆墸闄よ閲忥紝骞朵笖鍙互琚濞佺姸鎬佸厤鐤?
                    ApplyDamage(recordedDamage, counterTarget, counterSlot, isDirectDamage: true);
                }
                else
                {
                    // 鐩爣宸叉浜★紝瀵绘壘鍙€変腑鐨勯殢鏈烘晫鏂瑰崟浣?
                    List<Character> enemies = new List<Character>();
                    if (simaYi.IsAlly)
                    {
                        enemies.AddRange(Enemies);
                    }
                    else
                    {
                        enemies.AddRange(Players);
                    }
                    
                    // 绛涢€夊瓨娲讳笖鍙閫変腑鐨勬晫浜?
                    List<Character> aliveEnemies = enemies.FindAll(e => e.CurrentHealth > 0);
                    if (aliveEnemies.Count > 0)
                    {
                        // 闅忔満閫夋嫨涓€涓瓨娲荤殑鏁屼汉
                        Random random = new Random();
                        Character randomTarget = aliveEnemies[random.Next(aliveEnemies.Count)];
                        
                        // 浣跨敤ApplyDamage澶勭悊棰濆浼ゅ锛岀‘淇濆厛鎵ｉ櫎鎶ょ浘鍐嶆墸闄よ閲忥紝骞朵笖鍙互琚濞佺姸鎬佸厤鐤?
                        ApplyDamage(recordedDamage, randomTarget, counterSlot, isDirectDamage: true);
                    }
                }
            }
        }
        else
        {
            // 娌℃湁韬晦鐘舵€侊紝鑾峰緱1绾ч煬鏅?
            AddBuff(simaYi, new 韬晦(3, 1));
        }
    }
    
    private string GetTeamInfo(Character character)
    {
        return character?.IsAlly == true ? "-鎴戞柟" : "-鏁屾柟";
    }
}
