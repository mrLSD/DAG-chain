build:
	@dotnet build
run: build
	@./bin/Debug/netcoreapp3.1/DAG
exe:
	# Fully independed self contained executable app
	@dotnet publish -r linux-x64 --self-contained  --configuration Release -p:PublishSingleFile=true -o bin
