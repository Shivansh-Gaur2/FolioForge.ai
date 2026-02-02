# FolioForge 🚀

> **The Intelligent, Event-Driven Portfolio Platform.**
> *Turn a static PDF resume into a dynamic, deployable personal website in seconds.*

---

## 📖 Overview

FolioForge is not just a template engine; it is a **multi-tenant SaaS platform** designed to host millions of personal portfolios. It solves the problem of "Resume-to-Web" conversion using an **Event-Driven Architecture** to handle heavy AI processing asynchronously while ensuring zero-latency reads for public visitors.

### The Core Problem

Static site generators are manual. Existing resume builders are rigid. FolioForge uses a **Generic Widget System** (inspired by Notion blocks) to decouple the data from the presentation, allowing for infinite flexibility in how a portfolio is rendered.

---

## 🏗 Architecture & Design

We follow **Clean Architecture** principles and a **CQRS-inspired** flow to separate "Writing" (Ingestion) from "Reading" (Rendering).

### System High-Level

1. **Ingestion Engine (Write):** Handles PDF uploads, sanitization, and LLM orchestration.
2. **Rendering Engine (Read):** A highly optimized React SPA that renders generic widgets based on JSON configurations.
3. **Routing Layer:** Handles wildcard subdomains (`*.platform.com`) dynamically via Client-Side Routing and Nginx.

### Key Engineering Decisions

* **Generic Widget Protocol:** We do not store "Resumes." We store `Widgets` (Timeline, Grid, Markdown). This adheres to the **Open/Closed Principle**—adding a new "Spotify Widget" requires zero database migrations.
* **Async Processing:** PDF parsing is heavy. We use **RabbitMQ** to offload processing to background workers, ensuring the API remains responsive (`202 Accepted` pattern).
* **Hybrid Storage:** We use **PostgreSQL** with `JSONB` columns. This gives us ACID compliance for user data but NoSQL flexibility for the highly polymorphic widget content.

---

## 🛠 Tech Stack

### Frontend (Client)

* **Framework:** React 19 (Vite)
* **Styling:** Tailwind CSS + clsx
* **Components:** Radix UI (Headless accessibility)
* **State:** Zustand (Global), TanStack Query (Server)
* **Motion:** Framer Motion (Layout animations)

### Backend (API & Worker)

* **Framework:** .NET 8 (Core Web API)
* **Pattern:** Clean Architecture (Domain, Application, Infrastructure, API)
* **Database:** PostgreSQL 16
* **Messaging:** RabbitMQ (or Azure Service Bus)
* **AI/LLM:** OpenAI API (GPT-4o mini) with Structured Outputs (JSON Mode)

### DevOps & Infra

* **Containerization:** Docker & Docker Compose
* **Reverse Proxy:** Nginx (Local) / Cloudflare (Prod)
* **Observability:** OpenTelemetry (Planned)

---

## ⚡ Features

* **📄 AI-Powered Parsing:** Converts unstructured PDF text into structured JSON widgets.
* **🌍 Multi-Tenancy:** Automated subdomain routing (`shivansh.localhost` -> User Profile).
* **🧩 Drag-and-Drop Builder:** Reorder sections easily with `dnd-kit`.
* **🎨 Theming Engine:** JSON-based design tokens allow users to switch themes instantly.
* **🛡️ Robust Validation:** Shared Zod schemas between Frontend and Backend.

---

## 🚀 Getting Started

### Prerequisites

* Docker Desktop
* .NET 8 SDK
* Node.js 20+
* PostgreSQL (or run via Docker)

### 1. Clone the Repo

```bash
git clone https://github.com/yourusername/folioforge.git
cd folioforge

```

### 2. Infrastructure Setup (Docker)

Start the database and message queue.

```bash
docker-compose up -d postgres rabbitmq

```

### 3. Backend Setup

Configure your secrets (OpenAI Key, DB Connection).

```bash
cd apps/api
dotnet restore
dotnet ef database update # Apply migrations
dotnet run

```

### 4. Frontend Setup

In a new terminal:

```bash
cd apps/web
npm install
npm run dev

```

### 5. Domain Hosts (Localhost Magic)

To test subdomains locally, add this to your `hosts` file (`/etc/hosts` or `C:\Windows\System32\drivers\etc\hosts`):

```text
127.0.0.1   app.localhost
127.0.0.1   shivansh.localhost
127.0.0.1   demo.localhost

```

---

## 📂 Project Structure

```text
/
├── apps/
│   ├── api/             # .NET 8 Web API (The Orchestrator)
│   ├── worker/          # .NET Background Service (The Heavy Lifter)
│   └── web/             # React SPA (The Dashboard & Renderer)
├── packages/
│   └── shared-types/    # TypeScript types synced with C# DTOs
├── docker/              # Infrastructure config
└── docs/                # Architecture Decision Records (ADRs)

```

---

## 🤝 Contribution Guidelines

1. **Branching:** Use `feature/` or `fix/` prefixes.
2. **Commits:** Follow Conventional Commits (e.g., `feat: add spotify widget`).
3. **Code Style:**
* **C#:** Follows standard .NET conventions (`.editorconfig` included).
* **React:** ESLint + Prettier enabled.



---

## 📜 License

Distributed under the MIT License. See `LICENSE` for more information.
