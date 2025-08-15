# TicketSystem.Web/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY TicketSystem.sln ./
COPY TicketSystem.Application/TicketSystem.Application.csproj TicketSystem.Application/
COPY TicketSystem.Domain/TicketSystem.Domain.csproj TicketSystem.Domain/
COPY TicketSystem.Infrastructure/TicketSystem.Infrastructure.csproj TicketSystem.Infrastructure/
COPY TicketSystem.Shared/TicketSystem.Shared.csproj TicketSystem.Shared/
COPY TicketSystem.Web/TicketSystem.Web.csproj TicketSystem.Web/
RUN dotnet restore TicketSystem.Web/TicketSystem.Web.csproj
COPY . .
RUN dotnet build TicketSystem.Web/TicketSystem.Web.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish TicketSystem.Web/TicketSystem.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TicketSystem.Web.dll"]