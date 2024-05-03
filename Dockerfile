FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build-env
WORKDIR /App
ADD *.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish --no-restore --configuration Release --output out

FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine
WORKDIR /App
RUN apk add --no-cache tini
COPY scripts /usr/local/bin/
COPY --from=build-env /App/out .
ENV DOCKER_SOCKET=/var/run/docker.sock
ENV LOG_LEVEL=Info
ENV HEALTHCHECK_PATH=/tmp/health
ENTRYPOINT ["/sbin/tini", "--"]
CMD ["dotnet", "ReflectiveCode.DockerMonitor.dll"]
HEALTHCHECK --interval=30s --timeout=1s --retries=3 CMD health.sh || exit 1
