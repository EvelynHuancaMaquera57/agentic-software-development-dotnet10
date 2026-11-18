import { Link } from "react-router";

export function NotFoundPage() {
  return (
    <main className="placeholder">
      <p className="brand">Snap</p>
      <h1>Página no encontrada</h1>
      <p>La dirección que buscas no existe.</p>
      <Link to="/" className="button">
        Volver al inicio
      </Link>
    </main>
  );
}
