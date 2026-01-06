# Tanuki

Tanuki is a tool for mocking up RESTful webservices. Meant to be used for prototyping and developing applications while the REST APIs are not available or if you want to develop in isolation.

## Requirements

- .NET 9.0 SDK or later
- Windows, Linux, or macOS

## Getting Started

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

3. Run the application:
   ```bash
   cd src/Tanuki
   dotnet run
   ```

The server will start on `http://localhost:5000` (or the port configured in `appsettings.json`).

### Configuration

Create a `tanuki.json` file in the application directory (or configure the path in `appsettings.json`):

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

You can configure Tanuki in `appsettings.json`:

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

## Health Checks

Tanuki includes a health check endpoint at `/health` that validates the configuration and reports the number of paths, operations, and responses configured.

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

See `src/Tanuki/tanuki.json` for a complete example configuration.

## Logging

Tanuki uses structured logging with the following log levels:
- **Information**: Successful requests with performance metrics
- **Warning**: Configuration issues, missing paths/operations
- **Debug**: Detailed request processing, external value fetching
- **Error**: Invalid configurations, unexpected errors

## Performance

- External values are cached in memory for 60 minutes
- Request processing times are logged for monitoring
- Delay simulation helps test timeout scenarios

## Development

### Building

```bash
dotnet build
```

### Testing

```bash
dotnet test
```

### Running Tests

```bash
cd src/Tanuki.Tests
dotnet test
```

## Contribution

Tanuki is meant to be a community project; all contributions are welcome. Whether as a feature request, code, or any means you want. You can open an issue for any contribution you want and we can discuss and agree on the best way to move it forward.

## License

[Add your license information here]
