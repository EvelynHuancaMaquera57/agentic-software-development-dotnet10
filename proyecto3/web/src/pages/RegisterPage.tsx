import { useState, type FormEvent } from "react";
import { Link, useLocation } from "react-router";
import { Alert } from "../components/Alert";
import { AuthCard } from "../components/AuthCard";
import { FormField } from "../components/FormField";
import { useApiForm } from "../hooks/useApiForm";
import { useAuth } from "../auth/useAuth";

const FIELDS = ["name", "email", "password"] as const;

export function RegisterPage() {
  const { register } = useAuth();
  const location = useLocation();

  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [emailTaken, setEmailTaken] = useState(false);
  const { fieldErrors, formError, pending, submit, clearFieldError } = useApiForm(FIELDS);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    // Si va bien, la sesión cambia y RedirectIfAuthenticated lleva al dashboard (o a ?next=).
    const error = await submit(() => register({ name, email, password }));
    setEmailTaken(error?.status === 409);
  }

  const loginLink = { pathname: "/login", search: location.search };

  return (
    <AuthCard
      title="Crea tu cuenta"
      subtitle="Acorta enlaces y mide cuántas visitas reciben."
      footer={
        <>
          ¿Ya tienes cuenta? <Link to={loginLink}>Inicia sesión</Link>
        </>
      }
    >
      {formError && (
        <Alert tone="error">
          {formError}
          {emailTaken && (
            <>
              {" "}
              <Link to={loginLink} state={{ email }}>
                ¿Quieres iniciar sesión con ese email?
              </Link>
            </>
          )}
        </Alert>
      )}

      <form onSubmit={handleSubmit} noValidate aria-busy={pending}>
        <FormField
          id="name"
          label="Nombre"
          autoComplete="name"
          value={name}
          onChange={(value) => {
            setName(value);
            clearFieldError("name");
          }}
          error={fieldErrors.name}
          autoFocus
          disabled={pending}
        />
        <FormField
          id="email"
          label="Email"
          type="email"
          autoComplete="email"
          value={email}
          onChange={(value) => {
            setEmail(value);
            clearFieldError("email");
            setEmailTaken(false);
          }}
          error={fieldErrors.email}
          disabled={pending}
        />
        <FormField
          id="password"
          label="Password"
          type="password"
          autoComplete="new-password"
          hint="Mínimo 8 caracteres."
          value={password}
          onChange={(value) => {
            setPassword(value);
            clearFieldError("password");
          }}
          error={fieldErrors.password}
          disabled={pending}
        />
        <button type="submit" className="button button--primary" disabled={pending}>
          {pending ? "Creando cuenta…" : "Crear cuenta"}
        </button>
      </form>
    </AuthCard>
  );
}
