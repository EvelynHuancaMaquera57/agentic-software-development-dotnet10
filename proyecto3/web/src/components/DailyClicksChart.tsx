import type { DashboardResponse } from "../api/types";
import { formatDay, formatNumber } from "../lib/format";

const HEIGHT = 100; // unidades del viewBox; el SVG se estira al ancho disponible
const GAP = 0.2; // fracción del hueco entre barras

interface DailyClicksChartProps {
  daily: DashboardResponse["daily"];
}

/** Barras SVG de clicks por día (días naturales UTC, como los agrupa la API). Sin librerías. */
export function DailyClicksChart({ daily }: DailyClicksChartProps) {
  const max = Math.max(1, ...daily.map((day) => day.clicks));
  const total = daily.reduce((sum, day) => sum + day.clicks, 0);
  const peak = daily.reduce<(typeof daily)[number] | undefined>((best, day) => (!best || day.clicks > best.clicks ? day : best), undefined);
  const first = daily[0];
  const middle = daily[Math.floor(daily.length / 2)];
  const last = daily[daily.length - 1];

  const summary =
    total === 0 || !peak
      ? `Sin clicks en los últimos ${daily.length} días.`
      : `${formatNumber(total)} clicks en ${daily.length} días (máximo: ${formatNumber(peak.clicks)} el ${formatDay(peak.date)}).`;

  return (
    <figure className="chart">
      <div className="chart__plot">
        <span className="chart__max" aria-hidden="true">
          {formatNumber(max)}
        </span>
        <svg
          className="chart__svg"
          viewBox={`0 0 ${daily.length} ${HEIGHT}`}
          preserveAspectRatio="none"
          role="img"
          aria-label={`Clicks por día. ${summary}`}
        >
          {daily.map((day, index) => {
            const height = day.clicks === 0 ? 1 : Math.max(3, (day.clicks / max) * HEIGHT);
            return (
              <rect
                key={day.date}
                data-testid="chart-bar"
                className={day.clicks === 0 ? "chart__bar chart__bar--empty" : "chart__bar"}
                x={index + GAP / 2}
                y={HEIGHT - height}
                width={1 - GAP}
                height={height}
              >
                <title>{`${formatDay(day.date)}: ${formatNumber(day.clicks)} ${day.clicks === 1 ? "click" : "clicks"}`}</title>
              </rect>
            );
          })}
        </svg>
      </div>
      <div className="chart__axis" aria-hidden="true">
        <span>{first && formatDay(first.date)}</span>
        <span>{middle && middle !== first && middle !== last && formatDay(middle.date)}</span>
        <span>{last && formatDay(last.date)}</span>
      </div>
      <figcaption className="chart__caption">{summary} Días en hora UTC.</figcaption>
    </figure>
  );
}
