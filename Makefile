build:
	@dotnet build
run: build
	@./bin/Debug/netcoreapp3.1/DAG
exe:
	# Fully independed self contained executable app - Linux
	@dotnet publish -r linux-x64 --self-contained  --configuration Release -p:PublishSingleFile=true -o bin
exe-macos:
	# Fully independed self contained executable app - MacOS
	@dotnet publish -r osx.10.14-x64 --self-contained  --configuration Release -p:PublishSingleFile=true -o bin
exe-macos:
	# Fully independed self contained executable app - Win10
	@dotnet publish -r win10-x64 --self-contained  --configuration Release -p:PublishSingleFile=true -o bin
