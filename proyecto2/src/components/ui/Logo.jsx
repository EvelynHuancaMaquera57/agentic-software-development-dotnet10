import { site } from '../../data/content'

export default function Logo({ className = '' }) {
  return (
    <a href="#inicio" className={className} aria-label={`${site.name} — inicio`} style={{ display: 'inline-flex', alignItems: 'center', gap: '0.625rem' }}>
      <svg width="32" height="32" viewBox="0 0 32 32" aria-hidden="true">
        <defs>
          <linearGradient id="logo-g" x1="0" y1="0" x2="1" y2="1">
            <stop offset="0" stopColor="#8b8cff" />
            <stop offset="1" stopColor="#22d3ee" />
          </linearGradient>
        </defs>
        <rect width="32" height="32" rx="9" fill="url(#logo-g)" />
        <path d="M9 21.5 14.5 10l3.2 6.6L20 13l3 8.5" fill="none" stroke="#fff" strokeWidth="2.4" strokeLinecap="round" strokeLinejoin="round" />
      </svg>
      <span style={{ fontFamily: 'var(--font-display)', fontWeight: 700, fontSize: '1.125rem', letterSpacing: '-0.02em' }}>
        Automatiza<span style={{ color: 'var(--brand-400)' }}>360</span>
      </span>
    </a>
  )
}
