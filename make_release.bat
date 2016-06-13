:: require GnuWin32 and msys to work properly

@echo off
setlocal

if [%1]==[] goto usage
if [%2]==[] goto usage
if [%3]==[] goto usage
if [%4]==[] goto usage

goto start

:usage
echo Usage:
echo make_release.bat [output_directory] [major] [minor] [build]
goto end

:start

set callingdir=%cd%

set versionsuffix=%2%
set versionsuffix=%versionsuffix%_%3%
set versionsuffix=%versionsuffix%_%4%

set release=%1%\AllDataSheetFinder_Make_Release_Temp\
set files=%release%Files\

set outputupdate=%1%\AllDataSheetFinder_Update
set outputupdate=%outputupdate%_%versionsuffix%
set outputupdate=%outputupdate%.zip

set output=%1%\AllDataSheetFinder
set output=%output%_%versionsuffix%
set output=%output%.zip

echo Following directories and files will be modified:
echo %release%
echo %outputupdate%
echo %output%
set /P prompt=Do you want to continue (Y/N)? 
if /I "%prompt%" NEQ "Y" goto end

if not exist %release% goto norelease
set /P prompt=%release% exists and will be removed! Do you want to continue (Y/N)? 
if /I "%prompt%" NEQ "Y" goto end
echo Removing %release%
rm -r %release%
:norelease

if not exist %outputupdate% goto nooutputupdate
set /P prompt=%outputupdate% exists and will be removed! Do you want to continue (Y/N)? 
if /I "%prompt%" NEQ "Y" goto end
echo Removing %outputupdate%
rm %outputupdate%
:nooutputupdate

if not exist %output% goto nooutput
set /P prompt=%output% exists and will be removed! Do you want to continue (Y/N)? 
if /I "%prompt%" NEQ "Y" goto end
echo Removing %output%
rm %output%
:nooutput

echo Creating directories
mkdir %release%
mkdir %files%

echo Copying files
cp -r %cd%\AllDataSheetFinder\bin\Release\Languages %files%
cp %cd%\AllDataSheetFinder\bin\Release\AllDataSheetFinder.exe %files%
cp %cd%\AllDataSheetFinder\bin\Release\AllDataSheetFinder.exe.config %files%
cp %cd%\AllDataSheetFinder\bin\Release\HtmlAgilityPack.dll %files%
cp %cd%\AllDataSheetFinder\bin\Release\Ionic.Zip.dll %files%
cp %cd%\AllDataSheetFinder\bin\Release\MigraDoc.DocumentObjectModel-wpf.dll %files%
cp %cd%\AllDataSheetFinder\bin\Release\MigraDoc.Rendering-wpf.dll %files%
cp %cd%\AllDataSheetFinder\bin\Release\MigraDoc.RtfRendering-wpf.dll %files%
cp %cd%\AllDataSheetFinder\bin\Release\MVVMUtils.dll %files%
cp %cd%\AllDataSheetFinder\bin\Release\MVVMUtilsExt.dll %files%
cp %cd%\AllDataSheetFinder\bin\Release\PdfSharp.Charting-wpf.dll %files%
cp %cd%\AllDataSheetFinder\bin\Release\PdfSharp-wpf.dll %files%
cp %cd%\AllDataSheetFinder\bin\Release\System.Windows.Interactivity.dll %files%
cp %cd%\DEPENDENCIES_LICENSES.txt %files%
cp %cd%\ICONS_LICENSES.txt %files%
cp %cd%\LICENSE.txt %files%

cp %cd%\AllDataSheetFinderUpdater\bin\Release\AllDataSheetFinderUpdater.exe %release%

echo Zipping update
cd %release%
zip -q -r %outputupdate% .
cd %callingdir%

echo Zipping release
cd %files%
zip -q -r %output% .
cd %callingdir%

echo Removing temporary files
rm -r %release%

:end
endlocal