# User Management API

A RESTful API for managing user records, designed for HR and IT departments to efficiently create, read, update, and delete user information.

## Features

- **CRUD Operations**: Full Create, Read, Update, Delete functionality for user records
- **Email Validation**: Ensures unique email addresses across the system
- **Department Filtering**: Filter users by department
- **Soft Deletion**: Users are marked as inactive rather than permanently deleted
- **Input Validation**: Comprehensive validation using data annotations
- **Error Handling**: Proper HTTP status codes and error messages
- **Logging**: Built-in logging for monitoring and debugging

## API Endpoints

### Users

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/users` | Get all active users |
| GET | `/api/users/{id}` | Get user by ID |
| GET | `/api/users/by-email?email={email}` | Get user by email address |
| GET | `/api/users/by-department/{department}` | Get users by department |
| POST | `/api/users` | Create a new user |
| PUT | `/api/users/{id}` | Update an existing user |
| DELETE | `/api/users/{id}` | Delete a user (soft delete) |
| HEAD | `/api/users/{id}` | Check if user exists |

## User Model

```json
{
  "Id": 1,
  "FirstName": "John",
  "LastName": "Doe",
  "Email": "john.doe@company.com",
  "PhoneNumber": "+1-555-0123",
  "Department": "IT",
  "Position": "Software Developer",
  "DateCreated": "2025-01-15T10:30:00Z",
  "DateModified": "2025-01-15T10:30:00Z",
  "IsActive": true
}
```

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- Visual Studio Code or Visual Studio

### Running the Application

1. Clone the repository
2. Navigate to the project directory
3. Run the application:
   ```bash
   dotnet run
   ```
4. The API will be available at `http://localhost:5268`

### API Documentation

When running in development mode, you can access the OpenAPI documentation at:
- OpenAPI JSON: `http://localhost:5268/openapi/v1.json`

## Sample Data

The application includes sample users for testing:

1. **John Doe** - IT Department, Software Developer
2. **Jane Smith** - HR Department, HR Manager  
3. **Mike Johnson** - IT Department, System Administrator

## Usage Examples

### Create a User

```http
POST /api/users
Content-Type: application/json

{
  "FirstName": "Alice",
  "LastName": "Brown",
  "Email": "alice.brown@company.com",
  "PhoneNumber": "+1-555-0126",
  "Department": "Finance",
  "Position": "Financial Analyst"
}
```

### Update a User

```http
PUT /api/users/1
Content-Type: application/json

{
  "PhoneNumber": "+1-555-9999",
  "Position": "Senior Software Developer"
}
```

### Get Users by Department

```http
GET /api/users/by-department/IT
```

## Validation Rules

- **FirstName**: Required, max 100 characters
- **LastName**: Required, max 100 characters
- **Email**: Required, valid email format, max 255 characters, must be unique
- **PhoneNumber**: Optional, valid phone format, max 20 characters
- **Department**: Required, max 100 characters
- **Position**: Required, max 100 characters

## Error Handling

The API returns appropriate HTTP status codes:

- `200 OK`: Successful GET, PUT requests
- `201 Created`: Successful POST requests
- `204 No Content`: Successful DELETE requests
- `400 Bad Request`: Invalid input data
- `404 Not Found`: Resource not found
- `409 Conflict`: Duplicate email address
- `500 Internal Server Error`: Server errors

## Data Storage

Currently uses in-memory storage for simplicity. In a production environment, this should be replaced with a persistent database like SQL Server, PostgreSQL, or MongoDB.

## Future Enhancements

- Database integration (Entity Framework Core)
- Authentication and authorization
- Pagination for large datasets
- Search functionality
- Audit logging
- API versioning
- Unit and integration tests
