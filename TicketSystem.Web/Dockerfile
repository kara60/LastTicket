## See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.
#
## This stage is used when running from VS in fast mode (Default for Debug configuration)
#FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
#USER $APP_UID
#WORKDIR /app
#EXPOSE 8080
#EXPOSE 8081
#
#
## This stage is used to build the service project
#FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
#ARG BUILD_CONFIGURATION=Release
#WORKDIR /src
#COPY ["TicketSystem.Web/TicketSystem.Web.csproj", "TicketSystem.Web/"]
#RUN dotnet restore "./TicketSystem.Web/TicketSystem.Web.csproj"
#COPY . .
#WORKDIR "/src/TicketSystem.Web"
#RUN dotnet build "./TicketSystem.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build
#
## This stage is used to publish the service project to be copied to the final stage
#FROM build AS publish
#ARG BUILD_CONFIGURATION=Release
#RUN dotnet publish "./TicketSystem.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false
#
## This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
#FROM base AS final
#WORKDIR /app
#COPY --from=publish /app/publish .
#ENTRYPOINT ["dotnet", "TicketSystem.Web.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/TicketSystem.Web/TicketSystem.Web.csproj", "src/TicketSystem.Web/"]
COPY ["src/TicketSystem.Application/TicketSystem.Application.csproj", "src/TicketSystem.Application/"]
COPY ["src/TicketSystem.Domain/TicketSystem.Domain.csproj", "src/TicketSystem.Domain/"]
COPY ["src/TicketSystem.Infrastructure/TicketSystem.Infrastructure.csproj", "src/TicketSystem.Infrastructure/"]
COPY ["src/TicketSystem.Shared/TicketSystem.Shared.csproj", "src/TicketSystem.Shared/"]

RUN dotnet restore "src/TicketSystem.Web/TicketSystem.Web.csproj"
COPY . .
WORKDIR "/src/src/TicketSystem.Web"
RUN dotnet build "TicketSystem.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TicketSystem.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TicketSystem.Web.dll"]

// docker-compose.yml
version: '3.8'

services:
  ticketsystem-db:
    image: postgres:15
    container_name: ticketsystem-postgres
    environment:
      POSTGRES_DB: TicketSystemDb
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres123
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    networks:
      - ticketsystem-network

  ticketsystem-web:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: ticketsystem-web
    ports:
      - "8080:80"
      - "8443:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Host=ticketsystem-db;Database=TicketSystemDb;Username=postgres;Password=postgres123;Port=5432
    depends_on:
      - ticketsystem-db
    networks:
      - ticketsystem-network
    volumes:
      - ./uploads:/app/wwwroot/uploads

volumes:
  postgres_data:

networks:
  ticketsystem-network:
    driver: bridge