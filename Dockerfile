# Multi-stage build for the OnlineTeacher ASP.NET Core API (net10.0).
# Build context is the repository root.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the project files for the API and its transitive project references, then restore
# the API project only (the solution includes test projects not needed at this stage).
COPY src/OnlineTeacher.Api/OnlineTeacher.Api.csproj src/OnlineTeacher.Api/
COPY src/OnlineTeacher.Application/OnlineTeacher.Application.csproj src/OnlineTeacher.Application/
COPY src/OnlineTeacher.Domain/OnlineTeacher.Domain.csproj src/OnlineTeacher.Domain/
COPY src/OnlineTeacher.Infrastructure/OnlineTeacher.Infrastructure.csproj src/OnlineTeacher.Infrastructure/
RUN dotnet restore src/OnlineTeacher.Api/OnlineTeacher.Api.csproj

# Copy source and publish a framework-dependent, non-self-contained build.
COPY src/ src/
WORKDIR /src/src/OnlineTeacher.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Production runtime image.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "OnlineTeacher.Api.dll"]