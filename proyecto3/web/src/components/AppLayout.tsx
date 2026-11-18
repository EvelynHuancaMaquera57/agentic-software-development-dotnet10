import type { ReactNode } from "react";
import { useAuth } from "../auth/useAuth";

/** Cabecera común de las páginas privadas: marca, usuario y cerrar sesión. */
export function AppLayout({ children }: { children: ReactNode }) {
  const { user, logout } = useAuth();

  return (
    <div className="app">
      <header className="topbar">
        <div className="topbar__inner">
          <span className="brand brand--inline">Snap</span>
          <div className="topbar__user">
            <span className="topbar__email" title={user?.email}>
              {user?.email}
            </span>
            <button type="button" className="button button--small" onClick={logout}>
              Cerrar sesión
            </button>
          </div>
        </div>
      </header>
      <main className="page">{children}</main>
    </div>
  );
}
