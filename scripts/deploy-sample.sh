#!/bin/bash

# Configuration
IMAGE_NAME="your-docker-image:latest"
CONTAINER_NAME="your-container-name"

echo "Pulling latest image: $IMAGE_NAME"
docker pull $IMAGE_NAME

echo "Stopping and removing existing container: $CONTAINER_NAME"
docker stop $CONTAINER_NAME || true
docker rm $CONTAINER_NAME || true

echo "Starting new container..."
docker run -d --name $CONTAINER_NAME -p 8080:80 $IMAGE_NAME

echo "Deployment complete."
