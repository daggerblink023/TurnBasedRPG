@echo off
echo Running TurnBasedRPG...
echo ============================
dotnet run > output.txt 2>&1
echo ============================
echo Output saved to output.txt
type output.txt
pause