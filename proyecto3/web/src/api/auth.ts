import { request } from "./client";
import type { AuthResponse, LoginRequest, MeResponse, RegisterRequest } from "./types";

/** POST /auth/login → 200. Errores: 400 (campo), 401 (credenciales incorrectas). */
export function login(body: LoginRequest): Promise<AuthResponse> {
  return request<AuthResponse>("POST", "/auth/login", { body, auth: false });
}

/** POST /auth/register → 201. Errores: 400 (campo), 409 (email ya registrado). */
export function register(body: RegisterRequest): Promise<AuthResponse> {
  return request<AuthResponse>("POST", "/auth/register", { body, auth: false });
}

/** GET /auth/me → 200 si el token guardado sigue siendo válido; 401 si no. */
export function me(signal?: AbortSignal): Promise<MeResponse> {
  return request<MeResponse>("GET", "/auth/me", signal ? { signal } : {});
}
