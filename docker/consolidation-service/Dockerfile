FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["CashFlow.Consolidation.API/CashFlow.Consolidation.API.csproj", "CashFlow.Consolidation.API/"]
COPY ["CashFlow.Consolidation.Application/CashFlow.Consolidation.Application.csproj", "CashFlow.Consolidation.Application/"]
COPY ["CashFlow.Consolidation.Domain/CashFlow.Consolidation.Domain.csproj", "CashFlow.Consolidation.Domain/"]
COPY ["CashFlow.Consolidation.Infrastructure/CashFlow.Consolidation.Infrastructure.csproj", "CashFlow.Consolidation.Infrastructure/"]
COPY ["CashFlow.Shared/CashFlow.Shared.csproj", "CashFlow.Shared/"]
RUN dotnet restore "CashFlow.Consolidation.API/CashFlow.Consolidation.API.csproj"

# Copy the rest of the code
COPY . .

# Build the application
WORKDIR "/src/CashFlow.Consolidation.API"
RUN dotnet build "CashFlow.Consolidation.API.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "CashFlow.Consolidation.API.csproj" -c Release -o /app/publish

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CashFlow.Consolidation.API.dll"]