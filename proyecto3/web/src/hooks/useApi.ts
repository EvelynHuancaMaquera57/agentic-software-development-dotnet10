import { useCallback, useEffect, useState, type DependencyList } from "react";

export interface ApiState<T> {
  data: T | null;
  error: unknown;
  loading: boolean;
  /** Vuelve a pedir los datos (p. ej. tras crear una URL). Mantiene los anteriores mientras carga. */
  reload: () => void;
}

/**
 * Carga datos al montar y cuando cambian `deps`. Cancela la petición anterior (AbortController),
 * así una respuesta lenta de "30 días" no pisa a una más nueva de "7 días".
 */
export function useApi<T>(load: (signal: AbortSignal) => Promise<T>, deps: DependencyList): ApiState<T> {
  const [data, setData] = useState<T | null>(null);
  const [error, setError] = useState<unknown>(null);
  const [loading, setLoading] = useState(true);
  const [version, setVersion] = useState(0);

  useEffect(() => {
    const controller = new AbortController();
    setLoading(true);
    setError(null);
    load(controller.signal)
      .then((result) => {
        if (!controller.signal.aborted) {
          setData(result);
          setLoading(false);
        }
      })
      .catch((reason: unknown) => {
        if (!controller.signal.aborted) {
          setError(reason);
          setLoading(false);
        }
      });
    return () => controller.abort();
    // `load` se recrea en cada render; lo que decide recargar son `deps` y `version`.
  }, [...deps, version]);

  const reload = useCallback(() => setVersion((current) => current + 1), []);
  return { data, error, loading, reload };
}
