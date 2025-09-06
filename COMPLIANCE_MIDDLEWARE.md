# TechHive Solutions Corporate Compliance Middleware

## Overview
This document outlines the implementation of middleware components required to comply with TechHive Solutions' corporate policies for the User Management API.

## Corporate Policy Requirements ✅

### 1. Audit Logging Middleware ✅
**Requirement**: Log all incoming requests and outgoing responses for auditing purposes.

**Implementation**: `AuditLoggingMiddleware`
- **Location**: `Middleware/AuditLoggingMiddleware.cs`
- **Features**:
  - Comprehensive request/response logging
  - Configurable logging levels and excluded paths
  - Request/response body logging with size limits
  - Performance metrics (response time tracking)
  - Client IP address tracking
  - User identification from JWT claims
  - Sensitive data protection (header redaction)

**Configuration**:
```json
{
  "AuditSettings": {
    "LogRequestBody": true,
    "LogResponseBody": false,
    "LogHeaders": true,
    "SensitiveHeaders": ["Authorization", "Cookie", "Set-Cookie", "X-API-Key"],
    "ExcludedPaths": ["/health", "/metrics", "/swagger"],
    "MaxBodyLogSize": 4096
  }
}
```

### 2. Standardized Error Handling Middleware ✅
**Requirement**: Enforce standardized error handling across all endpoints.

**Implementation**: `ErrorHandlingMiddleware`
- **Location**: `Middleware/ErrorHandlingMiddleware.cs`
- **Features**:
  - Global exception handling
  - Consistent error response format (RFC 7807 Problem Details)
  - Environment-specific error details
  - Custom exception types for different scenarios
  - Automatic error categorization and HTTP status mapping

**Error Response Format**:
```json
{
  "type": "validation-error",
  "title": "Validation Failed",
  "status": 400,
  "detail": "One or more validation errors occurred",
  "instance": "/api/users",
  "traceId": "0HN7SKAEC42QJ:00000001",
  "timestamp": "2025-09-06T10:30:00Z",
  "errors": {
    "firstName": ["First name is required"]
  }
}
```

### 3. Token-Based Authentication Middleware ✅
**Requirement**: Secure API endpoints using token-based authentication.

**Implementation**: `JwtAuthenticationMiddleware` + `JwtTokenService`
- **Location**: `Middleware/JwtAuthenticationMiddleware.cs`
- **Features**:
  - JWT token validation
  - Configurable public endpoints
  - Token expiration checking
  - Claims-based user identification
  - Automatic authentication failure handling

**JWT Configuration**:
```json
{
  "JwtSettings": {
    "SecretKey": "TechHive-UserManagement-SuperSecretKey-2025-MinLength32Characters!",
    "Issuer": "TechHive.UserManagementAPI",
    "Audience": "TechHive.UserManagementAPI.Users",
    "ExpirationMinutes": 60,
    "ValidateIssuer": true,
    "ValidateAudience": true,
    "ValidateLifetime": true,
    "ValidateIssuerSigningKey": true,
    "ClockSkewSeconds": 30
  }
}
```

## Middleware Pipeline Order
The middleware pipeline is configured in the following order for optimal security and functionality:

1. **Error Handling** - Catches all exceptions globally
2. **CORS** - Cross-origin resource sharing (development only)
3. **HTTPS Redirection** - Enforces secure connections
4. **JWT Authentication** - Validates user tokens
5. **Audit Logging** - Records all requests/responses
6. **Routing** - Routes requests to controllers
7. **Controllers** - Application logic

## Authentication Endpoints

### POST /api/auth/login
Authenticates users and returns JWT tokens.

**Request**:
```json
{
  "email": "admin@techhive.com",
  "password": "Admin123!"
}
```

**Response**:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "tokenType": "Bearer",
  "expiresAt": "2025-09-06T11:30:00Z",
  "user": {
    "id": "1",
    "email": "admin@techhive.com",
    "firstName": "System",
    "lastName": "Administrator",
    "roles": ["Admin", "User"]
  }
}
```

### GET /api/auth/validate
Validates the current JWT token and returns user information.

### GET /api/auth/test-credentials
Returns available test credentials (development only).

## Test Credentials
For testing purposes, the following credentials are available:

| Email | Password | Roles |
|-------|----------|-------|
| admin@techhive.com | Admin123! | Admin, User |
| user@techhive.com | User123! | User |
| john.doe@company.com | Password123! | User |

## Security Features

### JWT Token Security
- **Algorithm**: HMAC SHA-256
- **Expiration**: 60 minutes (configurable)
- **Claims**: User ID, email, name, roles, timestamps
- **Validation**: Issuer, audience, lifetime, signature

### Audit Security
- **Sensitive Data Protection**: Headers like Authorization are redacted
- **Size Limits**: Request/response bodies limited to 4KB in logs
- **IP Tracking**: Client IP addresses captured for security monitoring
- **Request Correlation**: Unique request IDs for tracing

### Error Handling Security
- **Information Disclosure**: Detailed errors only in development
- **Stack Trace Protection**: Stack traces hidden in production
- **Consistent Responses**: Uniform error format prevents information leakage

## Usage Examples

### 1. Authentication Flow
```bash
# 1. Login to get token
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@techhive.com",
    "password": "Admin123!"
  }'

# 2. Use token for protected endpoints
curl -X GET http://localhost:5000/api/users \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### 2. API Usage with Authentication
```bash
# Get users (protected endpoint)
curl -X GET http://localhost:5000/api/users \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."

# Create user (protected endpoint)
curl -X POST http://localhost:5000/api/users \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "Jane",
    "lastName": "Smith",
    "email": "jane.smith@company.com",
    "department": "HR",
    "position": "Manager"
  }'
```

## Compliance Benefits

### Audit Compliance
- **Complete Request Tracking**: All API calls logged with full context
- **Performance Monitoring**: Response times tracked for SLA compliance
- **User Activity Tracking**: User actions linked to authenticated identities
- **Security Event Logging**: Failed authentication attempts logged

### Error Handling Compliance
- **Consistent Error Responses**: Standardized error format across all endpoints
- **Security-First Error Handling**: Prevents information disclosure
- **Debugging Support**: Detailed errors in development, secure in production

### Security Compliance
- **Token-Based Authentication**: Industry-standard JWT implementation
- **Configurable Security**: Flexible authentication and validation settings
- **Public Endpoint Control**: Granular control over protected resources

## Monitoring and Logging

### Log Levels
- **Information**: Successful requests, authentication events
- **Warning**: Failed authentication, validation errors
- **Error**: Exceptions, system errors
- **Debug**: Detailed middleware operations (development)

### Audit Log Structure
```json
{
  "requestId": "550e8400-e29b-41d4-a716-446655440000",
  "timestamp": "2025-09-06T10:30:00Z",
  "method": "POST",
  "path": "/api/users",
  "queryString": "?page=1&pageSize=10",
  "userAgent": "curl/7.68.0",
  "clientIpAddress": "192.168.1.100",
  "userId": "1",
  "userEmail": "admin@techhive.com",
  "statusCode": 201,
  "responseTimeMs": 45,
  "responseSizeBytes": 256,
  "headers": {
    "Accept": "application/json",
    "Authorization": "[REDACTED]"
  }
}
```

## Production Deployment Notes

1. **Environment Variables**: Use secure configuration for JWT secrets
2. **Log Storage**: Configure structured logging for audit retention
3. **Performance**: Monitor middleware overhead in production
4. **Security Updates**: Regularly update JWT libraries for security patches
5. **Error Monitoring**: Implement error tracking and alerting systems

This implementation fully satisfies TechHive Solutions' corporate compliance requirements while providing a robust, secure, and auditable API platform.
