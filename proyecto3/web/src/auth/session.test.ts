import { describe, expect, it } from "vitest";
import { authResponse } from "../test/fakeApi";
import { safeNext } from "./redirect";
import { SESSION_STORAGE_KEY, clearSession, readSession, saveSession } from "./session";

describe("session", () => {
  it("guarda y lee la sesión", () => {
    const auth = authResponse();

    saveSession(auth);

    expect(readSession()).toEqual({ token: auth.token, expiresAt: auth.expiresAt, user: auth.user });
  });

  it("descarta (y borra) una sesión caducada", () => {
    saveSession({ ...authResponse(), expiresAt: "2026-01-01T00:00:00+00:00" });

    expect(readSession(Date.parse("2026-01-01T00:00:01Z"))).toBeNull();
    expect(localStorage.getItem(SESSION_STORAGE_KEY)).toBeNull();
  });

  it.each(["{no es json", '{"token":"x"}', '{"token":1,"expiresAt":"2030-01-01","user":{}}', '{"token":"x","expiresAt":"mañana","user":{}}'])(
    "ignora datos corruptos: %s",
    (raw) => {
      localStorage.setItem(SESSION_STORAGE_KEY, raw);

      expect(readSession()).toBeNull();
    },
  );

  it("clearSession la elimina", () => {
    saveSession(authResponse());

    clearSession();

    expect(readSession()).toBeNull();
  });
});

describe("safeNext", () => {
  it.each([
    ["/app/urls?page=2", "/app/urls?page=2"],
    [null, "/app"],
    ["", "/app"],
    ["//evil.com", "/app"],
    ["/\\evil.com", "/app"],
    ["https://evil.com", "/app"],
    ["app", "/app"],
  ])("%s → %s", (next, expected) => {
    expect(safeNext(next)).toBe(expected);
  });
});
