import { useEffect, useState } from "react";

type CopyState = "idle" | "copied" | "failed";

/** Copia `text` al portapapeles y lo confirma durante 2 s (anunciado con aria-live). */
export function CopyButton({ text, label = "Copiar" }: { text: string; label?: string }) {
  const [state, setState] = useState<CopyState>("idle");

  useEffect(() => {
    if (state === "idle") {
      return;
    }
    const timer = setTimeout(() => setState("idle"), 2000);
    return () => clearTimeout(timer);
  }, [state]);

  async function copy() {
    try {
      await navigator.clipboard.writeText(text);
      setState("copied");
    } catch {
      setState("failed"); // sin permiso o contexto no seguro
    }
  }

  return (
    <button type="button" className="button button--small" onClick={copy} aria-label={`${label} ${text}`}>
      <span aria-live="polite">{state === "copied" ? "¡Copiado!" : state === "failed" ? "No se pudo copiar" : label}</span>
    </button>
  );
}
