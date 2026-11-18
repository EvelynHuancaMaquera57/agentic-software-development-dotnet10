import { Navigate, Outlet, useLocation, useSearchParams } from "react-router";
import { loginPathFor, safeNext } from "./redirect";
import { useAuth } from "./useAuth";

/** Rutas privadas: sin sesión → /login?next=<ruta actual>. */
export function RequireAuth() {
  const { isAuthenticated } = useAuth();
  const location = useLocation();
  if (!isAuthenticated) {
    return <Navigate to={loginPathFor(location.pathname + location.search)} replace />;
  }
  return <Outlet />;
}

/**
 * Login y registro: con sesión → a ?next= (o al dashboard). Así, tras un login correcto,
 * el cambio de sesión basta para redirigir, sin navegación manual en cada página.
 */
export function RedirectIfAuthenticated() {
  const { isAuthenticated } = useAuth();
  const [searchParams] = useSearchParams();
  if (isAuthenticated) {
    return <Navigate to={safeNext(searchParams.get("next"))} replace />;
  }
  return <Outlet />;
}
