# Use the official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /app

# Copy the csproj and restore any dependencies
COPY ImagePuller/ImagePuller/ImagePuller.csproj ./ImagePuller/
RUN dotnet restore ImagePuller/ImagePuller.csproj

# Copy the rest of the application and build it
COPY ImagePuller/ImagePuller/ ./ImagePuller/
RUN dotnet publish ImagePuller/ImagePuller.csproj -c Release -o out

# Build the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build-env /app/out .

# Install bash for the script execution
RUN apt-get update && apt-get install -y bash && rm -rf /var/lib/apt/lists/*

# Expose the port the app runs on
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Command to run the application
ENTRYPOINT ["dotnet", "ImagePuller.dll"]
