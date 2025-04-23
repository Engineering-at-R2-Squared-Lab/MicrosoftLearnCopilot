
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /app/publish

RUN APP_DLL=$(find /app/publish -name "*.dll" ! -name "*.Views.dll" ! -name "*.runtimeconfig.json" -type f -printf "%f\n" | head -1) \
    && echo "#!/bin/bash\ndotnet /app/${APP_DLL}" > /app/publish/entrypoint.sh \
    && chmod +x /app/publish/entrypoint.sh

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["/app/entrypoint.sh"]