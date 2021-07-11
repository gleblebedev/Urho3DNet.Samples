# 3.1-focal is based on Ubuntu-20.04. That matches libUrho3D.so build environment
FROM mcr.microsoft.com/dotnet/aspnet:3.1-focal

# Install dependencies
RUN apt-get update \
    && DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends \
        libgl-dev

# Server files copy
COPY bin/Release/netcoreapp3.1/publish App/
WORKDIR /App

EXPOSE 2345/udp

# Run the server
ENTRYPOINT ["dotnet", "Urho3DNet.Samples.Server.dll"]

