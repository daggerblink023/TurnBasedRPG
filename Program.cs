// 设置控制台编码为UTF-8
System.Console.OutputEncoding = new System.Text.UTF8Encoding(true);
using var game = new TurnBasedRPG.Game1();
game.Run();
