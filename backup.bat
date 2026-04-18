# 在项目根目录创建 backup.bat
@echo off
echo 开始备份回合制RPG项目...
cd /d "%~dp0"
git add .
git commit -m "自动备份: %date% %time%"
git push origin master
echo 备份完成！
pause