cd ./Protoc/protoc-3.19.0-win64/bin/
protoc ^
--csharp_out=./../../../../SL_Server/PB/ ^
--proto_path=./../../../ProtoFiles/ ^
LaunchPB.proto
pause