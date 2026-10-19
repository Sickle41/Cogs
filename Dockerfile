FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/WarmachineAPI/WarmachineAPI.csproj", "src/WarmachineAPI/"]
RUN dotnet restore "src/WarmachineAPI/WarmachineAPI.csproj"

COPY src/WarmachineAPI/ src/WarmachineAPI/
RUN dotnet publish "src/WarmachineAPI/WarmachineAPI.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "WarmachineAPI.dll"]
