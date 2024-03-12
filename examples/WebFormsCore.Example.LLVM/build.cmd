set EMSDK=C:\Sources\emsdk

copy /b Startup.cs +,,
call %EMSDK%\emsdk activate 3.1.47
dotnet publish -c Release
pause