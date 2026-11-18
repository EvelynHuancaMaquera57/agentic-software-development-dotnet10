const LOCALE = "es-PE";

const integer = new Intl.NumberFormat(LOCALE);
const dateTime = new Intl.DateTimeFormat(LOCALE, { dateStyle: "medium", timeStyle: "short" });
const shortDate = new Intl.DateTimeFormat(LOCALE, { day: "numeric", month: "short", timeZone: "UTC" });
const relative = new Intl.RelativeTimeFormat(LOCALE, { numeric: "auto" });

export function formatNumber(value: number): string {
  return integer.format(value);
}

/** Fecha y hora en la zona del navegador: "18 nov 2026, 13:33". */
export function formatDateTime(iso: string): string {
  return dateTime.format(new Date(iso));
}

/** Día natural UTC ("2026-11-18" → "18 nov"), como agrupa la API. */
export function formatDay(day: string): string {
  return shortDate.format(new Date(`${day}T00:00:00Z`));
}

/** "+40 %", "−12,5 %", "0 %". */
export function formatChange(percent: number): string {
  const sign = percent > 0 ? "+" : percent < 0 ? "−" : "";
  return `${sign}${new Intl.NumberFormat(LOCALE, { maximumFractionDigits: 1 }).format(Math.abs(percent))} %`;
}

const UNITS: [Intl.RelativeTimeFormatUnit, number][] = [
  ["year", 365 * 24 * 3600],
  ["month", 30 * 24 * 3600],
  ["day", 24 * 3600],
  ["hour", 3600],
  ["minute", 60],
];

/** "hace 5 minutos", "ayer", "hace un momento". */
export function formatRelative(iso: string, now: number = Date.now()): string {
  const seconds = Math.round((Date.parse(iso) - now) / 1000);
  for (const [unit, size] of UNITS) {
    if (Math.abs(seconds) >= size) {
      return relative.format(Math.round(seconds / size), unit);
    }
  }
  return "hace un momento";
}
