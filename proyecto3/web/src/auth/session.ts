import type { AuthResponse, User } from "../api/types";

/** Lo que se guarda tras un login/registro correcto. */
export interface Session {
  token: string;
  expiresAt: string;
  user: User;
}

export const SESSION_STORAGE_KEY = "snap.session";

/**
 * Lee la sesión guardada. Devuelve null (y la borra) si no hay, está corrupta o ya caducó.
 * localStorage puede lanzar (modo privado, almacenamiento bloqueado): en ese caso no hay sesión.
 */
export function readSession(now: number = Date.now()): Session | null {
  let raw: string | null;
  try {
    raw = localStorage.getItem(SESSION_STORAGE_KEY);
  } catch {
    return null;
  }
  if (!raw) {
    return null;
  }

  const session = parse(raw);
  if (!session || Date.parse(session.expiresAt) <= now) {
    clearSession();
    return null;
  }
  return session;
}

export function saveSession(auth: AuthResponse): Session {
  const session: Session = { token: auth.token, expiresAt: auth.expiresAt, user: auth.user };
  try {
    localStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(session));
  } catch {
    // Sin almacenamiento la sesión vive solo en memoria (se pierde al recargar).
  }
  return session;
}

export function clearSession(): void {
  try {
    localStorage.removeItem(SESSION_STORAGE_KEY);
  } catch {
    // nada que borrar
  }
}

function parse(raw: string): Session | null {
  try {
    const value: unknown = JSON.parse(raw);
    if (
      typeof value === "object" && value !== null &&
      "token" in value && typeof value.token === "string" &&
      "expiresAt" in value && typeof value.expiresAt === "string" && !Number.isNaN(Date.parse(value.expiresAt)) &&
      "user" in value && typeof value.user === "object" && value.user !== null
    ) {
      return value as Session;
    }
  } catch {
    // JSON inválido
  }
  return null;
}
