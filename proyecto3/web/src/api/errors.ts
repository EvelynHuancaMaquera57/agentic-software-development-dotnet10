/** Error de una llamada a la API, ya normalizado para la interfaz. */
export class ApiError extends Error {
  /** Código HTTP, o 0 si no hubo respuesta (servidor caído, sin red). */
  readonly status: number;
  /** El "detail" del problem+json, si el servidor lo envió. */
  readonly detail: string | null;
  /** Errores de validación por campo (primer mensaje de cada uno). Vacío si no los hay. */
  readonly fieldErrors: Readonly<Record<string, string>>;

  constructor(status: number, detail: string | null = null, fieldErrors: Record<string, string> = {}) {
    super(detail ?? `Error HTTP ${status}`);
    this.name = "ApiError";
    this.status = status;
    this.detail = detail;
    this.fieldErrors = fieldErrors;
  }

  get hasFieldErrors(): boolean {
    return Object.keys(this.fieldErrors).length > 0;
  }
}

export const NETWORK_ERROR_MESSAGE =
  "No se pudo conectar con el servidor. Comprueba tu conexión o que la API de Snap esté en marcha.";

/**
 * Mensaje general para mostrar al usuario cuando el error no es de un campo concreto.
 * Prioriza el texto del servidor (ya está en español y es específico) y si no hay, uno según el código.
 */
export function errorMessage(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return "Ha ocurrido un error inesperado. Inténtalo de nuevo.";
  }
  if (error.status === 0) {
    return NETWORK_ERROR_MESSAGE;
  }
  if (error.status >= 500) {
    return `El servidor tuvo un problema (error ${error.status}). Inténtalo de nuevo en unos segundos.`;
  }
  if (error.detail) {
    return error.detail;
  }
  if (error.status === 400) {
    return "Los datos enviados no son válidos. Revisa el formulario.";
  }
  if (error.status === 401) {
    return "No autorizado. Inicia sesión de nuevo.";
  }
  return `Error inesperado (${error.status}). Inténtalo de nuevo.`;
}
