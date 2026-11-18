import type { HTMLInputAutoCompleteAttribute } from "react";

interface FormFieldProps {
  id: string;
  label: string;
  value: string;
  onChange: (value: string) => void;
  type?: "text" | "email" | "password" | "url" | undefined;
  autoComplete?: HTMLInputAutoCompleteAttribute | undefined;
  placeholder?: string | undefined;
  /** Texto fijo delante del input (p. ej. "localhost:5080/" en el alias). */
  prefix?: string | undefined;
  /** Texto de ayuda permanente bajo el campo. */
  hint?: string | undefined;
  /** Error del servidor para este campo; marca el input como inválido. */
  error?: string | undefined;
  autoFocus?: boolean | undefined;
  disabled?: boolean | undefined;
  /** false para campos opcionales (por defecto, obligatorio). */
  required?: boolean | undefined;
}

/** Label + input + ayuda + error, enlazados con aria-describedby para lectores de pantalla. */
export function FormField({
  id, label, value, onChange, type = "text", autoComplete, placeholder, prefix, hint, error, autoFocus, disabled, required = true,
}: FormFieldProps) {
  const hintId = hint ? `${id}-hint` : undefined;
  const errorId = error ? `${id}-error` : undefined;
  const describedBy = [errorId, hintId].filter(Boolean).join(" ") || undefined;

  const input = (
    <input
      id={id}
      name={id}
      type={type}
      value={value}
      onChange={(event) => onChange(event.target.value)}
      autoComplete={autoComplete}
      placeholder={placeholder}
      autoFocus={autoFocus}
      disabled={disabled}
      required={required}
      aria-invalid={error ? true : undefined}
      aria-describedby={describedBy}
    />
  );

  return (
    <div className={error ? "field field--invalid" : "field"}>
      <label htmlFor={id}>{label}</label>
      {prefix ? (
        <div className="field__group">
          <span className="field__prefix" aria-hidden="true">
            {prefix}
          </span>
          {input}
        </div>
      ) : (
        input
      )}
      {error && (
        <p id={errorId} className="field__error">
          {error}
        </p>
      )}
      {hint && (
        <p id={hintId} className="field__hint">
          {hint}
        </p>
      )}
    </div>
  );
}
