up:
	cd _docker && docker compose --env-file ../.env up -d --no-build

down:
	cd _docker && docker compose --env-file ../.env down

restart:
	make down
	make up

restore:
	find app -maxdepth 1 -type f -name "*.csproj" -exec dotnet restore {} \;

rebuild:
	cd _docker && docker compose --env-file ../.env build
