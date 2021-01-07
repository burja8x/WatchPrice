#FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
#WORKDIR /app
#EXPOSE 80
#
#FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
#WORKDIR /src
#COPY ["Ros4.csproj", ""]
#RUN dotnet restore "./Ros4.csproj"
#COPY . .
#WORKDIR "/src/."
#RUN dotnet build "Ros4.csproj" -c Release -o /app/build
#
#FROM build AS publish
#RUN dotnet publish "Ros4.csproj" -c Release -o /app/publish
#
#FROM base AS final
#WORKDIR /app
#COPY --from=publish /app/publish .
#ENTRYPOINT ["dotnet", "Ros4.dll"]


FROM mcr.microsoft.com/dotnet/aspnet:3.1
WORKDIR /app
EXPOSE 80
COPY /app/publish .
ENTRYPOINT ["dotnet", "Ros4.dll"]