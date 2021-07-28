# Build it
FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine AS build

WORKDIR /Silk
COPY . ./
RUN dotnet restore 

RUN dotnet publish ./src/Silk.Core/Silk.Core.csproj -c Release -o out 

# Run it
FROM mcr.microsoft.com/dotnet/runtime:5.0-alpine

# Update OpenSSL for the bot to properly work (Discord sucks)
RUN apk upgrade --update-cache --available && \
    apk add openssl && \
    rm -rf /var/cache/apk/*

# Music commands *will* break without this.
RUN apk add opus && apk add libsodium
RUN ln -s /usr/lib/x86_64-linux-gnu/libopus.so.0 /usr/lib/x86_64-linux-gnu/opus.so

WORKDIR /Silk
COPY --from=build /Silk/out .

RUN chmod +x ./Silk.Core

CMD ["./Silk.Core"]
