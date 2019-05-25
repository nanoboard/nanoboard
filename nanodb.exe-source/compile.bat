::Without adding pathway to path
::I:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe /p:Configuration=Release nanodb.csproj

::x86 and x64
::I:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe /p:OutputPath="../x64/" /p:IntermediateOutputPath="../x86/" /property:Configuration=Release nanodb.csproj

::x86 only, in the main folder. Ready to start.
I:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe /p:IntermediateOutputPath="../" /property:Configuration=Release nanodb.csproj

::x64 only, in the main folder. Ready to start.
::I:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe /p:OutputPath="../" /property:Configuration=Release nanodb.csproj

::I:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe nanodb.csproj



::Generate ../pages/version.txt, like in old version of nanoboard.
::Previous - string format:
::Thu Feb  14 06:35:54 EST 2019
::      (12) - two whitespaces.
::+ "LF" (in the end) - not CRLF
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

::LF without CRLF
setlocal EnableDelayedExpansion
(set \n=^
%=Do not remove this line=%
)
::Usage:
::(echo Line1!\n!Line2
::echo Works also with quotes "!\n!line2") > ../pages/version2.txt
::But CRLF in the end.

::Create version.txt and write date and time in old canonical format
::Write multistring to file, using relative pathway

::Delete previous file if exists
del "../pages/version.txt"
::Write date and time - with LF and without CRLF in the end:
set /p ="%DOW% %Mon%  %Day% %TIME:~0,-3% EST %YEAR%!\n!"<nul >> "../pages/version.txt"

::don't close window, after all
pause
exit