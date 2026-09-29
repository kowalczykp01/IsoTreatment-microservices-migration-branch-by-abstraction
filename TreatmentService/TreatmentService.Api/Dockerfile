FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["TreatmentService/TreatmentService.Api/TreatmentService.Api.csproj", "TreatmentService/TreatmentService.Api/"]
COPY ["TreatmentService/TreatmentService.Domain/TreatmentService.Domain.csproj", "TreatmentService/TreatmentService.Domain/"]
COPY ["TreatmentService/TreatmentService.Application/TreatmentService.Application.csproj", "TreatmentService/TreatmentService.Application/"]
COPY ["TreatmentService/TreatmentService.Infrastructure/TreatmentService.Infrastructure.csproj", "TreatmentService/TreatmentService.Infrastructure/"]

RUN dotnet restore 'TreatmentService/TreatmentService.Api/TreatmentService.Api.csproj'

COPY ["TreatmentService", "TreatmentService/"]
RUN dotnet publish 'TreatmentService/TreatmentService.Api/TreatmentService.Api.csproj' -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
EXPOSE 8080
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT [ "dotnet", "TreatmentService.Api.dll" ]
