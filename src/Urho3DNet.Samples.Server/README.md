# Building docker container

https://docs.microsoft.com/en-us/dotnet/core/docker/build-container


Docker file points to "bin/Release/netcoreapp3.1/publish". To populate the folder publish the project.

To build the image:
```bash
docker build -t "urho3dnet-server-image" -f Dockerfile .
```

You can create a docker container form the image:
```bash
docker create --name "urho3dnet-server" "urho3dnet-server-image"
```

Or you can run locally with interactive terminal and clean up on exit:
```bash
docker run -p 2345:2345/udp -it --rm "urho3dnet-server-image"
```

# Setting up Azure infrastructure

Login into Azure could with the following command:
```bash
az login
```

Pick a location from available datacenters:
```bash
az account list-locations
```
I'll use northeurope for a sample.

Create Azure Resource Group in a region you would like to use:
```bash
az group create -l northeurope -n urho3dnet-server-rg --subscription <subscription-id>
```
Subscription argument is optional. You can use it if you have multiple subscriptions active on your account.

Now we need a container registy in the resource group:
```bash
az acr create --resource-group urho3dnet-server-rg --name urho3dnetservercontainers --sku Basic
```
Pick a low-case name for the registry to avoid authentication errors later.

# Deploying image to Azure

Tag image with container registry name:
```bash
docker image tag urho3dnet-server-image urho3dnetservercontainers.azurecr.io/urho3dnet-server-image:latest
```

Check that it is tagged correctly:
```bash
docker image ls -a
```

Now we need to enable Admin to get the credential of acr which we will be using to login into acr through cli. Run the following command to do so:
```bash
az acr update -n urho3dnetservercontainers -g urho3dnet-server-rg --admin-enabled true
az acr credential show -n urho3dnetservercontainers -g urho3dnet-server-rg
```

We would login into acr using the above credential, use the following command to do so:
```bash
az acr login -n urho3dnetservercontainers -u urho3dnetservercontainers -p <passwords>
```

Push the image to Azure Container Registry using the following command
```bash
docker push urho3dnetservercontainers.azurecr.io/urho3dnet-server-image:latest
```

Get Azure Container Registry credential using following command:
```bash
az acr credential show -n urho3dnetservercontainers -g urho3dnet-server-rg
```

Use the following command to create an Azure Container Instance from an Image on Azure Container Registry, when prompted for credential use the credential from previous step
```bash
az container create -g urho3dnet-server-rg --name urho3dnet-server --image urho3dnetservercontainers.azurecr.io/urho3dnet-server-image:latest --cpu 1 --memory 1 --dns-name-label urho3dnet-server --port 2345 --protocol UDP
```
