using Microsoft.Xna.Framework;
using TurnBasedRPG.Characters;

namespace TurnBasedRPG.Buffs;

public abstract class BaseBuff
{
    public Color IconColor { get; set; } = Color.White;
    public int? RemainingTurns { get; set; }
    public int Strength { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsFactionBuff { get; set; }
    public bool IsBuff { get; set; }
    
    public BaseBuff(string name, string description, int? remainingTurns = null, int strength = 0, bool isFactionBuff = false, bool isBuff = false)
    {
        Name = name;
        Description = description;
        RemainingTurns = remainingTurns;
        Strength = strength;
        IsFactionBuff = isFactionBuff;
        IsBuff = isBuff;
    }
    
    public virtual void UpdateBuff(Character character)
    {
        // 子类可以重写此方法来实现具体的buff效果
    }
    
    public virtual void OnAdded(Character character)
    {
        // 当buff被添加到角色身上时调�?    }
    
    public virtual void OnRemoved(Character character)
    {
        // 当buff从角色身上被移除时调�?    }
    
    public virtual bool ShouldRemove()
    {
        return RemainingTurns.HasValue && RemainingTurns <= 0;
    }
    
    public virtual void DecrementTurns()
    {
        if (RemainingTurns.HasValue)
        {
            RemainingTurns--;
        }
    }
}
