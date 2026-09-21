FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app

RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 8080

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY Directory.Packages.props .
COPY Directory.Build.props .
COPY . .

RUN dotnet restore "Web/PublicSite/MUnique.OpenMU.Web.PublicSite.csproj"

WORKDIR /src/Web/PublicSite
RUN dotnet publish "MUnique.OpenMU.Web.PublicSite.csproj" \
    -c Release \
    -o /app/publish \
    -p:ci=true

FROM build AS publish

FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .

RUN mkdir -p /app/Data && chmod 777 /app/Data

ARG APP_UID=1000
USER ${APP_UID}

ENTRYPOINT ["dotnet", "MUnique.OpenMU.Web.PublicSite.dll"]
