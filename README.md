# Image Pull and Deploy Tool

A lightweight .NET-based webhook receiver designed to automate the deployment of Docker images.

## Features
- **Webhook Integration**: Triggered by GitHub Actions or other CI/CD pipelines.
- **Automated Pull**: Automatically pulls the latest version of a specified Docker image.
- **Seamless Restart**: Stops and removes existing containers before starting the updated version.
- **Configurable**: Easily define image names, ports, and environment variables.

## How it Works
1. A GitHub Webhook sends a POST request to this tool.
2. The tool validates the request.
3. It executes a `docker pull` command for the target image.
4. It restarts the container with the new image.
