::This file created to start compile.bat from console command line
::and compile without lagging WMIC, and without creating WMICTempBatchFile.bat
::exit - added to compile.bat
::Window will be closed, after compilation, now.

::Now, compilation including next steps:
::1. Copy path_of_folder with compile.bat.
::2. WinKey + R -> cmd.exe
::3. cd path_of_folder
::4. start_compile.bat
::5. Another window opened, compilation successfully, press any key, and window is closing.

start compile.bat %1