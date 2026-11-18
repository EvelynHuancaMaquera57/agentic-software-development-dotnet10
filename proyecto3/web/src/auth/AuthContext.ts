import { createContext } from "react";
import type { LoginRequest, RegisterRequest, User } from "../api/types";

export interface AuthContextValue {
  user: User | null;
  isAuthenticated: boolean;
  /** Aviso para la pantalla de login (p. ej. "tu sesión ha expirado"). */
  notice: string | null;
  login(credentials: LoginRequest): Promise<void>;
  register(data: RegisterRequest): Promise<void>;
  logout(): void;
}

export const AuthContext = createContext<AuthContextValue | null>(null);
