# Tanuki

Tanuki is a developer-flow-first API simulator for mocking RESTful web services. Perfect for prototyping and developing applications while REST APIs are not available or when you want to develop in isolation.

## Features

- 🚀 **Easy CLI** - Simple command-line interface for quick setup and testing
- 📝 **JSON Configuration** - Define API endpoints using simple JSON configuration
- ⚡ **Delay Simulation** - Simulate network latency with configurable delays
- 🎲 **Response Selection** - Choose responses by status code, example name, or random selection
- 🔄 **Content-Type Negotiation** - Automatic content-type matching based on Accept headers
- 🌐 **External Value Fetching** - Reference external resources for example values with caching
- 📊 **Request/Response Logging** - Detailed logging of requests and responses for debugging
- ✅ **Health Checks** - Built-in health check endpoint for monitoring

## Requirements

- .NET 9.0 SDK or later
- Windows, Linux, or macOS

## Quick Start

### Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/onyx-ws/tanuki.git
   cd tanuki
   ```

2. Build the project:
   ```bash
   dotnet build
   ```

### Using the CLI

The easiest way to use Tanuki is through the command-line interface:

#### 1. Initialize a new project

Create a sample `tanuki.json` configuration file:

```bash
dotnet run --project src/Tanuki.Cli/Tanuki.Cli.csproj -- init
```

This creates a `tanuki.json` file in the current directory with example endpoints.

#### 2. Start the server

Start the Tanuki API simulator:

```bash
dotnet run --project src/Tanuki.Cli/Tanuki.Cli.csproj -- serve
```

The server will start on `http://localhost:5000` by default.

**Options:**
- `--port, -p` - Port to listen on (default: 5000)
- `--host, -h` - Host to bind to (default: localhost)
- `--config, -c` - Path to tanuki.json configuration file (default: ./tanuki.json)
- `--verbose, -v` - Enable verbose logging with detailed request/response information

**Examples:**
```bash
# Start on a custom port
dotnet run --project src/Tanuki.Cli/Tanuki.Cli.csproj -- serve --port 8080

# Start with verbose logging
dotnet run --project src/Tanuki.Cli/Tanuki.Cli.csproj -- serve --verbose

# Use a custom configuration file
dotnet run --project src/Tanuki.Cli/Tanuki.Cli.csproj -- serve --config my-api.json
```

#### 3. Validate configuration

Validate your `tanuki.json` configuration file:

```bash
dotnet run --project src/Tanuki.Cli/Tanuki.Cli.csproj -- validate
```

Or validate a specific file:

```bash
dotnet run --project src/Tanuki.Cli/Tanuki.Cli.csproj -- validate --config my-api.json
```

## Configuration

### Creating a Configuration File

Create a `tanuki.json` file (or use `tanuki init` to generate a sample):

```json
{
  "paths": {
    "/api/v0.1/ping": {
      "get": {
        "summary": "Execute test call",
        "operationId": "ping",
        "min-delay": 100,
        "max-delay": 300,
        "responses": {
          "200": {
            "description": "Ping reply",
            "content": {
              "application/json": {
                "examples": {
                  "reply-1": {
                    "summary": "Hello World Example",
                    "value": "{ \"message\": \"Hello World!\" }"
                  }
                }
              }
            }
          }
        }
      }
    }
  }
}
```

### Configuration Options

When using the CLI, the configuration file path can be specified with the `--config` option. When using the library directly, configure Tanuki in `appsettings.json`:

```json
{
  "Tanuki": {
    "ConfigurationFilePath": "./tanuki.json"
  }
}
```

## Features

### Delay Simulation

Simulate network latency by configuring `min-delay` and `max-delay` (in milliseconds) on operations:

```json
{
  "get": {
    "min-delay": 100,
    "max-delay": 300,
    "responses": { ... }
  }
}
```

### Response Selection

#### Status Code Selection
Select a specific response by status code using query parameters:
```
GET /api/v0.1/ping?status=404
```

#### Example Selection
Select a specific example by name:
```
GET /api/v0.1/ping?example=reply-1
```

#### Random Example Selection
Get a random example:
```
GET /api/v0.1/ping?random
```

### Content-Type Negotiation

Tanuki automatically matches the `Accept` header to available content types. If no match is found, it returns the first available content type.

### External Value Fetching

Reference external resources for example values:

```json
{
  "examples": {
    "external-example": {
      "externalValue": "https://example.com/api/data.json"
    }
  }
}
```

External values are automatically cached for 60 minutes to improve performance.

### Request/Response Logging

When using the CLI with the `--verbose` flag, Tanuki logs detailed information about each request and response:

- Request method, path, query string, and headers
- Request body (if present)
- Response status code, headers, and body
- Request duration
- Sensitive data (Authorization, Cookie, API keys) is automatically redacted

Example output:
```
═══════════════════════════════════════════════════════════
REQUEST: GET /api/example
From: 127.0.0.1
Headers:
  Host: localhost:5000
  User-Agent: Mozilla/5.0...
───────────────────────────────────────────────────────────
RESPONSE: ✓ 200 application/json | Duration: 15ms
Headers:
  Content-Type: application/json
Body:
{"message": "Hello from Tanuki!"}
═══════════════════════════════════════════════════════════
```

## Health Checks

Tanuki includes a health check endpoint at `/health` that validates the configuration and reports the number of paths, operations, and responses configured.

```bash
curl http://localhost:5000/health
```

## CLI Commands Reference

### `serve`

Start the Tanuki API simulator server.

**Usage:**
```bash
tanuki serve [options]
```

**Options:**
- `-p, --port <port>` - Port to listen on [default: 5000]
- `-h, --host <host>` - Host to bind to [default: localhost]
- `-c, --config <config>` - Path to tanuki.json configuration file [default: ./tanuki.json]
- `-v, --verbose` - Enable verbose logging (shows request/response details) [default: False]

### `validate`

Validate Tanuki configuration files.

**Usage:**
```bash
tanuki validate [options]
```

**Options:**
- `-c, --config <config>` - Path to tanuki.json configuration file to validate [default: ./tanuki.json]

### `init`

Initialize a new Tanuki project by creating a sample `tanuki.json` configuration file.

**Usage:**
```bash
tanuki init [options]
```

**Options:**
- `-o, --output <output>` - Output directory for the generated configuration [default: .]

## API Reference

### Configuration Schema

#### Paths
- **paths** (object, required): Map of path URIs to path definitions
  - Key: Path URI (e.g., `/api/v0.1/ping`)
  - Value: Path definition object

#### Path Definition
- **{method}** (object): HTTP method (get, post, put, delete, patch, options, head, trace)
  - **summary** (string, optional): Short description
  - **description** (string, optional): Detailed description
  - **operationId** (string, optional): Unique operation identifier
  - **tags** (array, optional): Tags for grouping operations
  - **min-delay** (number, optional): Minimum delay in milliseconds
  - **max-delay** (number, optional): Maximum delay in milliseconds
  - **responses** (object, required): Map of status codes to response definitions

#### Response Definition
- **{statusCode}** (object): HTTP status code (100-599)
  - **description** (string, optional): Response description
  - **content** (object, required): Map of content types to content definitions

#### Content Definition
- **{mediaType}** (object): Media type (e.g., `application/json`)
  - **examples** (object, required): Map of example names to example definitions

#### Example Definition
- **{exampleName}** (object): Example name
  - **summary** (string, optional): Example summary
  - **description** (string, optional): Example description
  - **value** (string, optional): Inline example value (mutually exclusive with externalValue)
  - **externalValue** (string, optional): URL to external example value (mutually exclusive with value)

## Examples

See `tanuki.json` in the root directory or `src/Tanuki/tanuki.json` for complete example configurations.

## Using as a Library

Tanuki can also be used as a library in your .NET applications. Add the `Onyx.Tanuki` NuGet package to your project and configure it:

```csharp
using Onyx.Tanuki;

var builder = WebApplication.CreateBuilder(args);

// Configure Tanuki services
builder.Services.AddTanuki(builder.Configuration);

var app = builder.Build();

// Add health checks
app.MapHealthChecks("/health");

// Add simulation middleware
app.UseSimulator();

app.Run();
```

## Logging

Tanuki uses structured logging with the following log levels:
- **Information**: Successful requests with performance metrics
- **Warning**: Configuration issues, missing paths/operations
- **Debug**: Detailed request processing, external value fetching
- **Error**: Invalid configurations, unexpected errors

When using the CLI with `--verbose`, additional request/response details are logged to the console.

## Performance

- External values are cached in memory for 60 minutes
- Request processing times are logged for monitoring
- Delay simulation helps test timeout scenarios
- Request body size is limited to prevent DoS attacks (1MB default)

## Development

### Building

```bash
dotnet build
```

### Testing

Run all tests:
```bash
dotnet test
```

Run CLI regression tests:
```bash
.\test-cli-regression.ps1
```

### Project Structure

- `src/Tanuki.Cli/` - Command-line interface application
- `src/Tanuki/` - Main library and web application
- `src/Tanuki.Runtime/` - Runtime components (shared with CLI)
- `src/Tanuki.Tests/` - Unit and integration tests

## Contribution

Tanuki is meant to be a community project; all contributions are welcome. Whether as a feature request, code, or any means you want. You can open an issue for any contribution you want and we can discuss and agree on the best way to move it forward.

## License

[Add your license information here]
