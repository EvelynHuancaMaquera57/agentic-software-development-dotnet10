// Espejo en TypeScript de los contratos de la API (src/Modules/Auth/Contracts.cs).
// Las fechas llegan como ISO 8601 (string) y se convierten a Date solo al mostrarlas.

export interface User {
  id: number;
  email: string;
  name: string;
  createdAt: string;
}

/** Respuesta de POST /auth/register (201) y POST /auth/login (200). */
export interface AuthResponse {
  user: User;
  token: string;
  expiresAt: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  name: string;
}

/** GET /auth/me: los datos del JWT. */
export interface MeResponse {
  id: number;
  email: string;
  name: string;
}

/** application/problem+json (RFC 9457) tal como lo genera ASP.NET Core. */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  /** Solo en errores 400 de validación: campo → mensajes. */
  errors?: Record<string, string[]>;
}

// ---------- URLs (src/Modules/Urls/Contracts.cs) ----------

export interface CreateUrlRequest {
  url: string;
  /** Código personalizado. Si se omite, la API genera uno aleatorio de 7 caracteres. */
  alias?: string;
}

/** Respuesta de POST /api/urls (201). */
export interface UrlResponse {
  code: string;
  originalUrl: string;
  shortUrl: string;
  createdAt: string;
}

/** Elemento de GET /api/urls/mine. */
export interface MyUrl extends UrlResponse {
  totalClicks: number;
  lastClickAt: string | null;
}

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

// ---------- Dashboard (src/Modules/Dashboard/Contracts.cs) ----------

export type DashboardDays = 7 | 30 | 90;

export interface DashboardResponse {
  totals: { urls: number; clicks: number; urlsWithoutClicks: number };
  period: {
    days: DashboardDays;
    from: string;
    to: string;
    clicks: number;
    previousClicks: number;
    /** null si el periodo anterior no tuvo clicks (no hay base para comparar). */
    changePercent: number | null;
  };
  /** Exactamente `days` días naturales UTC, del más antiguo a hoy. */
  daily: { date: string; clicks: number }[];
  /** Top 20 del periodo. */
  urls: {
    code: string;
    originalUrl: string;
    shortUrl: string;
    createdAt: string;
    totalClicks: number;
    periodClicks: number;
    previousPeriodClicks: number;
    lastClickAt: string | null;
  }[];
}
