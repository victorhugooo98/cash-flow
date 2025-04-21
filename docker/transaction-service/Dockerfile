FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["CashFlow.Transaction.API/CashFlow.Transaction.API.csproj", "CashFlow.Transaction.API/"]
COPY ["CashFlow.Transaction.Application/CashFlow.Transaction.Application.csproj", "CashFlow.Transaction.Application/"]
COPY ["CashFlow.Transaction.Domain/CashFlow.Transaction.Domain.csproj", "CashFlow.Transaction.Domain/"]
COPY ["CashFlow.Transaction.Infrastructure/CashFlow.Transaction.Infrastructure.csproj", "CashFlow.Transaction.Infrastructure/"]
COPY ["CashFlow.Shared/CashFlow.Shared.csproj", "CashFlow.Shared/"]
RUN dotnet restore "CashFlow.Transaction.API/CashFlow.Transaction.API.csproj"

# Copy the rest of the code
COPY . .

# Build the application
WORKDIR "/src/CashFlow.Transaction.API"
RUN dotnet build "CashFlow.Transaction.API.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "CashFlow.Transaction.API.csproj" -c Release -o /app/publish

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CashFlow.Transaction.API.dll"]