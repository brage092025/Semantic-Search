FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Copy csproj and restore
# Context is 'src', so we look in the 'Api' folder
COPY Api/Api.csproj Api/
RUN dotnet restore Api/Api.csproj

# Copy everything and build
COPY Api/ Api/
COPY Stories/ Stories/
RUN dotnet publish Api/Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

# The app expects a 'Stories' folder in the current directory for seeding
COPY Stories/ /app/Stories/

# Default ASP.NET Core port
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Api.dll"]
