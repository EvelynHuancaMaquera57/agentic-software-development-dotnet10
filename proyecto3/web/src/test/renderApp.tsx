import { render } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, useLocation } from "react-router";
import { App } from "../App";
import { AuthProvider } from "../auth/AuthProvider";

/** Muestra la ruta actual para poder comprobar redirecciones. */
function LocationProbe() {
  const location = useLocation();
  return <div data-testid="location" hidden>{location.pathname + location.search}</div>;
}

/** Renderiza la app completa (rutas + AuthProvider) empezando en `path`. */
export function renderApp(path: string) {
  const user = userEvent.setup();
  const result = render(
    <MemoryRouter initialEntries={[path]}>
      <AuthProvider>
        <App />
        <LocationProbe />
      </AuthProvider>
    </MemoryRouter>,
  );
  return { user, ...result };
}
