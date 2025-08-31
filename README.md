# Claude AI MCP

This project implements an MCP-compatible server that exposes practical tools for compliance and Azure operations, suitable for integration with MCP clients (e.g., Claude Desktop or an MCP gateway). It can run locally or in a container.

Note: Replace any TODO markers with your project-specific details as needed.

## Table of Contents
- Overview
- Features
- Requirements
- Quick Start
- MCP Client Configuration
- Configuration (Environment)
- Docker
- Project Structure
- Development
- Troubleshooting
- Security
- License

## Overview
- Purpose: Provide an MCP server exposing:
  - Compliance analysis and report generation.
  - Azure utilities (Blob text fetch, Log Analytics queries, resource info).
- Use cases:
  - Connect from an MCP client and invoke tools programmatically.
  - Automate compliance reviews or fetch operational data from Azure.

## Features
- MCP server over stdio transport.
- Compliance tools:
  - Analyze document content against RODO, ISO 27001, SOC 2 (configurable).
  - Generate structured audit reports from raw findings.
- Azure tools:
  - Download text content from Azure Blob Storage.
  - Execute KQL queries in Log Analytics.
  - Resource management helpers (requires appropriate permissions).
- Containerized runtime via Dockerfile and optional docker-compose.

## Requirements
- Runtime:
  - .NET 8 SDK for local builds and runs (or use the provided container).
- Accounts/credentials:
  - Anthropic API key for model calls.
  - Azure credentials compatible with DefaultAzureCredential (e.g., Azure CLI login, Managed Identity, or Service Principal).

## Quick Start

### 1) Set environment variables
Create a .env file or export variables in your shell:
```dotenv
# Required
ANTHROPIC_API_KEY=your_anthropic_key

# Optional (defaults to claude-3-haiku-20240307)
CLAUDE_MODEL=claude-3-5-sonnet-20240620

# Required for Azure Blob operations
AZURE_BLOB_ENDPOINT=https://yourstorageaccount.blob.core.windows.net

# For DefaultAzureCredential if using a Service Principal (one option)
# AZURE_TENANT_ID=...
# AZURE_CLIENT_ID=...
# AZURE_CLIENT_SECRET=...
```

Authenticate to Azure (one option):
```bash
az login
```

### 2) Restore, build, run
```bash
dotnet restore
dotnet build -c Release
dotnet run --project ./ClaudeMCP
```

The server uses stdio for MCP; it’s typically launched by an MCP client and not visited via HTTP.

## MCP Client Configuration

Example configuration snippet (adjust to your MCP client’s schema and location):
```json
{
  "mcpServers": {
    "claude-mcp": {
      "command": "dotnet",
      "args": ["ClaudeMCP.dll"]
    }
  }
}
```

Tips:
- If you prefer building on-the-fly, use: "args": ["run", "--project", "ClaudeMCP"].
- Ensure the working directory and paths match how your client launches the process.

## Configuration (Environment)

- ANTHROPIC_API_KEY: required.
- CLAUDE_MODEL: optional, e.g., claude-3-haiku-20240307 (default), claude-3-5-sonnet-20240620, etc.
- AZURE_BLOB_ENDPOINT: required for Blob operations, e.g., https://mystorage.blob.core.windows.net

Azure authentication via DefaultAzureCredential supports:
- Azure CLI (az login),
- Managed Identity,
- Service Principal (AZURE_TENANT_ID, AZURE_CLIENT_ID, AZURE_CLIENT_SECRET),
- Visual Studio/VS Code sign-in.

## Docker

### Build
```bash
docker build -t claude-mcp:latest -f ClaudeMCP/Dockerfile .
```

### Run
```bash
docker run --rm \
  -e ANTHROPIC_API_KEY=$ANTHROPIC_API_KEY \
  -e CLAUDE_MODEL=${CLAUDE_MODEL:-claude-3-haiku-20240307} \
  -e AZURE_BLOB_ENDPOINT=$AZURE_BLOB_ENDPOINT \
  claude-mcp:latest
```

### docker-compose (optional)
The repo includes docker-compose.yml and a sample MCP gateway configuration.

Note: docker-compose mounts ./config.json by default. Either:
- Copy ClaudeMCP/config.json to project root as config.json, or
- Update the volume path in docker-compose.yml to point to ClaudeMCP/config.json.

Start:
```bash
docker compose up --build
```

## Project Structure
```
.
├─ ClaudeMCP.sln
├─ docker-compose.yml
├─ ClaudeMCP/
│  ├─ ClaudeMCP.csproj
│  ├─ Dockerfile
│  ├─ Program.cs
│  ├─ config.json                 # sample MCP config
│  ├─ ClaudeMCP.http              # local HTTP test file (not needed for stdio)
│  ├─ Clients/
│  │  └─ ClaudeClient.cs
│  └─ McpTools/
│     ├─ ComplianceTools.cs
│     └─ AzureTools.cs
└─ (add other files as needed)
```

## Development
- Build:
  ```bash
  dotnet build
  ```
- Format/lint (if configured):
  ```bash
  dotnet format
  ```
- Tests (if present):
  ```bash
  dotnet test
  ```

## Troubleshooting
- Missing key:
  - "ANTHROPIC_API_KEY not found": set the environment variable.
  - "AZURE_BLOB_ENDPOINT not found": required for blob operations.
- Azure authentication:
  - Ensure az login completed successfully, or service principal variables are set.
  - Verify required permissions on target resources (Blob Storage, Log Analytics).
- Client cannot discover tools:
  - Confirm MCP configuration and working directory.
  - Run with more verbose logs by adjusting logging level if needed.

## Security
- Do not commit secrets. Use environment variables or a secrets manager.
- Scope Azure roles and permissions minimally.
- Rotate keys and credentials regularly.

## License
TODO: Add license information or reference the LICENSE file if present.
