build:
	dotnet build -c Release
	cp bin/Release/net4.8/StorageProcessing.dll ~/U3DS/Servers/RocketMod/Servers/RocketMod/Rocket/Plugins/StorageProcessing.dll
