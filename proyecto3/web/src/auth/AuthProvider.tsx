import { useCallback, useEffect, useMemo, useState, type ReactNode } from "react";
import * as authApi from "../api/auth";
import { setUnauthorizedHandler } from "../api/client";
import type { LoginRequest, RegisterRequest } from "../api/types";
import { AuthContext, type AuthContextValue } from "./AuthContext";
import { SESSION_STORAGE_KEY, clearSession, readSession, saveSession, type Session } from "./session";

export const SESSION_EXPIRED_NOTICE = "Tu sesión ha expirado. Inicia sesión de nuevo.";

/** Límite de setTimeout (~24,8 días). El token dura 24 h, pero por si acaso. */
const MAX_TIMEOUT_MS = 2_147_483_647;

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session | null>(() => readSession());
  const [notice, setNotice] = useState<string | null>(null);

  const expire = useCallback(() => {
    clearSession();
    setSession(null);
    setNotice(SESSION_EXPIRED_NOTICE);
  }, []);

  // Cualquier 401 de una petición autenticada (token caducado o inválido) cierra la sesión.
  useEffect(() => {
    setUnauthorizedHandler(expire);
    return () => setUnauthorizedHandler(null);
  }, [expire]);

  // Cierre automático cuando llega expiresAt (24 h tras el login).
  useEffect(() => {
    if (!session) {
      return;
    }
    const delay = Math.min(Math.max(Date.parse(session.expiresAt) - Date.now(), 0), MAX_TIMEOUT_MS);
    const timer = setTimeout(expire, delay);
    return () => clearTimeout(timer);
  }, [session, expire]);

  // Al arrancar con una sesión guardada, confirma con la API que el token sigue siendo válido.
  // Si responde 401, el handler de arriba cierra la sesión.
  const [initialToken] = useState(() => session?.token);
  useEffect(() => {
    if (!initialToken) {
      return;
    }
    const controller = new AbortController();
    authApi.me(controller.signal).catch(() => {
      // 401 ya lo gestiona el handler; un fallo de red no debe expulsar al usuario.
    });
    return () => controller.abort();
  }, [initialToken]); // Solo al montar: tras un login el token es nuevo y ya viene validado por la API.

  // Login/logout en otra pestaña.
  useEffect(() => {
    const onStorage = (event: StorageEvent) => {
      if (event.key === SESSION_STORAGE_KEY || event.key === null) {
        setSession(readSession());
      }
    };
    window.addEventListener("storage", onStorage);
    return () => window.removeEventListener("storage", onStorage);
  }, []);

  const start = useCallback((auth: Awaited<ReturnType<typeof authApi.login>>) => {
    setSession(saveSession(auth));
    setNotice(null);
  }, []);

  const value = useMemo<AuthContextValue>(() => ({
    user: session?.user ?? null,
    isAuthenticated: session !== null,
    notice,
    login: async (credentials: LoginRequest) => start(await authApi.login(credentials)),
    register: async (data: RegisterRequest) => start(await authApi.register(data)),
    logout: () => {
      clearSession();
      setSession(null);
      setNotice(null);
    },
  }), [session, notice, start]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
