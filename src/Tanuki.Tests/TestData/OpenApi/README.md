# OpenAPI Test Data

This directory contains official OpenAPI specification sample files used for testing.

## Files

- **petstore.yaml** - Official Swagger Petstore OpenAPI 3.0.4 specification (YAML format)
  - Source: https://github.com/swagger-api/swagger-petstore/blob/master/src/main/resources/openapi.yaml
  
- **openapi.yaml** - Official Swagger Petstore OpenAPI 3.0.4 specification (YAML format)
  - Source: https://petstore3.swagger.io/api/v3/openapi.yaml
  
- **openapi.json** - Official Swagger Petstore OpenAPI 3.0.4 specification (JSON format)
  - Source: https://petstore3.swagger.io/api/v3/openapi.json

## Usage in Tests

These files are used by the OpenAPI parser tests to ensure real-world compatibility.
Access them via the `TestDataHelper` class:

```csharp
var filePath = TestDataHelper.PetstoreYaml;
var filePath = TestDataHelper.PetstoreJson;
var filePath = TestDataHelper.PetstoreSwaggerYaml;
```

## Updating Test Data

To update these files, run:

```powershell
Invoke-WebRequest -Uri "https://petstore3.swagger.io/api/v3/openapi.yaml" -OutFile "openapi.yaml"
Invoke-WebRequest -Uri "https://petstore3.swagger.io/api/v3/openapi.json" -OutFile "openapi.json"
```
