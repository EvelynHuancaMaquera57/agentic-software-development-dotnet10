import { screen, waitFor, within } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { MyUrl } from "../api/types";
import { bodyOf, dashboardRoutes, emptyDashboard, installFakeApi, json, problem, storeSession } from "../test/fakeApi";
import { renderApp } from "../test/renderApp";
import { formatNumber } from "../lib/format";

const google: MyUrl = {
  code: "UziDfk3",
  originalUrl: "https://www.google.com/",
  shortUrl: "http://localhost:5080/UziDfk3",
  createdAt: "2026-11-18T16:13:59+00:00",
  totalClicks: 1234,
  lastClickAt: "2026-11-18T18:33:09+00:00",
};

const unvisited: MyUrl = {
  code: "promo",
  originalUrl: "https://example.com/promo",
  shortUrl: "http://localhost:5080/promo",
  createdAt: "2026-11-10T10:00:00+00:00",
  totalClicks: 0,
  lastClickAt: null,
};

function dashboardWithData() {
  const dashboard = emptyDashboard(30);
  dashboard.totals = { urls: 2, clicks: 1234, urlsWithoutClicks: 1 };
  dashboard.period = { ...dashboard.period, clicks: 45, previousClicks: 30, changePercent: 50 };
  dashboard.daily[29] = { date: "2026-11-18", clicks: 40 };
  dashboard.daily[28] = { date: "2026-11-17", clicks: 5 };
  return dashboard;
}

function page(items: MyUrl[], total = items.length, pageNumber = 1) {
  return { items, page: pageNumber, pageSize: 10, total };
}

describe("DashboardPage", () => {
  it("requiere autenticación: sin token redirige al login (y no llama a la API)", async () => {
    const fetchMock = installFakeApi(dashboardRoutes());

    renderApp("/app");

    expect(await screen.findByRole("heading", { name: "Inicia sesión" })).toBeTruthy();
    expect(screen.getByTestId("location").textContent).toBe("/login?next=%2Fapp");
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it("muestra los totales, el gráfico y las URLs del usuario con sus estadísticas", async () => {
    storeSession();
    const fetchMock = installFakeApi({
      ...dashboardRoutes(),
      "GET /api/dashboard": () => json(200, dashboardWithData()),
      "GET /api/urls/mine": () => json(200, page([google, unvisited])),
    });

    renderApp("/app");

    const totals = await screen.findByRole("region", { name: "Totales" });
    await waitFor(() => expect(within(totals).getByText(formatNumber(1234), { selector: ".stat__value" })).toBeTruthy());
    expect(within(totals).getByText("1 sin visitas")).toBeTruthy();
    expect(within(totals).getByText("+50 % vs. periodo anterior")).toBeTruthy();
    expect(screen.getAllByTestId("chart-bar")).toHaveLength(30);
    expect(screen.getByRole("img", { name: /45 clicks en 30 días \(máximo: 40 el 18 nov/ })).toBeTruthy();

    const rows = within(screen.getByRole("table")).getAllByRole("row").slice(1);
    expect(rows).toHaveLength(2);
    expect(within(rows[0]!).getByRole("link", { name: "/UziDfk3" }).getAttribute("href")).toBe("http://localhost:5080/UziDfk3");
    expect(within(rows[0]!).getByText(formatNumber(1234))).toBeTruthy();
    expect(within(rows[1]!).getByText("Nunca")).toBeTruthy();
    expect(screen.getByText("Mostrando 1–2 de 2")).toBeTruthy();

    const requested = fetchMock.mock.calls.map(([url]) => String(url));
    expect(requested).toContain("/api/dashboard?days=30");
    expect(requested).toContain("/api/urls/mine?page=1&pageSize=10");
  });

  it("usuario sin URLs: ceros, sin comparación y mensaje para crear la primera", async () => {
    storeSession();
    installFakeApi(dashboardRoutes());

    renderApp("/app");

    expect(await screen.findByText("Aún no has creado enlaces. Acorta el primero con el formulario de arriba.")).toBeTruthy();
    const totals = screen.getByRole("region", { name: "Totales" });
    expect(within(totals).getAllByText("0", { selector: ".stat__value" })).toHaveLength(4);
    expect(within(totals).getByText("— sin comparación")).toBeTruthy();
    expect(screen.getByRole("img", { name: /Sin clicks en los últimos 30 días/ })).toBeTruthy();
  });

  it("cambiar el periodo pide el dashboard de ese periodo y lo guarda en la URL", async () => {
    storeSession();
    const fetchMock = installFakeApi({ ...dashboardRoutes(), "GET /api/dashboard": () => json(200, emptyDashboard(7)) });
    const { user } = renderApp("/app");

    await user.click(await screen.findByRole("button", { name: "7 días" }));

    await waitFor(() => expect(fetchMock.mock.calls.map(([url]) => String(url))).toContain("/api/dashboard?days=7"));
    expect(screen.getByTestId("location").textContent).toBe("/app?days=7");
    expect(screen.getByRole("button", { name: "7 días" }).getAttribute("aria-pressed")).toBe("true");
  });

  it("crear URL con alias: envía url y alias, muestra el enlace y recarga la lista", async () => {
    storeSession();
    let urls: MyUrl[] = [google];
    const created = { code: "mi-enlace", originalUrl: "https://example.com/", shortUrl: "http://localhost:5080/mi-enlace", createdAt: "2026-11-18T19:00:00+00:00" };
    const fetchMock = installFakeApi({
      ...dashboardRoutes(),
      "GET /api/urls/mine": () => json(200, page(urls)),
      "POST /api/urls": () => {
        urls = [{ ...created, totalClicks: 0, lastClickAt: null }, ...urls];
        return json(201, created);
      },
    });
    const { user } = renderApp("/app");
    await screen.findByRole("link", { name: "/UziDfk3" });

    await user.type(screen.getByLabelText("URL larga"), "example.com");
    await user.type(screen.getByLabelText("Alias (opcional)"), "  mi-enlace ");
    await user.click(screen.getByRole("button", { name: "Acortar" }));

    const success = await screen.findByRole("status");
    expect(within(success).getByRole("link", { name: "http://localhost:5080/mi-enlace" })).toBeTruthy();
    expect(await screen.findByRole("link", { name: "/mi-enlace" })).toBeTruthy();
    expect((screen.getByLabelText("URL larga") as HTMLInputElement).value).toBe("");
    expect((screen.getByLabelText("Alias (opcional)") as HTMLInputElement).value).toBe("");

    const post = fetchMock.mock.calls.find(([, init]) => init?.method === "POST");
    expect(bodyOf(post?.[1])).toEqual({ url: "example.com", alias: "mi-enlace" });
    expect(screen.getByText("localhost:5080/")).toBeTruthy(); // prefijo del alias
  });

  it("sin alias no envía el campo (la API genera un código aleatorio)", async () => {
    storeSession();
    const fetchMock = installFakeApi({
      ...dashboardRoutes(),
      "POST /api/urls": () => json(201, { code: "Ab3dE9x", originalUrl: "https://example.com/", shortUrl: "http://localhost:5080/Ab3dE9x", createdAt: "2026-11-18T19:00:00+00:00" }),
    });
    const { user } = renderApp("/app");

    await user.type(await screen.findByLabelText("URL larga"), "example.com");
    await user.click(screen.getByRole("button", { name: "Acortar" }));

    await screen.findByRole("status");
    const post = fetchMock.mock.calls.find(([, init]) => init?.method === "POST");
    expect(bodyOf(post?.[1])).toEqual({ url: "example.com" });
  });

  it("alias en uso (409): el mensaje aparece bajo el campo alias", async () => {
    storeSession();
    installFakeApi({ ...dashboardRoutes(), "POST /api/urls": () => problem(409, { title: "Conflict", detail: "Ese alias ya está en uso. Elige otro." }) });
    const { user } = renderApp("/app");

    await user.type(await screen.findByLabelText("URL larga"), "example.com");
    await user.type(screen.getByLabelText("Alias (opcional)"), "promo");
    await user.click(screen.getByRole("button", { name: "Acortar" }));

    expect(await screen.findByText("Ese alias ya está en uso. Elige otro.")).toBeTruthy();
    const alias = screen.getByLabelText("Alias (opcional)");
    expect(alias.getAttribute("aria-invalid")).toBe("true");
    expect(document.activeElement).toBe(alias);
  });

  it("URL y alias inválidos (400): cada error bajo su campo", async () => {
    storeSession();
    installFakeApi({
      ...dashboardRoutes(),
      "POST /api/urls": () => problem(400, {
        errors: {
          url: ["La URL no es válida. Usa una dirección web como www.ejemplo.com o https://www.ejemplo.com/ruta."],
          alias: ["El alias solo puede tener letras, números, guiones (-) y guiones bajos (_), entre 3 y 30 caracteres."],
        },
      }),
    });
    const { user } = renderApp("/app");

    await user.type(await screen.findByLabelText("URL larga"), "google");
    await user.type(screen.getByLabelText("Alias (opcional)"), "x");
    await user.click(screen.getByRole("button", { name: "Acortar" }));

    expect(await screen.findByText(/La URL no es válida/)).toBeTruthy();
    expect(screen.getByText(/El alias solo puede tener/)).toBeTruthy();
    expect(screen.getByLabelText("URL larga").getAttribute("aria-invalid")).toBe("true");
    expect(screen.getByLabelText("Alias (opcional)").getAttribute("aria-invalid")).toBe("true");
  });

  it("paginación: pide la página siguiente", async () => {
    storeSession();
    const fetchMock = installFakeApi({
      ...dashboardRoutes(),
      "GET /api/urls/mine": () => json(200, page([google], 15)),
    });
    const { user } = renderApp("/app");

    await user.click(await screen.findByRole("button", { name: "Siguiente →" }));

    await waitFor(() => expect(fetchMock.mock.calls.map(([url]) => String(url))).toContain("/api/urls/mine?page=2&pageSize=10"));
    expect(screen.getByTestId("location").textContent).toBe("/app?page=2");
  });

  it("si la API falla, muestra el error con opción de reintentar", async () => {
    storeSession();
    let fail = true;
    installFakeApi({
      ...dashboardRoutes(),
      "GET /api/urls/mine": () => (fail ? new Response(null, { status: 500 }) : json(200, page([google]))),
    });
    const { user } = renderApp("/app");

    expect((await screen.findByRole("alert")).textContent).toMatch(/No se pudieron cargar tus enlaces: .*error 500/);
    fail = false;
    await user.click(screen.getByRole("button", { name: "Reintentar" }));

    expect(await screen.findByRole("link", { name: "/UziDfk3" })).toBeTruthy();
  });

  it("token caducado durante el uso (401): vuelve al login con aviso", async () => {
    storeSession();
    installFakeApi({ ...dashboardRoutes(), "GET /api/dashboard": () => new Response(null, { status: 401 }) });

    renderApp("/app");

    expect(await screen.findByText("Tu sesión ha expirado. Inicia sesión de nuevo.")).toBeTruthy();
    expect(screen.getByTestId("location").textContent).toBe("/login?next=%2Fapp");
  });
});
