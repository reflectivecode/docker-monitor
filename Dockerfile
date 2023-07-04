FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS build-env
WORKDIR /App
COPY . ./
RUN dotnet restore
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:7.0-alpine
WORKDIR /App
COPY --from=build-env /App/out .
ENV DOCKER_SOCKET=/var/run/docker.sock
ENV LOG_LEVEL=Info
ENTRYPOINT ["dotnet", "ReflectiveCode.DockerMonitor.dll"]
