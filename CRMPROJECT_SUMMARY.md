# Macquires CRM - Project Summary

## Project Overview
**Macquires CRM** is a comprehensive Enterprise Resource Planning (ERP) and Customer Relationship Management (CRM) system built with .NET 9.0. It provides end-to-end business management capabilities covering sales, purchasing, inventory, lead management, financial operations, and resource scheduling.

## Core Concept
The system follows **Clean Architecture** (Onion Architecture) principles with clear separation of concerns across three main layers:
- **Domain Layer**: Core business entities, enums, and domain logic (no dependencies)
- **Application Layer**: Business logic, CQRS pattern with MediatR, validation, and feature managers
- **Infrastructure Layer**: Data access (Entity Framework Core), email services, file management, security
- **Presentation Layer**: ASP.NET Core MVC web application with Razor pages and JavaScript frontend

## Technology Stack
- **Framework**: .NET 9.0
- **Database**: SQL Server (Entity Framework Core 9.0)
- **Architecture Pattern**: Clean Architecture + CQRS
- **Key Libraries**:
  - MediatR (CQRS implementation)
  - FluentValidation (input validation)
  - AutoMapper (object mapping)
  - ASP.NET Core Identity (authentication/authorization)
  - JWT Bearer tokens
  - Serilog (logging)
  - MailKit (email services)

## Core Modules & Features

### 1. Sales Management
- Sales Quotations → Sales Orders → Invoices → Payment Receives
- Sales Returns → Credit Notes
- Sales Teams, Sales Representatives
- Sales Reports & Analytics

### 2. Purchase Management
- Purchase Requisitions → Purchase Orders → Bills → Payment Disburses
- Purchase Returns → Debit Notes
- Vendor Management (Vendors, Groups, Categories, Contacts)
- Purchase Reports

### 3. Inventory Management
- Product Catalog (Products, Groups, Unit Measures)
- Warehouse Management
- Stock Counts, Transfers (In/Out)
- Positive/Negative Adjustments
- Scrapping Management
- Inventory Transactions (full audit trail)
- Stock Reports

### 4. Lead & Campaign Management
- Lead Pipeline (7 stages: Prospecting → Qualification → Need Analysis → Proposal → Negotiation → Decision Making → Closed)
- BANT Scoring (Budget, Authority, Need, Timeline)
- Campaign Management
- Lead Activities & Contacts
- Sales Team Lead Tracking

### 5. Financial Management
- Payment Receives (from customers)
- Payment Disburses (to vendors)
- Expenses (linked to campaigns)
- Budgets
- Credit Notes & Debit Notes
- Financial Reports

### 6. Resource & Booking Management
- Booking Groups & Resources
- Booking Scheduler
- Program Managers (Kanban board)
- Program Resources

### 7. System Administration
- User Management (Internal, Customer, Vendor types)
- Role-Based Access Control (RBAC)
- Company Settings
- Number Sequence Management
- Tax Management
- File Document & Image Management
- Todo Management

### 8. Dashboard & Reporting
- Sales Dashboard (cards, charts, funnels)
- Purchase Dashboard
- Inventory Dashboard
- Lead Pipeline Funnel
- Campaign Status Tracking
- Sales Team Performance

## Architecture Patterns

### CQRS (Command Query Responsibility Segregation)
- **Commands**: Create, Update, Delete operations (e.g., `CreateSalesOrder`, `UpdateLead`)
- **Queries**: Read operations (e.g., `GetSalesOrderList`, `GetLeadSingle`)
- All handled through MediatR pipeline

### Repository Pattern
- Generic repository with Unit of Work pattern
- Entity Framework Core for data access

### Pipeline Behaviors
- **ValidationBehavior**: Automatic FluentValidation execution
- **LoggingBehavior**: Request/response logging

### Domain-Driven Design (DDD)
- Rich domain entities with business logic
- Value objects and enums for domain concepts
- Aggregate roots with proper encapsulation

## Key Design Features

### Base Entity Pattern
All entities inherit from `BaseEntity` providing:
- Sequential GUID generation (time-based, sortable)
- Soft delete (`IsDeleted` flag)
- Audit trail (`CreatedAtUtc`, `CreatedById`, `UpdatedAtUtc`, `UpdatedById`)

### Status Management
All transactional entities use status enums (Draft → Confirmed → Archived/Cancelled) for workflow management.

### Number Sequence System
Automatic document numbering for invoices, orders, quotations, etc.

### File Management
- Image uploads (max 5MB) → `wwwroot/app_data/images`
- Document uploads (max 25MB) → `wwwroot/app_data/docs`

## Frontend Architecture
- **Razor Pages** (.cshtml) for server-side rendering
- **JavaScript** (Vue.js, Axios) for client-side interactivity
- **Syncfusion** components for data grids and charts
- **Bootstrap 5** for UI styling
- **SweetAlert2** for user notifications

## Security Features
- JWT-based authentication
- ASP.NET Core Identity for user management
- Role-based authorization
- Email confirmation required
- Account lockout after failed attempts
- Token refresh mechanism

## Database Design
- SQL Server database
- Entity Framework Core migrations
- Soft delete pattern (no physical deletion)
- Audit fields on all entities
- Foreign key relationships with proper navigation properties

## Demo/Seed Data
Comprehensive seeders available for all entities to populate demo data for testing and development.

## Project Structure
```
Core/
  ├── Domain/          # Entities, Enums, Common interfaces
  └── Application/     # Features, CQRS handlers, Services, Behaviors

Infrastructure/
  └── Infrastructure/   # EF Core, Email, File Management, Security, Seeders

Presentation/
  └── ASPNET/          # Controllers, Razor Pages, Frontend assets
```

## Development Notes
- Default admin: `admin@root.com` / `123456`
- Demo mode enabled by default
- SMTP configuration required for email features
- JWT token expiration: 30 minutes
- Supports multiple user types: Internal, Customer, Vendor
