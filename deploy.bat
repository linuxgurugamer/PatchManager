
@echo off

set H=%KSPDIR%
set GAMEDIR=PatchManager
set GAMEDATA="GameData"
set VERSIONFILE=%GAMEDIR%.version

set DP0=r:\dp0\kspdev

copy /Y "%1%2" "%GAMEDATA%\%GAMEDIR%\Plugins"
copy /Y "%1%3".pdb "%GAMEDATA%\%GAMEDIR%\Plugins"

copy /Y %VERSIONFILE% %GAMEDATA%\%GAMEDIR%

xcopy /y /s /I %GAMEDATA%\%GAMEDIR% "%H%\GameData\%GAMEDIR%"
xcopy /y /s /I %GAMEDATA%\%GAMEDIR% "%DP0%\GameData\%GAMEDIR%"

rem pause
