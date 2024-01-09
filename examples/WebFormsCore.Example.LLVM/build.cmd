set EMSDK=C:\Sources\emsdk

call %EMSDK%\emsdk activate 3.1.23
dotnet publish -c Release
