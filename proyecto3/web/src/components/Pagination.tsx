import { formatNumber } from "../lib/format";

interface PaginationProps {
  page: number;
  pageSize: number;
  total: number;
  onChange: (page: number) => void;
}

export function Pagination({ page, pageSize, total, onChange }: PaginationProps) {
  const pages = Math.max(1, Math.ceil(total / pageSize));
  const from = total === 0 ? 0 : (page - 1) * pageSize + 1;
  const to = Math.min(page * pageSize, total);

  return (
    <nav className="pagination" aria-label="Paginación de enlaces">
      <span className="muted">
        Mostrando {formatNumber(from)}–{formatNumber(to)} de {formatNumber(total)}
      </span>
      {pages > 1 && (
        <span className="pagination__buttons">
          <button type="button" className="button button--small" onClick={() => onChange(page - 1)} disabled={page <= 1}>
            ← Anterior
          </button>
          <span aria-current="page">
            {page} / {pages}
          </span>
          <button type="button" className="button button--small" onClick={() => onChange(page + 1)} disabled={page >= pages}>
            Siguiente →
          </button>
        </span>
      )}
    </nav>
  );
}
