
@echo off

set H=R:\KSP_1.5.1_dev
set GAMEDIR=PatchManager

echo %H%

copy /Y "%1%2" "GameData\%GAMEDIR%\Plugins"
copy /Y %GAMEDIR%.version GameData\%GAMEDIR%
xcopy /y/e/i PatchManager\Lang GameData\%GAMEDIR%\Lang
mkdir "%H%\GameData\%GAMEDIR%"
xcopy /y /s GameData\%GAMEDIR% "%H%\GameData\%GAMEDIR%"

