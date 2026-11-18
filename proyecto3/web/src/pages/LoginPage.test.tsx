import { screen, waitFor } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { SESSION_STORAGE_KEY } from "../auth/session";
import { authResponse, bodyOf, dashboardRoutes, empty, installFakeApi, json, problem, storeSession } from "../test/fakeApi";
import { renderApp } from "../test/renderApp";

async function fillAndSubmit(user: ReturnType<typeof renderApp>["user"], email: string, password: string) {
  await user.type(screen.getByLabelText("Email"), email);
  await user.type(screen.getByLabelText("Password"), password);
  await user.click(screen.getByRole("button", { name: "Entrar" }));
}

describe("LoginPage", () => {
  it("login correcto: llama a POST /auth/login, guarda el token y va al dashboard", async () => {
    const fetchMock = installFakeApi({ ...dashboardRoutes(), "POST /auth/login": () => json(200, authResponse()) });
    const { user } = renderApp("/login");

    await fillAndSubmit(user, "Ana@Example.com", "s3cret-pass");

    expect(await screen.findByRole("heading", { name: "Hola, Ana" })).toBeTruthy();
    expect(screen.getByTestId("location").textContent).toBe("/app");
    expect(bodyOf(fetchMock.mock.calls[0]?.[1])).toEqual({ email: "Ana@Example.com", password: "s3cret-pass" });
    expect(JSON.parse(localStorage.getItem(SESSION_STORAGE_KEY)!).token).toBe("token-de-prueba");
  });

  it("vuelve a la ruta pedida en ?next=", async () => {
    installFakeApi({ ...dashboardRoutes(), "POST /auth/login": () => json(200, authResponse()) });
    const { user } = renderApp("/login?next=%2Fapp%3Fdays%3D7");

    await fillAndSubmit(user, "ana@example.com", "s3cret-pass");

    await waitFor(() => expect(screen.getByTestId("location").textContent).toBe("/app?days=7"));
  });

  it("ignora un ?next= externo (redirección abierta)", async () => {
    installFakeApi({ ...dashboardRoutes(), "POST /auth/login": () => json(200, authResponse()) });
    const { user } = renderApp("/login?next=%2F%2Fevil.com");

    await fillAndSubmit(user, "ana@example.com", "s3cret-pass");

    await waitFor(() => expect(screen.getByTestId("location").textContent).toBe("/app"));
  });

  it("credenciales incorrectas (401): muestra el mensaje, vacía el password y no guarda sesión", async () => {
    installFakeApi({ ...dashboardRoutes(), "POST /auth/login": () => problem(401, { title: "Unauthorized", detail: "Email o password incorrectos." }) });
    const { user } = renderApp("/login");

    await fillAndSubmit(user, "ana@example.com", "mal-password");

    expect((await screen.findByRole("alert")).textContent).toBe("Email o password incorrectos.");
    expect((screen.getByLabelText("Password") as HTMLInputElement).value).toBe("");
    expect(screen.getByTestId("location").textContent).toBe("/login");
    expect(localStorage.getItem(SESSION_STORAGE_KEY)).toBeNull();
  });

  it("error de validación (400): lo muestra bajo el campo, lo marca inválido y le da el foco", async () => {
    installFakeApi({ ...dashboardRoutes(), "POST /auth/login": () => problem(400, { errors: { email: ["El email es obligatorio."] } }) });
    const { user } = renderApp("/login");

    await user.type(screen.getByLabelText("Password"), "algo");
    await user.click(screen.getByRole("button", { name: "Entrar" }));

    const email = screen.getByLabelText("Email");
    expect(await screen.findByText("El email es obligatorio.")).toBeTruthy();
    expect(email.getAttribute("aria-invalid")).toBe("true");
    expect(email.getAttribute("aria-describedby")).toBe("email-error");
    expect(document.activeElement).toBe(email);
    expect(screen.queryByRole("alert")).toBeNull(); // el error es del campo, no general

    await user.type(email, "a");
    expect(screen.queryByText("El email es obligatorio.")).toBeNull();
  });

  it("servidor caído: mensaje de conexión", async () => {
    installFakeApi({ ...dashboardRoutes(), "POST /auth/login": () => Promise.reject(new TypeError("Failed to fetch")) });
    const { user } = renderApp("/login");

    await fillAndSubmit(user, "ana@example.com", "s3cret-pass");

    expect((await screen.findByRole("alert")).textContent).toMatch(/No se pudo conectar con el servidor/);
  });

  it("error 500: mensaje genérico del servidor", async () => {
    installFakeApi({ ...dashboardRoutes(), "POST /auth/login": () => empty(500) });
    const { user } = renderApp("/login");

    await fillAndSubmit(user, "ana@example.com", "s3cret-pass");

    expect((await screen.findByRole("alert")).textContent).toMatch(/error 500/);
  });

  it("deshabilita el formulario mientras se envía", async () => {
    let resolve!: (response: Response) => void;
    installFakeApi({ ...dashboardRoutes(), "POST /auth/login": () => new Promise<Response>((r) => (resolve = r)) });
    const { user } = renderApp("/login");

    await fillAndSubmit(user, "ana@example.com", "s3cret-pass");

    const button = screen.getByRole("button", { name: "Entrando…" }) as HTMLButtonElement;
    expect(button.disabled).toBe(true);
    resolve(json(200, authResponse()));
    expect(await screen.findByRole("heading", { name: "Hola, Ana" })).toBeTruthy();
  });

  it("con sesión iniciada, /login redirige al dashboard", async () => {
    storeSession();
    installFakeApi({ ...dashboardRoutes(), });

    renderApp("/login");

    expect(await screen.findByRole("heading", { name: "Hola, Ana" })).toBeTruthy();
  });
});

describe("rutas protegidas", () => {
  it("sin sesión, /app manda al login conservando la ruta en ?next=", async () => {
    installFakeApi(dashboardRoutes());

    renderApp("/app");

    expect(await screen.findByRole("heading", { name: "Inicia sesión" })).toBeTruthy();
    expect(screen.getByTestId("location").textContent).toBe("/login?next=%2Fapp");
  });

  it("token guardado pero rechazado por la API (401): cierra la sesión y avisa", async () => {
    storeSession();
    installFakeApi({ ...dashboardRoutes(), "GET /auth/me": () => empty(401) });

    renderApp("/app");

    expect(await screen.findByText("Tu sesión ha expirado. Inicia sesión de nuevo.")).toBeTruthy();
    expect(screen.getByRole("heading", { name: "Inicia sesión" })).toBeTruthy();
    expect(localStorage.getItem(SESSION_STORAGE_KEY)).toBeNull();
  });

  it("cerrar sesión borra el token y vuelve al login", async () => {
    storeSession();
    installFakeApi({ ...dashboardRoutes(), });
    const { user } = renderApp("/app");

    await user.click(await screen.findByRole("button", { name: "Cerrar sesión" }));

    expect(await screen.findByRole("heading", { name: "Inicia sesión" })).toBeTruthy();
    expect(localStorage.getItem(SESSION_STORAGE_KEY)).toBeNull();
  });
});
