import type { Metadata } from "next";
import Link from "next/link";
import "./globals.css";
import Providers from "@/components/providers";
import {
  LayoutDashboard,
  Server,
  Cpu,
  Route,
  Coins,
  History,
  Key,
  ShieldCheck,
  Settings,
  Layers,
  UserCheck
} from "lucide-react";

export const metadata: Metadata = {
  title: "TokenShield AI Gateway",
  description: "Enterprise LLM FinOps, Routing, and Governance",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  // Setup navigation items list
  const navItems = [
    { name: "Dashboard", href: "/dashboard", icon: LayoutDashboard },
    { name: "Providers", href: "/providers", icon: Server },
    { name: "Models", href: "/models", icon: Cpu },
    { name: "Routing Rules", href: "/routing-rules", icon: Route },
    { name: "Budgets", href: "/budgets", icon: Coins },
    { name: "Usage Logs", href: "/usage-logs", icon: History },
    { name: "API Keys", href: "/api-keys", icon: Key },
    { name: "Audit Logs", href: "/audit-logs", icon: ShieldCheck },
    { name: "Settings", href: "/settings", icon: Settings },
  ];

  return (
    <html lang="en" className="h-full dark">
      <body className="h-full bg-background text-foreground antialiased flex">
        {/* Navigation Sidebar */}
        <aside className="w-64 bg-card border-r border-border flex flex-col shrink-0">
          {/* Logo Header */}
          <div className="h-16 px-6 border-b border-border flex items-center gap-2">
            <Layers className="h-6 w-6 text-primary" />
            <span className="font-bold text-lg tracking-wider text-foreground">
              TokenShield
            </span>
            <span className="bg-primary/20 text-primary text-[10px] font-semibold px-1.5 py-0.5 rounded uppercase">
              MVP
            </span>
          </div>

          {/* Nav Links */}
          <nav className="flex-1 px-4 py-6 space-y-1 overflow-y-auto">
            {navItems.map((item) => {
              const Icon = item.icon;
              return (
                <Link
                  key={item.name}
                  href={item.href}
                  className="flex items-center gap-3 px-3 py-2.5 rounded-md text-sm font-medium transition-colors text-muted-foreground hover:text-foreground hover:bg-secondary"
                >
                  <Icon className="h-4 w-4 shrink-0" />
                  {item.name}
                </Link>
              );
            })}
          </nav>

          {/* Tenant/Context Box */}
          <div className="p-4 border-t border-border bg-secondary/50">
            <div className="flex items-center gap-2 mb-1">
              <div className="h-2 w-2 rounded-full bg-emerald-500 animate-pulse"></div>
              <span className="text-[11px] uppercase tracking-wider font-semibold text-muted-foreground">
                Active Tenant
              </span>
            </div>
            <p className="text-xs font-semibold text-foreground truncate">
              Acme Enterprise
            </p>
            <p className="text-[10px] text-muted-foreground">
              Scope: Production
            </p>
          </div>
        </aside>

        {/* Content Body Container */}
        <div className="flex-1 flex flex-col overflow-hidden">
          {/* Top Bar Header */}
          <header className="h-16 border-b border-border bg-card/50 flex items-center justify-between px-8">
            <div className="flex items-center gap-4">
              <h2 className="text-sm font-medium text-muted-foreground">
                Admin Console
              </h2>
            </div>
            <div className="flex items-center gap-4">
              <div className="flex items-center gap-2 bg-secondary px-3 py-1.5 rounded-full border border-border">
                <UserCheck className="h-3.5 w-3.5 text-primary" />
                <span className="text-xs font-medium">admin@acme.com</span>
              </div>
            </div>
          </header>

          {/* Main workspace view */}
          <main className="flex-1 overflow-y-auto bg-background p-8">
            <Providers>{children}</Providers>
          </main>
        </div>
      </body>
    </html>
  );
}
