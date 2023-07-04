FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS build-env
WORKDIR /App
COPY . ./
RUN dotnet restore
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine
RUN apk add --no-cache icu-libs
WORKDIR /App
COPY --from=build-env /App/out .
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV LC_ALL=en_US.UTF-8
ENV LANG=en_US.UTF-8
ENV DOCKER_SOCKET=/var/run/docker.sock
ENV LOG_LEVEL=Info
ENTRYPOINT ["dotnet", "ReflectiveCode.DockerMonitor.dll"]
