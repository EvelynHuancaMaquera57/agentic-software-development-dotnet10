import { useSearchParams } from "react-router";
import { DASHBOARD_PERIODS, DEFAULT_DASHBOARD_DAYS, getDashboard, isDashboardDays } from "../api/dashboard";
import { errorMessage } from "../api/errors";
import type { DashboardDays } from "../api/types";
import { MY_URLS_PAGE_SIZE, listMyUrls } from "../api/urls";
import { useAuth } from "../auth/useAuth";
import { Alert } from "../components/Alert";
import { AppLayout } from "../components/AppLayout";
import { CreateUrlForm } from "../components/CreateUrlForm";
import { DailyClicksChart } from "../components/DailyClicksChart";
import { Pagination } from "../components/Pagination";
import { ChangeBadge, StatCard } from "../components/StatCard";
import { UrlsTable } from "../components/UrlsTable";
import { useApi } from "../hooks/useApi";
import { formatNumber } from "../lib/format";

function parseDays(value: string | null): DashboardDays {
  const days = Number(value);
  return isDashboardDays(days) ? days : DEFAULT_DASHBOARD_DAYS;
}

function parsePage(value: string | null): number {
  const page = Number(value);
  return Number.isInteger(page) && page >= 1 ? page : 1;
}

/** "http://localhost:5080/abc" → "localhost:5080/" (prefijo visual del alias). */
function shortBaseOf(shortUrl: string | undefined): string | undefined {
  if (!shortUrl) {
    return undefined;
  }
  try {
    return `${new URL(shortUrl).host}/`;
  } catch {
    return undefined;
  }
}

/**
 * Vista principal: resumen (GET /api/dashboard), crear URL (POST /api/urls) y
 * la lista paginada de URLs del usuario con sus clicks (GET /api/urls/mine).
 * El periodo y la página viven en la URL (?days=7&page=2): sobreviven a recargas y se pueden compartir.
 */
export function DashboardPage() {
  const { user } = useAuth();
  const [searchParams, setSearchParams] = useSearchParams();
  const days = parseDays(searchParams.get("days"));
  const page = parsePage(searchParams.get("page"));

  const dashboard = useApi((signal) => getDashboard(days, signal), [days]);
  const mine = useApi((signal) => listMyUrls(page, signal), [page]);

  function updateParams(changes: Record<string, string | null>) {
    setSearchParams((current) => {
      const next = new URLSearchParams(current);
      for (const [key, value] of Object.entries(changes)) {
        if (value === null) {
          next.delete(key);
        } else {
          next.set(key, value);
        }
      }
      return next;
    });
  }

  function handleCreated() {
    updateParams({ page: null }); // la nueva URL aparece la primera
    mine.reload();
    dashboard.reload();
  }

  const stats = dashboard.data;
  const shortBase = shortBaseOf(mine.data?.items[0]?.shortUrl ?? stats?.urls[0]?.shortUrl);

  return (
    <AppLayout>
      <div className="page__header">
        <div>
          <h1>Hola, {user?.name}</h1>
          <p className="muted">Resumen de tus enlaces y sus visitas.</p>
        </div>
        <div className="segmented" role="group" aria-label="Periodo">
          {DASHBOARD_PERIODS.map((option) => (
            <button
              key={option}
              type="button"
              className="segmented__option"
              aria-pressed={option === days}
              onClick={() => updateParams({ days: option === DEFAULT_DASHBOARD_DAYS ? null : String(option) })}
            >
              {option} días
            </button>
          ))}
        </div>
      </div>

      {dashboard.error !== null ? (
        <Alert tone="error">
          No se pudo cargar el resumen: {errorMessage(dashboard.error)}{" "}
          <button type="button" className="link-button" onClick={dashboard.reload}>
            Reintentar
          </button>
        </Alert>
      ) : (
        <>
          <section className="stats" aria-label="Totales" aria-busy={dashboard.loading}>
            <StatCard
              label="Enlaces"
              value={stats ? formatNumber(stats.totals.urls) : "…"}
              detail={stats && `${formatNumber(stats.totals.urlsWithoutClicks)} sin visitas`}
            />
            <StatCard label="Clicks totales" value={stats ? formatNumber(stats.totals.clicks) : "…"} detail="Desde siempre" />
            <StatCard
              label={`Clicks en ${days} días`}
              value={stats ? formatNumber(stats.period.clicks) : "…"}
              detail={stats && <ChangeBadge percent={stats.period.changePercent} />}
            />
            <StatCard
              label={`Periodo anterior`}
              value={stats ? formatNumber(stats.period.previousClicks) : "…"}
              detail={`Los ${days} días previos`}
            />
          </section>

          <section className="card" aria-labelledby="chart-title">
            <h2 id="chart-title">Clicks por día</h2>
            {stats ? <DailyClicksChart daily={stats.daily} /> : <p className="muted">Cargando…</p>}
          </section>
        </>
      )}

      <CreateUrlForm shortBase={shortBase} onCreated={handleCreated} />

      <section className="card" aria-labelledby="urls-title" aria-busy={mine.loading}>
        <h2 id="urls-title">Tus enlaces</h2>
        {mine.error !== null ? (
          <Alert tone="error">
            No se pudieron cargar tus enlaces: {errorMessage(mine.error)}{" "}
            <button type="button" className="link-button" onClick={mine.reload}>
              Reintentar
            </button>
          </Alert>
        ) : !mine.data ? (
          <p className="muted">Cargando…</p>
        ) : mine.data.total === 0 ? (
          <p className="empty">Aún no has creado enlaces. Acorta el primero con el formulario de arriba.</p>
        ) : (
          <>
            <UrlsTable urls={mine.data.items} />
            <Pagination
              page={mine.data.page}
              pageSize={MY_URLS_PAGE_SIZE}
              total={mine.data.total}
              onChange={(next) => updateParams({ page: next === 1 ? null : String(next) })}
            />
          </>
        )}
      </section>
    </AppLayout>
  );
}
