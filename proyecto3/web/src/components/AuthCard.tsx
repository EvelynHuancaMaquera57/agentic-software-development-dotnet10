import type { ReactNode } from "react";

interface AuthCardProps {
  title: string;
  subtitle: string;
  footer: ReactNode;
  children: ReactNode;
}

/** Marco común de las pantallas de login y registro. */
export function AuthCard({ title, subtitle, footer, children }: AuthCardProps) {
  return (
    <main className="auth">
      <section className="auth__card" aria-labelledby="auth-title">
        <p className="brand" aria-hidden="true">
          Snap
        </p>
        <h1 id="auth-title">{title}</h1>
        <p className="auth__subtitle">{subtitle}</p>
        {children}
        <p className="auth__footer">{footer}</p>
      </section>
    </main>
  );
}
