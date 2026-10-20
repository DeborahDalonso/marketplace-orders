FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore separado do resto para o cache de camadas segurar o download de pacotes.
COPY Marketplace.Orders.sln .
COPY src/Marketplace.Orders/Marketplace.Orders.csproj src/Marketplace.Orders/
COPY tests/Marketplace.Orders.Tests/Marketplace.Orders.Tests.csproj tests/Marketplace.Orders.Tests/
RUN dotnet restore

COPY . .
RUN dotnet publish src/Marketplace.Orders/Marketplace.Orders.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

ENV PORT=3002
EXPOSE 3002
USER app

ENTRYPOINT ["dotnet", "Marketplace.Orders.dll"]
