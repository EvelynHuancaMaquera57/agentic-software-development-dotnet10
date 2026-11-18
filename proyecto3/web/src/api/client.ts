import { readSession } from "../auth/session";
import { ApiError, NETWORK_ERROR_MESSAGE } from "./errors";
import type { ProblemDetails } from "./types";

/** Vacío = mismo origen (en desarrollo, el proxy de Vite reenvía /api y /auth a la API). */
const baseUrl = (import.meta.env.VITE_API_BASE_URL ?? "").replace(/\/$/, "");

type HttpMethod = "GET" | "POST" | "PUT" | "DELETE";

export interface RequestOptions {
  body?: unknown;
  signal?: AbortSignal;
  /**
   * Si es true (por defecto) se envía el token guardado y un 401 cierra la sesión.
   * Login y registro usan false: no llevan token y su 401 significa "credenciales incorrectas".
   */
  auth?: boolean;
}

let onUnauthorized: (() => void) | null = null;

/** Lo registra AuthProvider: se llama cuando una petición autenticada recibe 401 (token caducado o inválido). */
export function setUnauthorizedHandler(handler: (() => void) | null): void {
  onUnauthorized = handler;
}

/** Único punto que usa fetch. Devuelve el JSON tipado o lanza ApiError. */
export async function request<T>(method: HttpMethod, path: string, options: RequestOptions = {}): Promise<T> {
  const { body, signal, auth = true } = options;
  const headers = new Headers({ Accept: "application/json" });
  const token = auth ? readSession()?.token : undefined;
  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  const init: RequestInit = { method, headers };
  if (body !== undefined) {
    headers.set("Content-Type", "application/json");
    init.body = JSON.stringify(body);
  }
  if (signal) {
    init.signal = signal;
  }

  let response: Response;
  try {
    response = await fetch(`${baseUrl}${path}`, init);
  } catch (error) {
    if (error instanceof DOMException && error.name === "AbortError") {
      throw error; // cancelación intencionada: no es un error que mostrar
    }
    throw new ApiError(0, NETWORK_ERROR_MESSAGE);
  }

  if (response.ok) {
    const text = response.status === 204 ? "" : await response.text();
    return (text ? JSON.parse(text) : undefined) as T;
  }

  if (response.status === 401 && token) {
    onUnauthorized?.();
  }

  const problem = await readProblem(response);
  throw new ApiError(response.status, problem?.detail ?? null, firstErrorPerField(problem?.errors));
}

/** Algunos errores llegan sin cuerpo (401 del middleware, 404, JSON mal formado): devuelve null. */
async function readProblem(response: Response): Promise<ProblemDetails | null> {
  const contentType = response.headers.get("Content-Type") ?? "";
  if (!contentType.includes("json")) {
    return null;
  }
  try {
    return (await response.json()) as ProblemDetails;
  } catch {
    return null;
  }
}

function firstErrorPerField(errors: Record<string, string[]> | undefined): Record<string, string> {
  const result: Record<string, string> = {};
  for (const [field, messages] of Object.entries(errors ?? {})) {
    const first = messages[0];
    if (first) {
      result[field] = first;
    }
  }
  return result;
}
