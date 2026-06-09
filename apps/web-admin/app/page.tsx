import Link from "next/link";
import {
  Coins,
  Cpu,
  Route,
  Server,
  Key,
  ShieldAlert,
  ArrowRight,
  TrendingUp,
  Percent,
  CheckCircle,
  HelpCircle
} from "lucide-react";

export default function Home() {
  // Mock metrics for high-fidelity presentation
  const metrics = [
    { name: "Estimated Monthly Spend", value: "$1,245.89", change: "+12.4% vs last month", icon: Coins, color: "text-emerald-500" },
    { name: "Total Routed Requests", value: "348,290", change: "99.99% success rate", icon: Route, color: "text-blue-500" },
    { name: "Active Budget Status", value: "Within Limits", change: "0 warnings triggered", icon: ShieldAlert, color: "text-purple-500" },
    { name: "Cheap Tier Adoption", value: "62.4%", change: "Saved ~$800 this month", icon: Percent, color: "text-amber-500" },
  ];

  const tiers = [
    {
      name: "Cheap",
      desc: "Fast, highly cost-effective models for simple tasks like classification, parsing, or low-risk inputs.",
      models: ["gpt-4o-mini", "claude-3-haiku", "mock-cheap"],
      color: "border-emerald-500/30 bg-emerald-500/5 text-emerald-400"
    },
    {
      name: "Standard",
      desc: "Balanced models for general-purpose text generation, conversational agents, and medium-complexity tasks.",
      models: ["gpt-4o", "claude-3-5-sonnet", "mock-standard"],
      color: "border-blue-500/30 bg-blue-500/5 text-blue-400"
    },
    {
      name: "Premium",
      desc: "Advanced reasoning, complex logic planning, or high-risk human-in-the-loop workflows.",
      models: ["o1-preview", "claude-3-opus", "mock-premium"],
      color: "border-purple-500/30 bg-purple-500/5 text-purple-400"
    }
  ];

  return (
    <div className="space-y-8">
      {/* Hero Welcome Panel */}
      <section className="bg-card border border-border p-8 rounded-lg shadow-sm relative overflow-hidden">
        <div className="max-w-2xl space-y-4 relative z-10">
          <div className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full bg-primary/10 border border-primary/20 text-xs font-semibold text-primary">
            <TrendingUp className="h-3.5 w-3.5" />
            Active AI Governance Proxy
          </div>
          <h1 className="text-3xl font-extrabold tracking-tight text-foreground sm:text-4xl">
            Control LLM Cost, Routing, & Policy
          </h1>
          <p className="text-base text-muted-foreground leading-relaxed">
            Welcome to the **TokenShield AI Gateway** administration console. TokenShield intercepts every AI request, profiling payload size, predicting downstream cost, checking monthly tenant budgets, and dynamically routing to the optimal tier.
          </p>
          <div className="flex gap-4 pt-2">
            <Link
              href="/api-keys"
              className="inline-flex items-center justify-center px-4 py-2 text-sm font-medium text-primary-foreground bg-primary rounded-md shadow hover:bg-primary/95 transition-colors gap-2"
            >
              Configure API Keys
              <ArrowRight className="h-4 w-4" />
            </Link>
            <Link
              href="/routing-rules"
              className="inline-flex items-center justify-center px-4 py-2 text-sm font-medium text-foreground bg-secondary rounded-md hover:bg-secondary/80 border border-border transition-colors"
            >
              Manage Routing Rules
            </Link>
          </div>
        </div>
        {/* Subtle background glow */}
        <div className="absolute top-0 right-0 w-96 h-96 bg-primary/10 rounded-full blur-3xl -translate-y-12 translate-x-12 z-0"></div>
      </section>

      {/* Metrics Grid */}
      <section className="grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
        {metrics.map((metric, i) => {
          const Icon = metric.icon;
          return (
            <div key={i} className="bg-card border border-border p-6 rounded-lg flex items-center justify-between">
              <div className="space-y-1.5">
                <p className="text-xs font-medium text-muted-foreground uppercase tracking-wider">{metric.name}</p>
                <h3 className="text-2xl font-bold tracking-tight">{metric.value}</h3>
                <p className="text-[11px] text-muted-foreground">{metric.change}</p>
              </div>
              <div className={`p-3 bg-secondary rounded-lg ${metric.color}`}>
                <Icon className="h-5 w-5" />
              </div>
            </div>
          );
        })}
      </section>

      {/* Tiers & Proxy Workflow */}
      <div className="grid gap-8 lg:grid-cols-3">
        {/* Model Tiers Column */}
        <div className="lg:col-span-2 space-y-6">
          <div className="space-y-1">
            <h2 className="text-lg font-semibold text-foreground">Active Model Tiers</h2>
            <p className="text-sm text-muted-foreground">Dynamic routing targets mapped inside TokenShield.</p>
          </div>

          <div className="grid gap-4 sm:grid-cols-3">
            {tiers.map((tier) => (
              <div key={tier.name} className={`border p-5 rounded-lg flex flex-col justify-between ${tier.color}`}>
                <div className="space-y-2">
                  <h3 className="font-bold text-base uppercase tracking-wider">{tier.name}</h3>
                  <p className="text-xs leading-relaxed opacity-90">{tier.desc}</p>
                </div>
                <div className="mt-4 pt-4 border-t border-current/10">
                  <span className="text-[10px] uppercase font-bold tracking-wider opacity-70 block mb-1.5">Active Providers</span>
                  <div className="flex flex-wrap gap-1">
                    {tier.models.map((model) => (
                      <span key={model} className="text-[9px] bg-foreground/10 px-1.5 py-0.5 rounded font-mono">
                        {model}
                      </span>
                    ))}
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Gateway Pipeline Flow Column */}
        <div className="bg-card border border-border p-6 rounded-lg space-y-4">
          <h3 className="text-sm font-semibold uppercase tracking-wider text-foreground">Proxy Middleware Pipeline</h3>
          <div className="relative border-l-2 border-primary/30 pl-6 ml-2 space-y-6 py-2">
            
            <div className="relative">
              <span className="absolute -left-[31px] top-0.5 bg-primary text-primary-foreground h-4 w-4 rounded-full flex items-center justify-center text-[9px] font-bold">1</span>
              <h4 className="text-xs font-semibold">API Key Auth</h4>
              <p className="text-[10px] text-muted-foreground">Validates sha-256 hashed headers against tenant records.</p>
            </div>

            <div className="relative">
              <span className="absolute -left-[31px] top-0.5 bg-primary text-primary-foreground h-4 w-4 rounded-full flex items-center justify-center text-[9px] font-bold">2</span>
              <h4 className="text-xs font-semibold">Profiling & Estimation</h4>
              <p className="text-[10px] text-muted-foreground">Calculates input tokens (1 char ≈ 0.25 tokens) & inspects PII.</p>
            </div>

            <div className="relative">
              <span className="absolute -left-[31px] top-0.5 bg-primary text-primary-foreground h-4 w-4 rounded-full flex items-center justify-center text-[9px] font-bold">3</span>
              <h4 className="text-xs font-semibold">Pre-Call Budget Check</h4>
              <p className="text-[10px] text-muted-foreground">Verifies threshold limits before routing or calling provider adapters.</p>
            </div>

            <div className="relative">
              <span className="absolute -left-[31px] top-0.5 bg-primary text-primary-foreground h-4 w-4 rounded-full flex items-center justify-center text-[9px] font-bold">4</span>
              <h4 className="text-xs font-semibold">Rule-Based Router</h4>
              <p className="text-[10px] text-muted-foreground">Selects model tier matching configured routing rules and conditions.</p>
            </div>

            <div className="relative">
              <span className="absolute -left-[31px] top-0.5 bg-primary text-primary-foreground h-4 w-4 rounded-full flex items-center justify-center text-[9px] font-bold">5</span>
              <h4 className="text-xs font-semibold">Provider Adapter & Logs</h4>
              <p className="text-[10px] text-muted-foreground">Calls OpenAI/Azure/Anthropic/Mock, updates budgets, and saves hash logs.</p>
            </div>
            
          </div>
        </div>
      </div>

      {/* Quick Links Section */}
      <section className="bg-secondary/40 border border-border p-6 rounded-lg">
        <h3 className="text-sm font-semibold uppercase tracking-wider mb-4 flex items-center gap-2">
          <HelpCircle className="h-4 w-4 text-primary" />
          Gateway Management Setup Links
        </h3>
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <Link href="/providers" className="bg-card hover:bg-secondary p-4 rounded-md border border-border flex items-center gap-3 transition-colors">
            <Server className="h-4 w-4 text-primary" />
            <div className="text-left">
              <span className="text-xs font-semibold block">Model Providers</span>
              <span className="text-[10px] text-muted-foreground">Manage API URL integrations</span>
            </div>
          </Link>
          <Link href="/models" className="bg-card hover:bg-secondary p-4 rounded-md border border-border flex items-center gap-3 transition-colors">
            <Cpu className="h-4 w-4 text-primary" />
            <div className="text-left">
              <span className="text-xs font-semibold block">Models Catalog</span>
              <span className="text-[10px] text-muted-foreground">Configure pricing per million</span>
            </div>
          </Link>
          <Link href="/budgets" className="bg-card hover:bg-secondary p-4 rounded-md border border-border flex items-center gap-3 transition-colors">
            <Coins className="h-4 w-4 text-primary" />
            <div className="text-left">
              <span className="text-xs font-semibold block">Spend Budgets</span>
              <span className="text-[10px] text-muted-foreground">Define warning & block limits</span>
            </div>
          </Link>
          <Link href="/api-keys" className="bg-card hover:bg-secondary p-4 rounded-md border border-border flex items-center gap-3 transition-colors">
            <Key className="h-4 w-4 text-primary" />
            <div className="text-left">
              <span className="text-xs font-semibold block">Gateway API Keys</span>
              <span className="text-[10px] text-muted-foreground">Generate live & dev access credentials</span>
            </div>
          </Link>
        </div>
      </section>
    </div>
  );
}
