import type { ReactNode } from "react";
import { formatChange } from "../lib/format";

interface StatCardProps {
  label: string;
  value: string;
  /** Línea secundaria bajo la cifra. */
  detail?: ReactNode;
}

export function StatCard({ label, value, detail }: StatCardProps) {
  return (
    <div className="stat">
      <p className="stat__label">{label}</p>
      <p className="stat__value">{value}</p>
      {detail !== undefined && <div className="stat__detail">{detail}</div>}
    </div>
  );
}

/** Variación frente al periodo anterior. null = no hay base para comparar (periodo anterior sin clicks). */
export function ChangeBadge({ percent }: { percent: number | null }) {
  if (percent === null) {
    return (
      <span className="badge badge--flat" title="El periodo anterior no tuvo clicks, no hay con qué comparar">
        — sin comparación
      </span>
    );
  }
  const tone = percent > 0 ? "up" : percent < 0 ? "down" : "flat";
  return <span className={`badge badge--${tone}`}>{formatChange(percent)} vs. periodo anterior</span>;
}
