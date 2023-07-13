FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS build-env
WORKDIR /App
ADD *.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish --configuration Release --output out

FROM mcr.microsoft.com/dotnet/runtime:7.0-alpine
WORKDIR /App
COPY scripts /usr/local/bin/
COPY --from=build-env /App/out .
ENV DOCKER_SOCKET=/var/run/docker.sock
ENV LOG_LEVEL=Info
ENV HEALTHCHECK_PATH=/tmp/health
ENTRYPOINT ["dotnet", "ReflectiveCode.DockerMonitor.dll"]
HEALTHCHECK --interval=30s --timeout=1s --retries=3 CMD health.sh || exit 1
