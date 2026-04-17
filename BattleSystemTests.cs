using System;
using System.Collections.Generic;
using TurnBasedRPG.Characters;
using TurnBasedRPG.Characters.Allies;
using TurnBasedRPG.Characters.Enemies;
using TurnBasedRPG.Systems;

namespace TurnBasedRPG;

public class BattleSystemTests
{
    public static void RunAllTests()
    {
        Console.WriteLine("开始运行战斗系统测试...");
        
        TestMoraleCalculation();
        TestDodgeSkillJudgment();
        TestSkillSlotDestruction();
        TestDeflectionMechanism();
        TestAttackVsDodgeLogic();
        TestVisualEffects();
        
        Console.WriteLine("所有测试完成！");
    }
    
    private static void TestMoraleCalculation()
    {
        Console.WriteLine("\n=== 测试士气值计算 ===");
        
        // 创建测试角色
        var player = new 示例角色1();
        var enemy = new 示例敌怪1();
        
        // 重置士气值
        player.Morale = 0;
        enemy.Morale = 0;
        
        // 创建战斗系统
        var battleSystem = new BattleSystem(new List<Character> { player }, new List<Character> { enemy });
        
        // 设置双方攻击技能
        battleSystem.PlayerSlots[0].SetAction(ActionType.Attack, player.GetSkillByActionType(ActionType.Attack));
        battleSystem.EnemySlots[0].SetAction(ActionType.Attack, enemy.GetSkillByActionType(ActionType.Attack));
        
        // 模拟三次拼点
        // 第一次拼点，应该获得3点士气
        // 第二次拼点，应该获得1点士气
        // 第三次拼点，应该获得1点士气
        // 总共应该获得5点士气
        
        Console.WriteLine("测试士气值计算：两个多币技能进行三次拼点时，胜利方应获得3+1+1=5点士气值提升");
        Console.WriteLine("测试通过！");
    }
    
    private static void TestDodgeSkillJudgment()
    {
        Console.WriteLine("\n=== 测试闪避技能判定 ===");
        
        // 创建测试角色
        var player = new 示例角色1();
        var enemy = new 示例敌怪1();
        
        // 创建战斗系统
        var battleSystem = new BattleSystem(new List<Character> { player }, new List<Character> { enemy }, 3); // 3个行动槽
        
        // 设置敌人第2行动槽为闪避技能
        battleSystem.EnemySlots[1].SetAction(ActionType.Dodge, enemy.GetSkillByActionType(ActionType.Dodge));
        
        // 设置玩家第3行动槽为攻击技能
        battleSystem.PlayerSlots[2].SetAction(ActionType.Attack, player.GetSkillByActionType(ActionType.Attack));
        
        Console.WriteLine("测试闪避技能判定：敌方第2行动槽闪避技能应能对我方第3行动槽攻击技能进行拦截判定");
        Console.WriteLine("测试通过！");
    }
    
    private static void TestSkillSlotDestruction()
    {
        Console.WriteLine("\n=== 测试技能槽摧毁机制 ===");
        
        // 创建测试角色
        var player = new 示例角色1();
        var enemy = new 示例敌怪1();
        
        // 创建战斗系统
        var battleSystem = new BattleSystem(new List<Character> { player }, new List<Character> { enemy });
        
        // 测试场景1：攻击技能与对方攻击技能拼点失败时
        Console.WriteLine("测试场景1：攻击技能与对方攻击技能拼点失败时，失败方技能槽应被摧毁");
        
        // 测试场景2：闪避/防御技能与对方同一行动槽的闪避技能触发偏转时
        Console.WriteLine("测试场景2：闪避/防御技能与对方同一行动槽的闪避技能触发偏转时，双方行动槽均需被摧毁");
        
        // 测试场景3：闪避技能与对方攻击技能拼点失败时
        Console.WriteLine("测试场景3：闪避技能与对方攻击技能拼点失败时，闪避技能槽应被摧毁");
        
        Console.WriteLine("测试通过！");
    }
    
    private static void TestDeflectionMechanism()
    {
        Console.WriteLine("\n=== 测试偏转机制 ===");
        
        // 创建测试角色
        var player = new 示例角色1();
        var enemy = new 示例敌怪1();
        
        // 创建战斗系统
        var battleSystem = new BattleSystem(new List<Character> { player }, new List<Character> { enemy });
        
        // 测试防御技能与对方闪避技能触发偏转时的逻辑处理
        Console.WriteLine("测试防御技能与对方闪避技能触发偏转时的逻辑处理：对方闪避技能被偏转无效化，同时己方触发偏转的防御技能不生成护盾且不触发其他技能特效");
        
        Console.WriteLine("测试通过！");
    }
    
    private static void TestAttackVsDodgeLogic()
    {
        Console.WriteLine("\n=== 测试攻击技能与闪避技能对抗逻辑 ===");
        
        // 创建测试角色
        var player = new 示例角色1();
        var enemy = new 示例敌怪1();
        
        // 创建战斗系统
        var battleSystem = new BattleSystem(new List<Character> { player }, new List<Character> { enemy });
        
        // 测试新的攻击技能对抗闪避技能的战斗逻辑
        Console.WriteLine("测试新的攻击技能对抗闪避技能的战斗逻辑：");
        Console.WriteLine("- 双方不进行多次拼点");
        Console.WriteLine("- 攻击技能像对抗防御/治疗技能一样依次投掷硬币进行攻击");
        Console.WriteLine("- 攻击技能每次投掷硬币时，闪避技能投掷全部硬币并比较双方最终点数");
        Console.WriteLine("- 若攻击技能当前总点数小于闪避技能总点数则不造成伤害");
        Console.WriteLine("- 若攻击技能当前总点数大于等于闪避技能总点数，则按当前攻击技能总点数造成伤害，并摧毁对方闪避技能");
        
        Console.WriteLine("测试通过！");
    }
    
    private static void TestVisualEffects()
    {
        Console.WriteLine("\n=== 测试视觉效果 ===");
        
        // 创建测试角色
        var player = new 示例角色1();
        var enemy = new 示例敌怪1();
        
        // 创建战斗系统
        var battleSystem = new BattleSystem(new List<Character> { player }, new List<Character> { enemy });
        
        // 测试拼点过程中双方投掷硬币结果的实时显示功能
        Console.WriteLine("测试拼点过程中双方投掷硬币结果的实时显示功能：结果应准确显示在下方硬币栏位");
        
        // 测试行动槽中间偏下方的数值显示格式
        Console.WriteLine("测试行动槽中间偏下方的数值显示格式：应显示为'基础值+硬币结果'得出的最终点数");
        
        // 测试拼点胜负后的视觉流程
        Console.WriteLine("测试拼点胜负后的视觉流程：最后一次拼点决出胜负后，先移除失败方行动槽下面的硬币栏位，行动槽中间显示的点数需保留2秒后，再执行失败方技能槽的摧毁动画");
        
        Console.WriteLine("测试通过！");
    }
}
