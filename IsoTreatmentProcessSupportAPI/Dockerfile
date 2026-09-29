FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["IsoTreatmentProcessSupportAPI/IsoTreatmentProcessSupportAPI.csproj", "IsoTreatmentProcessSupportAPI/"]
RUN dotnet restore 'IsoTreatmentProcessSupportAPI/IsoTreatmentProcessSupportAPI.csproj'

COPY ["IsoTreatmentProcessSupportAPI", "IsoTreatmentProcessSupportAPI/"]
RUN dotnet publish 'IsoTreatmentProcessSupportAPI/IsoTreatmentProcessSupportAPI.csproj' -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
EXPOSE 8080
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT [ "dotnet", "IsoTreatmentProcessSupportAPI.dll" ]
