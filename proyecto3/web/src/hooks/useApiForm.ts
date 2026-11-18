import { useEffect, useState } from "react";
import { ApiError, errorMessage } from "../api/errors";

/**
 * Estado común de los formularios que llaman a la API: envío, errores por campo y error general.
 * Los errores por campo vienen del servidor (400 con "errors"); el resto se muestra arriba del formulario.
 * `fieldForStatus` asigna a un campo el "detail" de otros códigos (p. ej. 409 alias en uso → campo alias).
 */
export function useApiForm<Field extends string>(
  fields: readonly Field[],
  fieldForStatus: Partial<Record<number, Field>> = {},
) {
  const [fieldErrors, setFieldErrors] = useState<Partial<Record<Field, string>>>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  // Lleva el foco al primer campo con error para que el usuario lo vea (y lo anuncie el lector).
  useEffect(() => {
    const first = fields.find((field) => fieldErrors[field]);
    if (first) {
      document.getElementById(first)?.focus();
    }
  }, [fieldErrors, fields]);

  async function submit(action: () => Promise<void>): Promise<ApiError | null> {
    setPending(true);
    setFieldErrors({});
    setFormError(null);
    try {
      await action();
      return null;
    } catch (error) {
      const apiError = error instanceof ApiError ? error : null;
      const known: Partial<Record<Field, string>> = {};
      const unknown: string[] = [];
      for (const [field, message] of Object.entries(apiError?.fieldErrors ?? {})) {
        if ((fields as readonly string[]).includes(field)) {
          known[field as Field] = message;
        } else {
          unknown.push(message);
        }
      }
      const statusField = apiError ? fieldForStatus[apiError.status] : undefined;
      if (statusField && apiError?.detail) {
        known[statusField] = apiError.detail;
      }
      setFieldErrors(known);
      if (Object.keys(known).length === 0) {
        setFormError(unknown.length > 0 ? unknown.join(" ") : errorMessage(error));
      }
      return apiError;
    } finally {
      setPending(false);
    }
  }

  /** Al editar un campo su error deja de tener sentido: se quita. */
  function clearFieldError(field: Field) {
    setFieldErrors((current) => {
      if (!current[field]) {
        return current;
      }
      const next = { ...current };
      delete next[field];
      return next;
    });
  }

  return { fieldErrors, formError, pending, submit, clearFieldError };
}
