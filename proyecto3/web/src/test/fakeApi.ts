import { vi } from "vitest";
import type { AuthResponse, DashboardDays, DashboardResponse } from "../api/types";
import { SESSION_STORAGE_KEY } from "../auth/session";

type Handler = (init: RequestInit) => Response | Promise<Response>;

/**
 * Sustituye fetch por un falso con rutas "MÉTODO /ruta". Una petición sin ruta definida
 * hace fallar el test (así no se cuelan llamadas inesperadas).
 */
export function installFakeApi(routes: Record<string, Handler>) {
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init: RequestInit = {}) => {
    const url = new URL(String(input), "http://localhost");
    const key = `${init.method ?? "GET"} ${url.pathname}`;
    const handler = routes[key];
    if (!handler) {
      throw new Error(`Petición inesperada en el test: ${key}`);
    }
    return handler(init);
  });
  vi.stubGlobal("fetch", fetchMock);
  return fetchMock;
}

export function json(status: number, body: unknown, contentType = "application/json"): Response {
  return new Response(JSON.stringify(body), { status, headers: { "Content-Type": contentType } });
}

export function problem(status: number, body: object): Response {
  return json(status, { status, ...body }, "application/problem+json");
}

export function empty(status: number): Response {
  return new Response(null, { status });
}

/** Cuerpo JSON de la petición capturada por el mock. */
export function bodyOf(init: RequestInit | undefined): unknown {
  return JSON.parse(String(init?.body));
}

export function authResponse(overrides: Partial<AuthResponse["user"]> = {}): AuthResponse {
  return {
    user: { id: 1, email: "ana@example.com", name: "Ana", createdAt: "2026-11-18T12:00:00+00:00", ...overrides },
    token: "token-de-prueba",
    expiresAt: new Date(Date.now() + 24 * 3600_000).toISOString(),
  };
}

/** Deja una sesión guardada como si el usuario ya hubiera iniciado sesión. */
export function storeSession(auth: AuthResponse = authResponse()): void {
  localStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify({ token: auth.token, expiresAt: auth.expiresAt, user: auth.user }));
}

/** Respuesta de GET /api/dashboard sin datos: todo a cero y `days` días vacíos que acaban hoy. */
export function emptyDashboard(days: DashboardDays = 30): DashboardResponse {
  const today = Date.UTC(2026, 10, 18);
  return {
    totals: { urls: 0, clicks: 0, urlsWithoutClicks: 0 },
    period: { days, from: "2026-10-19T12:00:00+00:00", to: "2026-11-18T12:00:00+00:00", clicks: 0, previousClicks: 0, changePercent: null },
    daily: Array.from({ length: days }, (_, i) => ({
      date: new Date(today - (days - 1 - i) * 86_400_000).toISOString().slice(0, 10),
      clicks: 0,
    })),
    urls: [],
  };
}

/** Rutas que pide el dashboard al entrar en /app, con un usuario sin datos. */
export function dashboardRoutes() {
  return {
    "GET /auth/me": () => json(200, { id: 1, email: "ana@example.com", name: "Ana" }),
    "GET /api/dashboard": () => json(200, emptyDashboard()),
    "GET /api/urls/mine": () => json(200, { items: [], page: 1, pageSize: 10, total: 0 }),
  };
}
