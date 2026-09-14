# 🏗️ Architecture Documentation

## Overview

The **DotNetCoreSocialApi** is built following the principles of **Clean Architecture** (also known as Onion Architecture). This design ensures that the core business logic is independent of external frameworks, databases, and UI concerns.

## 📁 Project Structure

The solution is divided into four main layers:

### 1. Social.API (Presentation Layer)
- **Role**: Entry point for HTTP requests.
- **Components**:
  - `Controllers`: Handle incoming HTTP requests and delegate work via `MediatR`.
  - `Middlewares`: Custom logic for authentication, rate limiting, and token blacklisting.
  - `Configuration`: Environment-specific settings and DI registration.
- **File Path**: `d:\Projects\DotNetCore-Projects\DotNetCoreSocialApi\Social`

### 2. Social.Application (Application Layer)
- **Role**: Orchestrates business workflows.
- **Pattern**: **CQRS (Command Query Responsibility Segregation)** using `MediatR`.
- **Components**:
  - `Commands`: Handlers for write operations (Create, Update, Delete).
  - `Queries`: Handlers for read operations (Get, Search).
  - `DTOs & Mappers`: Objects for data transfer and `AutoMapper` profiles.
- **File Path**: `d:\Projects\DotNetCore-Projects\DotNetCoreSocialApi\Social.Application`

### 3. Social.Core (Domain Layer)
- **Role**: Contains the heart of the system.
- **Components**:
  - `Entities`: Pure C# classes representing domain concepts (Post, User, Comment).
  - `Interfaces`: Abstractions for repositories and services.
  - `Common`: Shared constants and enums.
- **File Path**: `d:\Projects\DotNetCore-Projects\DotNetCoreSocialApi\Social.Core`

### 4. Social.Infrastructure (Persistence Layer)
- **Role**: Handles data access and external integrations.
- **Components**:
  - `Data`: `DbContext` implementation for EF Core.
  - `Repositories`: Concrete implementations of data access logic.
  - `Migrations`: Database schema versioning.
  - `Token`: JWT generation and validation services.
- **File Path**: `d:\Projects\DotNetCore-Projects\DotNetCoreSocialApi\Social.Infrastructure`

## 🛠️ Tech Stack
- **Framework**: .NET 9 (ASP.NET Core)
- **ORM**: Entity Framework Core
- **Database**: MySQL
- **Caching**: Redis
- **Patterns**: CQRS, Repository, Unit of Work, Dependency Injection.
- **Security**: JWT Authentication, Identity Framework.
