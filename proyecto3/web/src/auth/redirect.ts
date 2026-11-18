export const DEFAULT_AFTER_LOGIN = "/app";

/**
 * Valida el parámetro ?next= para no convertir el login en una redirección abierta:
 * solo rutas internas ("/app/urls"), nunca "//otro.com", "https://..." ni "/\otro.com".
 */
export function safeNext(next: string | null | undefined): string {
  if (!next || !next.startsWith("/") || next.startsWith("//") || next.startsWith("/\\")) {
    return DEFAULT_AFTER_LOGIN;
  }
  return next;
}

/** "/login?next=%2Fapp%2Furls" a partir de la ruta actual. */
export function loginPathFor(currentPath: string): string {
  return `/login?next=${encodeURIComponent(currentPath)}`;
}
