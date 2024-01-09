set EMSDK=C:\Sources\emsdk

call %EMSDK%\emsdk activate 3.1.23
touch Interop.cs
dotnet publish -c Release
