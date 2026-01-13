# Onyx.Tanuki Specification

## 1. Overview
Tanuki is a developer-flow-first API simulator for mocking RESTful web services. It enables prototyping and testing against a simulated API defined by a JSON configuration.

### 1.1. Non-Goals
Tanuki does not aim to be a full contract-testing framework, schema validator, or OpenAPI server implementation. It focuses on fast, deterministic API simulation for local development and testing.

### 1.2. Core Goals
*   **Zero-Code Simulation**: enable developers to mock complex APIs purely through JSON configuration.
*   **Determinism**: Provide predictable responses based on explicit inputs (query params, headers) for reliable testing.
*   **Developer Flow**: Ensure sub-second startup and low-friction integration into existing workflows (CLI, Docker, Middleware).

### 1.3. Conventions & Versioning
*   **Keywords**: The words "MUST", "MUST NOT", "REQUIRED", "SHALL", "SHALL NOT", "SHOULD", "SHOULD NOT", "RECOMMENDED", "MAY", and "OPTIONAL" in this document are to be interpreted as described in [RFC 2119](https://tools.ietf.org/html/rfc2119).
*   **Compatibility**: 
    *   **Configuration**: The `tanuki.json` schema follows semantic versioning principles. Breaking changes to the schema will require a major version bump of the tool.
    *   **CLI**: Existing flags and commands MUST NOT be removed or renamed within a minor version.

## 2. Architecture

### 2.1. Project Structure
```
src/
├── Tanuki.Cli/         # Console Application (Entry Point)
│   ├── Commands/       # CLI Command definitions (Serve, Init, Validate)
│   └── Program.cs      # Main entry point
└── Tanuki.Runtime/     # Core Library (Logic)
    ├── Configuration/  # Models and Parsers for tanuki.json
    ├── Middleware/     # ASP.NET Core Middleware (Simulation logic)
    ├── OpenApi/        # OpenAPI loading and mapping logic
    └── Simulation/     # Response selection strategies
tests/
└── Tanuki.Tests/       # Unit and Integration Tests
```

### 2.2. Technologies & Dependencies
*   **Language**: C# 12 / .NET 9.0
*   **Web Framework**: ASP.NET Core (Minimal APIs & Middleware)
*   **CLI Framework**: `System.CommandLine`
*   **JSON Handling**: `System.Text.Json`
*   **OpenAPI**: `Microsoft.OpenApi` (Reader/Writer)

### 2.3. Core Concepts
*   **Stateless Simulator**: Tanuki is a stateless request/response simulator. It does not persist data or maintain session state between requests.
*   **Configuration-Driven**: The behavior of the simulator is entirely defined by `tanuki.json`.
*   **Middleware Pipeline**: Request processing is handled by custom ASP.NET Core middleware (`SimulationMiddleware`), which intercepts requests matching configured paths.
*   **Response Selection**: A strategy pattern (`IResponseSelector`) determines which response to return based on a deterministic precedence order (Query Param -> Random -> Default).

### 2.3. Data Flow

```text
Request -> [Kestrel] -> [Logging] -> [SimulationMiddleware] -> [End]
                                            |
                                            v
                                    [Config Service]
                                            |
                                            v
                                    [Response Selector]
```

1.  **Startup**: `TanukiStartupService` loads and validates `tanuki.json`. External values are fetched and cached.
2.  **Request**: An HTTP request enters the pipeline.
3.  **SimulationMiddleware**: Intercepts requests matching configured paths.
4.  **Matching**: Checks if request path/method matches a defined operation.
5.  **Delay**: If configured, an **asynchronous delay** is applied.
6.  **Selection**: `ResponseSelector` determines the response.
7.  **Response**: Content is written to the stream.

## 3. Configuration Schema (`tanuki.json`)

The configuration file follows a hierarchical structure similar to OpenAPI but simplified.

```json
{
  "paths": {
    "/resource/path": {
      "method": {
        "summary": "...",
        "min-delay": 0,
        "max-delay": 0,
        "responses": {
          "200": {
            "content": {
              "application/json": {
                "examples": {
                  "example-name": {
                    "value": "..."
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

### 3.1. Fields
*   **Paths**: Key is the URL path.
    *   **Note**: Path matching is currently literal (exact string match). Wildcards or path parameters (e.g., `/users/{id}`) are not yet supported.
*   **Method**: HTTP method (get, post, put, delete, etc.).
    *   `min-delay` / `max-delay`: Latency simulation in milliseconds.
*   **Responses**: Key is the HTTP status code.
*   **Content**: Key is the media type (e.g., `application/json`).
*   **Examples**: Key is a unique name for the example.
    *   `value`: Inline string/JSON content.
    *   `externalValue`: URL to fetch content from.

## 4. Features & Capabilities

### 4.1. Configuration Sources
*   **Native Configuration (`tanuki.json`)**:
    *   Hierarchical JSON structure.
    *   Supports defining paths, methods, and responses.
*   **OpenAPI Specification**:
    *   **Versions Supported**: OpenAPI 3.0 and 3.1.
    *   **Formats**: JSON and YAML (`.json`, `.yaml`, `.yml`).
    *   **Integration**: Automatically maps OpenAPI `paths`, `operations`, and `examples` to the internal Tanuki simulation model.
    *   **Resolution**: Resolves external file references relative to the main document.

### 4.2. Response Simulation
*   **Static Responses**: Returns pre-defined data from configuration.
    ```json
    "200": { "content": { "application/json": { "examples": { "simple": { "value": "{\"msg\":\"hi\"}" } } } } }
    ```
*   **External Data**: Fetches and caches data from external URLs (60-minute default cache, currently not configurable; restart to clear).
    *   **Conflict Resolution**: `value` and `externalValue` are mutually exclusive. Invalid configurations fail validation.
    *   **Verification**: Ensure unit tests cover fetching failures and cache hits.
    ```json
    "examples": { "dynamic": { "externalValue": "https://api.example.com/data.json" } }
    ```
*   **Delay Simulation**:
    *   Configurable `min-delay` and `max-delay` in milliseconds per operation.
    *   Delays are applied before response selection.
    *   **Verification**: Use `TimeProvider` in tests to verify delays without brittle `Thread.Sleep`.
    ```json
    "get": { "min-delay": 100, "max-delay": 500, "responses": { ... } }
    ```

### 4.3. Control Logic
*   **Query Parameters**:
    *   `?status=<code>`: Force a specific HTTP status code (e.g., `?status=400`).
    *   `?example=<name>`: Force a specific named example (e.g., `?example=user_1`).
    *   `?random`: Force a random example selection.
*   **Content Negotiation**: Respects the `Accept` header to select the matching media type. Defaults to the first configured content type if no match is found.

### 4.4. Developer Experience
*   **Logging**:
    *   Standard logging: Warnings and Errors.
    *   Verbose logging (`--verbose`): Detailed request/response payloads, headers, and timings.
    *   **Security**: Automatically redacts sensitive headers (Authorization, Cookie, X-Api-Key) in logs.
*   **Health Checks**:
    *   Endpoint at `/health` verifies simulator status and loaded configuration stats. Returns `application/json`.
    *   **Response Shape**:
        ```json
        {
          "status": "Healthy",
          "totalDuration": "00:00:00.0012345",
          "entries": {
            "tanuki": {
              "data": {
                "paths": 12,
                "operations": 34
              },
              "duration": "00:00:00.0001234",
              "status": "Healthy",
              "tags": ["tanuki", "configuration"]
            }
          }
        }
        ```

### 4.5. Operational Lifecycle
*   **Startup**: Validates configuration. Fails fast on errors. Pre-fetches external values.
*   **Runtime**: Handles requests statelessly. Logs activity.
*   **Reloading**: Configuration changes currently require a server restart. Hot-reload is not supported.
*   **Shutdown**: Graceful shutdown releases port and flushes logs.

## 5. System Limitations & Constraints

### 5.1. OpenAPI Constraints
*   **File Size Limit**: OpenAPI specification files must be under **2 MB**.
    *   *Reason*: Limits memory usage during parsing and prevents denial-of-service from massive documents.
*   **Validation**: The system validates spec version and file size before loading.
    *   **Mapping**: Only `paths` with `examples` defined in `responses` are currently simulated. Examples defined at the Schema/Component level are not automatically resolved unless referenced directly in the Response. Schema generation from models is not yet supported.
        *   *Pro-Tip*: Ensure your OpenAPI tool (e.g., Swashbuckle) puts examples in the `Response` object, not just the schema components.

### 5.2. Runtime Limits
*   **Body Size**: Default ASP.NET Core limits apply to request bodies.
*   **Cache**: External value cache is in-memory only; it does not persist across restarts.
*   **Concurrency**: Thread-safe, relying on standard ASP.NET Core concurrency handling.

## 6. CLI Reference

The CLI is built using `System.CommandLine`.

*   **`tanuki init`**: Generates a scaffold `tanuki.json` file.
*   **`tanuki validate`**: Checks the validity of the configuration file without starting the server.
*   **`tanuki serve`**: Starts the HTTP server.
    *   `--port, -p`: Custom port (default: 5000).
    *   `--host, -h`: Custom host (default: localhost).
    *   `--config, -c`: Path to `tanuki.json` config file (exclusive with `--openapi`).
    *   `--openapi, -o`: Path to OpenAPI spec file (exclusive with `--config`).
    *   `--verbose, -v`: Detailed logging.
    *   **Exit Codes**:
        *   `0`: Success.
        *   `1`: General failure (invalid arguments, port in use).

## 7. Error Behavior

| Scenario | Behavior | HTTP Status | Log Level |
| :--- | :--- | :--- | :--- |
| **Invalid Config** | Simulator fails to start. | N/A | Critical |
| **Path Not Found** | No matching path in configuration. | 404 Not Found | Warning |
| **Response Not Found** | Path matched, but requested response (status/example) not defined. | 404 Not Found | Warning |
| **External Fetch Fail** | `externalValue` URL could not be retrieved. | 500 Internal Error | Error |
| **Internal Error** | Unexpected exception in middleware. | 500 Internal Error | Error |
