import Link from "next/link";

export default function Page() {
  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold font-sans">Audit Logs</h1>
      <div className="bg-card border border-border p-6 rounded-lg">
        <p className="text-sm text-muted-foreground">This is the placeholder view for the **Audit Logs** section.</p>
        <p className="text-xs text-muted-foreground mt-2">Implementation of active vertical slices will be added in subsequent epics.</p>
      </div>
      <Link href="/" className="inline-flex items-center text-xs text-primary hover:underline">
        &larr; Back to Hub Home
      </Link>
    </div>
  );
}
