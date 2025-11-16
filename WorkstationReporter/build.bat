dotnet publish --self-contained -r win-x64 -c Release -o publish ^
-p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true