// Shared between the Navbar (authenticated pages) and LoginPage (unauthenticated),
// so the same identity — wordmark + pulsing dot — is the very first thing anyone
// sees, before they've even logged in.
export default function BrandMark() {
  return (
    <div className="flex items-center gap-2">
      <span className="relative flex h-2.5 w-2.5">
        <span className="live-dot absolute inline-flex h-full w-full rounded-full bg-live" />
      </span>
      <span className="font-[family-name:var(--font-display)] text-lg font-semibold tracking-tight text-ink">
        FlowBoard
      </span>
    </div>
  );
}
