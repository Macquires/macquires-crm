FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY Tools/NetworkSimulator/NetworkSimulator.csproj Tools/NetworkSimulator/
RUN dotnet restore Tools/NetworkSimulator/NetworkSimulator.csproj
COPY Tools/NetworkSimulator/ Tools/NetworkSimulator/
RUN dotnet publish Tools/NetworkSimulator/NetworkSimulator.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "NetworkSimulator.dll"]
