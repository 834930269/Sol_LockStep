@echo off
rem 查找文件  
for /f "delims=" %%i in ('dir /b ".\ProtoFiles\*.proto"') do echo %%i   
rem 转cpp  for /f "delims=" %%i in ('dir /b/a "*.proto"') do protoc -I=. --cpp_out=. %%i  
for /f "delims=" %%i in ('dir /b/a ".\ProtoFiles\*.proto"') do .\Protoc\protoc-3.19.0-win64\bin\protoc --csharp_out=./../SL_Client/Assets/SL_Client/PB/ --proto_path=./ProtoFiles/ %%i  
pause