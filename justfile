publish:
	dotnet publish src/SFD.ImageGenerator.csproj -c Release -f net10.0-windows -r win-x64 --self-contained true -p:PublishSingleFile=true
	dotnet publish src/SFD.ImageGenerator.csproj -c Release -f net10.0 -r linux-x64 --self-contained true -p:PublishSingleFile=true
