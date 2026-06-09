# TokenShield Web Admin Console

This is the frontend administration panel for **TokenShield AI Gateway**, built as a Next.js TypeScript application using Tailwind CSS.

---

## Folder Structure
- **app/**: App router pages (Dashboard, Providers, Models, Routing Rules, Budgets, Usage Logs, API Keys, Audit Logs, Settings).
- **app/globals.css**: Main styling sheets integrating custom theme parameters (light/dark configuration) using Tailwind CSS v4 variables.
- **public/**: Static public asset resources.

---

## Local Development Setup

### Prerequisite
Ensure you have Node.js (v22.x or higher) installed.

### 1. Install Dependencies
```bash
npm install
```

### 2. Run the Development Server
```bash
npm run dev
```

The web console will be accessible on:
- Web Client: `http://localhost:3000`

### 3. Build & Compile Production Target
To test production compilation:
```bash
npm run build
```
