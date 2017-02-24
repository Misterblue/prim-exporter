..\..\..\flatc-win.exe --csharp Vector3.fbs
..\..\..\flatc-win.exe --csharp Quaternion.fbs
..\..\..\flatc-win.exe --csharp MeshInstance.fbs

move /Y .\InWorldz\PrimExporter\ExpLib\ImportExport\BabylonFlatBuffers\* ..\ImportExport\BabylonFlatBuffers
rmdir /S/Q .\InWorldz
