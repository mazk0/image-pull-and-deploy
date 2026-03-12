# Image Pull and Deploy Tool

A lightweight .NET-based webhook receiver designed to automate the deployment of Docker images via GitHub Webhooks.

## Features
- **Webhook Integration**: Securely triggered by GitHub using HMAC-SHA256 signature verification.
- **Background Execution**: Runs deployment scripts in the background to ensure immediate response to the webhook.
- **Seq Logging**: Integrated with Serilog and Seq for centralized, structured logging.
- **Docker Ready**: Includes a Dockerfile for easy containerized deployment.

## How to Use

### 1. GitHub Webhook Setup
1. Go to your GitHub repository **Settings** > **Webhooks** > **Add webhook**.
2. **Payload URL**: `http://your-server-ip:8080/webhook`
3. **Content type**: `application/json`
4. **Secret**: Enter a secure string (this must match your configuration).
5. **Which events**: `Just the push event` (or as needed).

### 2. Configuration
The application uses `appsettings.json` for configuration, which can be overridden by environment variables:

| Setting | Environment Variable | Description |
|---------|----------------------|-------------|
| `Webhook:Secret` | `Webhook__Secret` | The secret string entered in GitHub. |
| `Webhook:DeploymentScriptPath` | `Webhook__DeploymentScriptPath` | Path to the bash script to execute (Default: `/scripts/deploy.sh`). |
| `Serilog:WriteTo:1:Args:serverUrl` | `Serilog__WriteTo__1__Args__serverUrl` | URL to your Seq instance (Default: `http://seq:5341`). |

### 3. Deployment Script
The tool expects a bash script at the configured path. Ensure the script is executable (`chmod +x deploy.sh`).

**Example `deploy.sh`:**
```bash
#!/bin/bash
docker pull my-repo/my-image:latest
docker stop my-container || true
docker rm my-container || true
docker run -d --name my-container -p 80:80 my-repo/my-image:latest
```

### 4. Docker Deployment
When running this tool in a container, you must mount the host's Docker socket and your deployment script:

```yaml
services:
  image-puller:
    build: .
    ports:
      - "8080:8080"
    environment:
      - Webhook__Secret=your_secret_here
      - Webhook__DeploymentScriptPath=/scripts/deploy.sh
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
      - ./scripts/deploy.sh:/scripts/deploy.sh
```

## Security
- The tool validates the `X-Hub-Signature-256` header from GitHub to ensure requests are authentic.
- Ensure your webhook secret is strong and kept private.
