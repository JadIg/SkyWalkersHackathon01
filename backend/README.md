# Tech For Good - Form Builder Backend

## 🚀 Project Overview
A high-performance, multi-tenant form builder API designed for organizations (National Bank, Zain, CBI) to create, manage, and analyze forms. Built with the latest **C# .NET 9 Minimal APIs** for maximum speed and low latency.

## 🛠️ Technology Stack
* **Framework:** .NET 9 (Minimal API)
* **Database:** SQLite (Portable & Fast)
* **ORM:** Entity Framework Core
* **Documentation:** Swagger/OpenAPI

## 🏗️ Architectural Decisions

### 1. Multi-Tenancy Strategy
We implemented **Row-Level Isolation**.
Instead of complex separate databases, every `Form` and `User` entity is tagged with a `TenantId`. This ensures data isolation (e.g., Zain users cannot see National Bank forms) while keeping the architecture simple and cloud-ready.

### 2. Versioning Strategy (Strategy A)
To satisfy the "Version Locking" requirement, we implemented **Immutable Forms**:
* When a form with existing submissions is edited, the system **locks** the original version (v1).
* It creates a **new version (v2)** automatically.
* This guarantees that historical data never breaks, even if questions are changed or deleted.

## ⚙️ Setup & Installation
1.  **Prerequisites:** .NET 9 SDK
2.  **Clone the Repo:** `git clone [repo_link]`
3.  **Run the Backend:**
    ```bash
    cd backend
    dotnet run
    ```
4.  **Access API:** Open `http://localhost:5000/swagger`

## 🧪 Testing
The system includes an **Auto-Seeder** that generates 20 mock submissions on startup, allowing for immediate testing of the Analytics Dashboard.