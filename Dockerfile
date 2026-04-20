FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["ErpDataImportValidator.sln", "./"]
COPY ["src/ErpDataImportValidator.Api/ErpDataImportValidator.Api.csproj", "src/ErpDataImportValidator.Api/"]
COPY ["src/ErpDataImportValidator.Application/ErpDataImportValidator.Application.csproj", "src/ErpDataImportValidator.Application/"]
COPY ["src/ErpDataImportValidator.Domain/ErpDataImportValidator.Domain.csproj", "src/ErpDataImportValidator.Domain/"]
COPY ["src/ErpDataImportValidator.Infrastructure/ErpDataImportValidator.Infrastructure.csproj", "src/ErpDataImportValidator.Infrastructure/"]

RUN dotnet restore "ErpDataImportValidator.sln"

COPY . .
RUN dotnet publish "src/ErpDataImportValidator.Api/ErpDataImportValidator.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "ErpDataImportValidator.Api.dll"]
