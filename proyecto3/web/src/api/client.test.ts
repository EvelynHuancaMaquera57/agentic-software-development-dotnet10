import { afterEach, describe, expect, it, vi } from "vitest";
import { empty, installFakeApi, json, problem, storeSession } from "../test/fakeApi";
import { request, setUnauthorizedHandler } from "./client";
import { ApiError, NETWORK_ERROR_MESSAGE, errorMessage } from "./errors";

afterEach(() => setUnauthorizedHandler(null));

function headersOf(init: RequestInit | undefined): Headers {
  return new Headers(init?.headers);
}

describe("request", () => {
  it("envía JSON y el token Bearer guardado", async () => {
    storeSession();
    const fetchMock = installFakeApi({ "POST /api/urls": () => json(201, { code: "abc1234" }) });

    const result = await request<{ code: string }>("POST", "/api/urls", { body: { url: "www.google.com" } });

    expect(result).toEqual({ code: "abc1234" });
    const init = fetchMock.mock.calls[0]?.[1];
    expect(headersOf(init).get("Authorization")).toBe("Bearer token-de-prueba");
    expect(headersOf(init).get("Content-Type")).toBe("application/json");
    expect(init?.body).toBe('{"url":"www.google.com"}');
  });

  it("con auth: false no envía el token", async () => {
    storeSession();
    const fetchMock = installFakeApi({ "POST /auth/login": () => json(200, {}) });

    await request("POST", "/auth/login", { body: {}, auth: false });

    expect(headersOf(fetchMock.mock.calls[0]?.[1]).has("Authorization")).toBe(false);
  });

  it("204 devuelve undefined", async () => {
    installFakeApi({ "DELETE /api/urls/abc": () => empty(204) });

    await expect(request("DELETE", "/api/urls/abc")).resolves.toBeUndefined();
  });

  it("400 de validación → ApiError con el primer error de cada campo", async () => {
    installFakeApi({
      "POST /auth/register": () => problem(400, {
        title: "One or more validation errors occurred.",
        errors: { password: ["El password debe tener al menos 8 caracteres.", "otro"] },
      }),
    });

    const error = await request("POST", "/auth/register", { body: {}, auth: false }).catch((e: unknown) => e);

    expect(error).toBeInstanceOf(ApiError);
    expect(error).toMatchObject({ status: 400, fieldErrors: { password: "El password debe tener al menos 8 caracteres." } });
  });

  it("problem+json con detail → ApiError.detail", async () => {
    installFakeApi({ "POST /auth/register": () => problem(409, { detail: "Ya existe una cuenta con ese email." }) });

    const error = await request("POST", "/auth/register", { auth: false }).catch((e: unknown) => e);

    expect(error).toMatchObject({ status: 409, detail: "Ya existe una cuenta con ese email.", fieldErrors: {} });
  });

  it("401 sin cuerpo con token → avisa para cerrar la sesión", async () => {
    storeSession();
    const onUnauthorized = vi.fn();
    setUnauthorizedHandler(onUnauthorized);
    installFakeApi({ "GET /auth/me": () => empty(401) });

    const error = await request("GET", "/auth/me").catch((e: unknown) => e);

    expect(error).toMatchObject({ status: 401, detail: null });
    expect(onUnauthorized).toHaveBeenCalledOnce();
  });

  it("401 del login (credenciales) NO cierra la sesión", async () => {
    storeSession();
    const onUnauthorized = vi.fn();
    setUnauthorizedHandler(onUnauthorized);
    installFakeApi({ "POST /auth/login": () => problem(401, { detail: "Email o password incorrectos." }) });

    await request("POST", "/auth/login", { auth: false }).catch(() => undefined);

    expect(onUnauthorized).not.toHaveBeenCalled();
  });

  it("fallo de red → ApiError con status 0", async () => {
    vi.stubGlobal("fetch", vi.fn().mockRejectedValue(new TypeError("Failed to fetch")));

    const error = await request("GET", "/health").catch((e: unknown) => e);

    expect(error).toMatchObject({ status: 0, detail: NETWORK_ERROR_MESSAGE });
  });
});

describe("errorMessage", () => {
  it.each([
    [new ApiError(0, NETWORK_ERROR_MESSAGE), NETWORK_ERROR_MESSAGE],
    [new ApiError(500), "El servidor tuvo un problema (error 500). Inténtalo de nuevo en unos segundos."],
    [new ApiError(401, "Email o password incorrectos."), "Email o password incorrectos."],
    [new ApiError(400), "Los datos enviados no son válidos. Revisa el formulario."],
    [new Error("x"), "Ha ocurrido un error inesperado. Inténtalo de nuevo."],
  ])("%#: %s", (error, expected) => {
    expect(errorMessage(error)).toBe(expected);
  });
});
