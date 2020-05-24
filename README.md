# CODSCRIPT

Installation:
- Copy content from folder 'cod4' to your base cod4 folder (where iw3mp.exe is located)
- Copy content from folder 'npp' to your base Notepad++ folder (where is notepad++.exe located)
- Modify params in plugins\CODSCRIPTNpp\settings.dat (set workingDir, settingsFile and FSGameFolderName)

CODSCRIPT:
- New syntax features (constants, usings, foreach, do...while)
- Support of NINJA modified exe and custom functions

- bin/CODSCRIPT/settings.xml
  - this file needs to be set for every mod!!!
  - contains all setting for currently developed map/mod
  - you can develope maps simultaleously, however, only one mod at a time is supported

- bin/CODSCRIPT/scriptinfo
  - contains informations about files, methods, API, etc.
  - under normal circumstances, you do not have to touch these files

New syntax:
// compile-time constants
// [private|public] name = value;
public IA_Flags_NoCollide = 1;
// use in code
self IA_DisableFlag( IA_FLAGS_NOCOLLIDE );

// usings
// [private|public] using name = file path;
public using C_IMAPVARS = custom\include\_mapvars;
// use in code
sPopInfo = C_IMAPVARS::Get(sMapName, "pop");
// instead of
sPopInfo = custom\include\_mapvars::Get(sMapName, "pop");

// foreach
foreach (var in array)
  statement;

foreach (var in array)
{
  statement;
}

// do...while
do
  statement;
while (exp);

do
{
  statement;
}
while (exp);

Notepad++ plugin:
- Maximum supported version of notepad++ is 7.5.6!
- Code coloured markup
- Full database od COD4 API
- IntelliSense style helper to writing code
- SolutionExplorer style tree with currently developed mod/map
- Compiling source code from NPP

- plugins/CODSCRIPTNpp/settings.dat
  - workingDir - path to your cod4 base folder + \bin\CODSCRIPT
  - workingDirList - alternate base folders (if you have more base folder and want to switch between them), 
                   path has to be in same form as workingDir
  - settingsFile - path to currently developed mod (contains all setting connected to you mod)
  - settingsFileList - alternate mods settings (if you have more mods in same base folder)
  - FSGameFolderName - name of folder of you mod
  - save_ShowErrorMsgBox - ???
  - compile_StartImmediately - ???
  - compile_CloseAfterCompile - ???
  - compile_Raw - ???
  - compile_CompareDate - ???
  
Developed between 2013-08-04 to 2014-07-04.
