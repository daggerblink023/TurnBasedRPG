@echo off
chcp 65001 >nul
echo 开始备份回合制RPG项目...
cd /d "%~dp0"
git add .
git commit -m "自动备份: %date% %time%"
git push origin master
echo 备份完成！
pause