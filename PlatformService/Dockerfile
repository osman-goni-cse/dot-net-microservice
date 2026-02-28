# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
WORKDIR /App

COPY . ./

RUN dotnet restore
RUN dotnet publish -c Release -o out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /App

# Expose port 80 (inside container)
EXPOSE 80

# Copy published app
COPY --from=build-env /App/out .

# Make ASP.NET Core listen on all interfaces
ENV ASPNETCORE_URLS=http://+:80

ENTRYPOINT ["dotnet", "PlatformService.dll"]
