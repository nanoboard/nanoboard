::Without adding pathway to path
::I:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe /p:Configuration=Release nanodb.csproj
I:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe /property:Configuration=Release nanodb.csproj
::I:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe nanodb.csproj



::Generate ../pages/version.txt, like in old version of nanoboard.
::Previous - string format:
::Thu Feb  14 06:35:54 EST 2019
::      (12)
@echo off
REM Get day of week number, Sunday = 0
for /f "skip=2 tokens=2 delims=," %%a in ('WMIC Path Win32_LocalTime Get DayOfWeek /Format:csv') do set /a DowNum=%%a + 1
REM Convert day of week number to text abbreviation
for /f "tokens=%DowNum%" %%a in ("Sun Mon Tue Wed Thu Fri Sat") do set DOW=%%a
::Show day of week
::echo day_of_week = %DOW%

REM Get Month number, Jan = 0;
for /f "skip=1 tokens=2 delims=," %%b in ('WMIC Path Win32_LocalTime Get Month /Format:csv') do set /a MonNum=%%b
REM Convert day of week number to text abbreviation
for /f "tokens=%MonNum%" %%b in ("Jan Feb Mar Apr May Jun Jul Aug Sep Oct Nov Dec") do set Mon=%%b
::Show Month number
::echo Month = %Mon%

REM Just Get DAY number;
for /f "skip=1 tokens=2 delims=," %%c in ('WMIC Path Win32_LocalTime Get Day /Format:csv') do set /a Day=%%c
::Show day number
::echo Day = %Day%

REM Time without milliseconds
::Show Time without milliseconds
::echo TIME_WITHOUT_MILLISECONDS = %TIME:~0,-3%
FOR /F "skip=1 tokens=2 delims=," %%g IN ('WMIC Path Win32_LocalTime Get Year /Format:csv') DO set /a YEAR=%%g
::Show year YYYY
::echo YEAR = %YEAR%

::Write multistring to file, using relative pathway
(echo %DOW% %Mon%  %Day% %TIME:~0,-3% EST %YEAR%
) > ../pages/version.txt

::don't close window
pause
