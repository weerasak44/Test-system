# POS System (Minimal Prototype)

This is a prototype implementation of a Point-of-Sale (POS) backend written in C# using ASP.NET Core 8 Minimal APIs.  
It demonstrates the core domain objects and REST endpoints based on the requirements specification.

## Requirements

* .NET SDK 8.0 or newer (`https://dotnet.microsoft.com/download`)

## How to run

```bash
cd PosSystem
# Restore and launch the web server on http://localhost:5000
 dotnet run
```

Swagger UI will be available at:

```
http://localhost:5000/swagger
```

### Seeded credentials

| Username | Password | Role  |
|----------|----------|-------|
| admin    | admin    | Admin |

## Caveats & Next Steps

* **In-memory storage** – Data resets every time the application restarts. Replace the `Repositories` class with Entity Framework Core and a real database (e.g., SQLite, PostgreSQL).
* **Authentication** – `POST /auth/login` currently performs a plaintext check and returns the whole user object. Swap out with JWT tokens and secure password hashing (e.g., BCrypt).
* **Front-end** – Only a backend API is provided. A Blazor or JavaScript front-end can be added to deliver the described UI/UX.
* **Reporting / Export** – The included sales report is basic. Extend to full Excel exports using libraries such as ClosedXML.
* **Receipt printing** – Integrate a PDF/text receipt generator and connect to a printer API as needed.