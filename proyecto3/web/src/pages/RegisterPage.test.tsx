import { screen, waitFor } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { SESSION_STORAGE_KEY } from "../auth/session";
import { authResponse, bodyOf, dashboardRoutes, installFakeApi, json, problem } from "../test/fakeApi";
import { renderApp } from "../test/renderApp";

async function fillAndSubmit(user: ReturnType<typeof renderApp>["user"], name: string, email: string, password: string) {
  await user.type(screen.getByLabelText("Nombre"), name);
  await user.type(screen.getByLabelText("Email"), email);
  await user.type(screen.getByLabelText("Password"), password);
  await user.click(screen.getByRole("button", { name: "Crear cuenta" }));
}

describe("RegisterPage", () => {
  it("registro correcto: llama a POST /auth/register, guarda el token y va al dashboard", async () => {
    const fetchMock = installFakeApi({ ...dashboardRoutes(), "POST /auth/register": () => json(201, authResponse({ name: "Ana" })) });
    const { user } = renderApp("/register");

    await fillAndSubmit(user, "Ana", "ana@example.com", "s3cret-pass");

    expect(await screen.findByRole("heading", { name: "Hola, Ana" })).toBeTruthy();
    expect(screen.getByTestId("location").textContent).toBe("/app");
    expect(bodyOf(fetchMock.mock.calls[0]?.[1])).toEqual({ name: "Ana", email: "ana@example.com", password: "s3cret-pass" });
    expect(JSON.parse(localStorage.getItem(SESSION_STORAGE_KEY)!).token).toBe("token-de-prueba");
  });

  it("password corto (400): el error del servidor aparece bajo el campo password", async () => {
    installFakeApi({
      ...dashboardRoutes(),
      "POST /auth/register": () => problem(400, { errors: { password: ["El password debe tener al menos 8 caracteres."] } }),
    });
    const { user } = renderApp("/register");

    await fillAndSubmit(user, "Ana", "ana@example.com", "1234");

    const password = screen.getByLabelText("Password");
    expect(await screen.findByText("El password debe tener al menos 8 caracteres.")).toBeTruthy();
    expect(password.getAttribute("aria-invalid")).toBe("true");
    expect(password.getAttribute("aria-describedby")).toBe("password-error password-hint");
    expect(localStorage.getItem(SESSION_STORAGE_KEY)).toBeNull();
  });

  it("email inválido (400): el error aparece bajo el campo email", async () => {
    installFakeApi({ ...dashboardRoutes(), "POST /auth/register": () => problem(400, { errors: { email: ["El email no es válido."] } }) });
    const { user } = renderApp("/register");

    await fillAndSubmit(user, "Ana", "no-es-email", "s3cret-pass");

    expect(await screen.findByText("El email no es válido.")).toBeTruthy();
    expect(screen.getByLabelText("Email").getAttribute("aria-invalid")).toBe("true");
  });

  it("email ya registrado (409): muestra el mensaje y ofrece ir al login con el email relleno", async () => {
    installFakeApi({ ...dashboardRoutes(), "POST /auth/register": () => problem(409, { title: "Conflict", detail: "Ya existe una cuenta con ese email." }) });
    const { user } = renderApp("/register");

    await fillAndSubmit(user, "Ana", "ana@example.com", "s3cret-pass");

    const alert = await screen.findByRole("alert");
    expect(alert.textContent).toContain("Ya existe una cuenta con ese email.");
    await user.click(screen.getByRole("link", { name: "¿Quieres iniciar sesión con ese email?" }));

    expect(await screen.findByRole("heading", { name: "Inicia sesión" })).toBeTruthy();
    await waitFor(() => expect((screen.getByLabelText("Email") as HTMLInputElement).value).toBe("ana@example.com"));
    expect(document.activeElement).toBe(screen.getByLabelText("Password"));
  });

  it("los enlaces entre login y registro conservan ?next=", async () => {
    installFakeApi(dashboardRoutes());
    const { user } = renderApp("/login?next=%2Fapp%3Fdays%3D7");

    await user.click(screen.getByRole("link", { name: "Regístrate" }));

    expect(screen.getByTestId("location").textContent).toBe("/register?next=%2Fapp%3Fdays%3D7");
  });
});
