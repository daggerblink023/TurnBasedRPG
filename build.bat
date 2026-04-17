@echo off
echo 开始构建单文件可执行程序...
dotnet publish TurnBasedRPG.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true -o ./publish
echo 构建完成！可执行文件已生成到 ./publish 目录
pause