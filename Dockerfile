# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first so the package layer is cached until a project file changes.
COPY global.json Directory.Build.props .editorconfig ./
COPY src/ClinicSlot.Domain/ClinicSlot.Domain.csproj src/ClinicSlot.Domain/
COPY src/ClinicSlot.Application/ClinicSlot.Application.csproj src/ClinicSlot.Application/
COPY src/ClinicSlot.Infrastructure/ClinicSlot.Infrastructure.csproj src/ClinicSlot.Infrastructure/
COPY src/ClinicSlot.Api/ClinicSlot.Api.csproj src/ClinicSlot.Api/
RUN dotnet restore src/ClinicSlot.Api/ClinicSlot.Api.csproj

COPY src/ src/
RUN dotnet publish src/ClinicSlot.Api/ClinicSlot.Api.csproj \
    --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

RUN mkdir -p /app/data && chown app:app /app/data
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ConnectionStrings__ClinicSlot="Data Source=/app/data/clinicslot.db"

EXPOSE 8080
VOLUME ["/app/data"]
USER app

ENTRYPOINT ["dotnet", "ClinicSlot.Api.dll"]
