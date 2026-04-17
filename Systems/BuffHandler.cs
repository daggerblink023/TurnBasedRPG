using System.Collections.Generic;
using TurnBasedRPG.Characters;
using TurnBasedRPG.Characters.Allies;
using TurnBasedRPG.Buffs;
using TurnBasedRPG.Buffs.Buff;
using TurnBasedRPG.Buffs.Debuff;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG.Systems;

public class BuffHandler
{
    private Dictionary<Character, List<BaseBuff>> _characterBuffs = new Dictionary<Character, List<BaseBuff>>();
    private BattleSystem _battleSystem;
    
    public void SetBattleSystem(BattleSystem battleSystem)
    {
        _battleSystem = battleSystem;
    }
    
    public void AddBuff(Character character, BaseBuff buff)
    {
        if (!_characterBuffs.ContainsKey(character))
        {
            _characterBuffs[character] = new List<BaseBuff>();
        }
        
        // 检查：如果目标是持有神威状态的张辽，且施加的是减益状态，则不施加
        if (character is TurnBasedRPG.Characters.Allies.张辽 && 
            CheckBuff<神威>(character) &&
            !buff.IsBuff)
        {
            return;
        }
        
        // 总是添加新的buff，不检查是否存在相同类型的buff
        // 这样不同来源的同名Buff会被视为独立的状态
        _characterBuffs[character].Add(buff);
        buff.OnAdded(character);
    }
    
    public void RemoveBuff(Character character, BaseBuff buff)
    {
        if (_characterBuffs.ContainsKey(character))
        {
            if (_characterBuffs[character].Remove(buff))
            {
                buff.OnRemoved(character);
            }
        }
    }
    
    public bool CheckBuff<T>(Character character) where T : BaseBuff
    {
        if (!_characterBuffs.ContainsKey(character))
        {
            return false;
        }
        
        return _characterBuffs[character].Exists(b => b is T);
    }
    
    // 获取角色的所有buff
    public List<BaseBuff> GetBuffs(Character character)
    {
        if (!_characterBuffs.ContainsKey(character))
        {
            return new List<BaseBuff>();
        }
        
        return _characterBuffs[character];
    }
    
    public void UpdateBuffs(Character character)
    {
        if (!_characterBuffs.ContainsKey(character))
        {
            return;
        }
        
        var buffs = _characterBuffs[character];
        
        // 先分离武道独尊和其他buff
        List<BaseBuff> normalBuffs = new List<BaseBuff>();
        List<BaseBuff> wuDaoDuZunBuffs = new List<BaseBuff>();
        
        foreach (var buff in buffs)
        {
            if (buff is 武道独尊)
            {
                wuDaoDuZunBuffs.Add(buff);
            }
            else
            {
                normalBuffs.Add(buff);
            }
        }
        
        // 先处理武道独尊（确保HasWuDaoDuZun先设置为true）
        foreach (var buff in wuDaoDuZunBuffs)
        {
            buff.UpdateBuff(character);
            
            // 处理特殊buff效果
            HandleSpecialBuffEffects(character, buff);
            
            if (buff.ShouldRemove())
            {
                RemoveBuff(character, buff);
            }
        }
        
        // 再处理普通buff
        for (int i = normalBuffs.Count - 1; i >= 0; i--)
        {
            var buff = normalBuffs[i];
            // 如果角色有武道独尊，跳过固阵/魏武固阵的攻击等级降低
            if (character.HasWuDaoDuZun && (buff is 固阵 || buff is 魏武固阵))
            {
                // 仍然更新buff，但不处理攻击等级降低
                // 先保存原始的AttackLevelAdjustment
                int originalAttackLevelAdjustment = character.AttackLevelAdjustment;
                int originalDefenseLevelAdjustment = character.DefenseLevelAdjustment;
                
                buff.UpdateBuff(character);
                
                // 武道独尊免疫攻击等级降低，强制使用攻击等级计算
                // 恢复固阵/魏武固阵对攻击等级的修改
                character.AttackLevelAdjustment = originalAttackLevelAdjustment;
                character.DefenseLevelAdjustment = originalDefenseLevelAdjustment;
            }
            else
            {
                buff.UpdateBuff(character);
            }
            
            // 处理特殊buff效果
            HandleSpecialBuffEffects(character, buff);
            
            if (buff.ShouldRemove())
            {
                RemoveBuff(character, buff);
            }
        }
    }
    
    public void DecrementTurns(Character character)
    {
        if (!_characterBuffs.ContainsKey(character))
        {
            return;
        }
        
        var buffs = _characterBuffs[character];
        
        foreach (var buff in buffs)
        {
            buff.DecrementTurns();
        }
        
        // 减少回合数后，立即更新状态并清除过期状态
        UpdateBuffs(character);
    }
    
    private void HandleSpecialBuffEffects(Character character, BaseBuff buff)
    {
        // 处理沉默debuff
        if (buff is Silence)
        {
            // 沉默效果将在技能选择时处理
        }
        
        // 处理忍耐buff
        if (buff is Endurance)
        {
            // 忍耐效果已经在Endurance类的UpdateBuff方法中处理
        }
        
        // 处理刚烈buff
        if (buff is Ganglie && character is 夏侯惇)
        {
            // 刚烈效果：当角色有护盾时，最终减伤+50%
            // 这里需要获取角色的护盾值，暂时假设角色有护盾属性
            // 实际实现中需要根据游戏的护盾系统来调整
            character.FinalDamageReduction += 0.5f;
        }
    }
    
    // 处理沉默debuff对技能选择的影响
    public AttackSkill HandleSilenceSkillSelection(Character character, AttackSkill selectedSkill)
    {
        if (CheckBuff<Silence>(character))
        {
            // 带有沉默debuff时，只能使用技能1
            return AttackSkill.Skill1;
        }
        return selectedSkill;
    }
    
    // 处理刚烈buff的伤害反弹效果
    public void HandleGanglieDamageReflection(Character character, Character attacker, int damage)
    {
        if (CheckBuff<Ganglie>(character) && character is 夏侯惇)
        {
            // 这里需要获取角色的护盾值，暂时假设角色有护盾属性
            // 实际实现中需要根据游戏的护盾系统来调整
            // 假设角色有护盾
            int reflectedDamage = (int)(damage * 1.5f);
            // 反弹真实伤害，攻击类型乘区固定为1.00
            // 使用ApplyDamage方法处理反弹伤害，确保先扣除护盾再扣除血量
            if (attacker != null && _battleSystem != null)
            {
                // 使用ApplyDamage处理反弹伤害
                _battleSystem.ApplyDamage(reflectedDamage, attacker, null, isDirectDamage: false);
                
                // 添加战斗日志
                System.Collections.Generic.List<string> battleLog = new System.Collections.Generic.List<string>();
                battleLog.Add($"刚烈反弹了{reflectedDamage}点伤害给{attacker.Name}！");
            }
        }
    }
}
