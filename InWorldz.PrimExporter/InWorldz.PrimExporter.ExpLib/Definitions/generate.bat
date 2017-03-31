..\..\..\flatc-win.exe --csharp Vector3.fbs
..\..\..\flatc-win.exe --csharp Quaternion.fbs
..\..\..\flatc-win.exe --csharp MeshInstance.fbs
..\..\..\flatc-win.exe --csharp Material.fbs
..\..\..\flatc-win.exe --csharp Texture.fbs
..\..\..\flatc-win.exe --csharp MultiMaterial.fbs
..\..\..\flatc-win.exe --csharp SubMesh.fbs
..\..\..\flatc-win.exe --csharp Mesh.fbs
..\..\..\flatc-win.exe --csharp BabylonFileFlatbuffer.fbs

move /Y .\InWorldz\PrimExporter\ExpLib\ImportExport\BabylonFlatBuffers\* ..\ImportExport\BabylonFlatBuffers
rmdir /S/Q .\InWorldz
