import { useState, type FormEvent } from "react";
import { Link, useLocation } from "react-router";
import { Alert } from "../components/Alert";
import { AuthCard } from "../components/AuthCard";
import { FormField } from "../components/FormField";
import { useApiForm } from "../hooks/useApiForm";
import { useAuth } from "../auth/useAuth";

const FIELDS = ["email", "password"] as const;

/** Estado de navegación opcional: el registro manda aquí el email si ya existía la cuenta. */
interface LoginLocationState {
  email?: string;
}

export function LoginPage() {
  const { login, notice } = useAuth();
  const location = useLocation();
  const prefilledEmail = (location.state as LoginLocationState | null)?.email ?? "";

  const [email, setEmail] = useState(prefilledEmail);
  const [password, setPassword] = useState("");
  const { fieldErrors, formError, pending, submit, clearFieldError } = useApiForm(FIELDS);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    // Si va bien, la sesión cambia y RedirectIfAuthenticated lleva al dashboard (o a ?next=).
    const error = await submit(() => login({ email, password }));
    if (error?.status === 401) {
      setPassword(""); // credenciales incorrectas: se vuelve a escribir el password
    }
  }

  return (
    <AuthCard
      title="Inicia sesión"
      subtitle="Accede a tus enlaces y a sus estadísticas."
      footer={
        <>
          ¿No tienes cuenta? <Link to={{ pathname: "/register", search: location.search }}>Regístrate</Link>
        </>
      }
    >
      {notice && !formError && <Alert tone="info">{notice}</Alert>}
      {formError && <Alert tone="error">{formError}</Alert>}

      <form onSubmit={handleSubmit} noValidate aria-busy={pending}>
        <FormField
          id="email"
          label="Email"
          type="email"
          autoComplete="email"
          value={email}
          onChange={(value) => {
            setEmail(value);
            clearFieldError("email");
          }}
          error={fieldErrors.email}
          autoFocus={!prefilledEmail}
          disabled={pending}
        />
        <FormField
          id="password"
          label="Password"
          type="password"
          autoComplete="current-password"
          value={password}
          onChange={(value) => {
            setPassword(value);
            clearFieldError("password");
          }}
          error={fieldErrors.password}
          autoFocus={Boolean(prefilledEmail)}
          disabled={pending}
        />
        <button type="submit" className="button button--primary" disabled={pending}>
          {pending ? "Entrando…" : "Entrar"}
        </button>
      </form>
    </AuthCard>
  );
}
