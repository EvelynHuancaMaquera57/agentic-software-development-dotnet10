import { useState, type FormEvent } from "react";
import { createUrl } from "../api/urls";
import type { CreateUrlRequest, UrlResponse } from "../api/types";
import { useApiForm } from "../hooks/useApiForm";
import { Alert } from "./Alert";
import { CopyButton } from "./CopyButton";
import { FormField } from "./FormField";

// 409 en POST /api/urls solo puede ser "alias en uso": se muestra bajo el campo alias.
const FIELDS = ["url", "alias"] as const;
const STATUS_FIELDS = { 409: "alias" } as const;

interface CreateUrlFormProps {
  /** "localhost:5080/" para mostrar delante del alias cómo quedará el enlace. */
  shortBase?: string | undefined;
  onCreated: (url: UrlResponse) => void;
}

export function CreateUrlForm({ shortBase, onCreated }: CreateUrlFormProps) {
  const [url, setUrl] = useState("");
  const [alias, setAlias] = useState("");
  const [created, setCreated] = useState<UrlResponse | null>(null);
  const { fieldErrors, formError, pending, submit, clearFieldError } = useApiForm(FIELDS, STATUS_FIELDS);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setCreated(null);
    await submit(async () => {
      const body: CreateUrlRequest = alias.trim() ? { url, alias: alias.trim() } : { url };
      const result = await createUrl(body);
      setCreated(result);
      setUrl("");
      setAlias("");
      onCreated(result);
    });
  }

  return (
    <section className="card" aria-labelledby="create-title">
      <h2 id="create-title">Acortar un enlace</h2>

      {formError && <Alert tone="error">{formError}</Alert>}
      {created && (
        <Alert tone="success">
          <span>
            Enlace creado:{" "}
            <a href={created.shortUrl} target="_blank" rel="noopener noreferrer">
              {created.shortUrl}
            </a>
          </span>{" "}
          <CopyButton text={created.shortUrl} />
        </Alert>
      )}

      <form className="create-form" onSubmit={handleSubmit} noValidate aria-busy={pending}>
        <FormField
          id="url"
          label="URL larga"
          type="url"
          placeholder="www.ejemplo.com/una/ruta/muy/larga"
          autoComplete="url"
          value={url}
          onChange={(value) => {
            setUrl(value);
            clearFieldError("url");
          }}
          error={fieldErrors.url}
          disabled={pending}
        />
        <FormField
          id="alias"
          label="Alias (opcional)"
          prefix={shortBase}
          placeholder="mi-enlace"
          autoComplete="off"
          required={false}
          hint="Letras, números, - y _ (3 a 30). Vacío = código aleatorio."
          value={alias}
          onChange={(value) => {
            setAlias(value);
            clearFieldError("alias");
          }}
          error={fieldErrors.alias}
          disabled={pending}
        />
        <button type="submit" className="button button--primary create-form__submit" disabled={pending}>
          {pending ? "Acortando…" : "Acortar"}
        </button>
      </form>
    </section>
  );
}
