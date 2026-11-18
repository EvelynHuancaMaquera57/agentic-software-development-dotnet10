import type { MyUrl } from "../api/types";
import { formatDateTime, formatNumber, formatRelative } from "../lib/format";
import { CopyButton } from "./CopyButton";

/** Solo se enlazan destinos http/https (la API ya lo garantiza; esto es defensa en profundidad). */
function isWebUrl(value: string): boolean {
  return /^https?:\/\//i.test(value);
}

export function UrlsTable({ urls }: { urls: MyUrl[] }) {
  return (
    <div className="table-wrap">
      <table className="urls">
        <thead>
          <tr>
            <th scope="col">Enlace corto</th>
            <th scope="col">Destino</th>
            <th scope="col" className="num">
              Clicks
            </th>
            <th scope="col">Último click</th>
            <th scope="col">Creado</th>
          </tr>
        </thead>
        <tbody>
          {urls.map((url) => (
            <tr key={url.code}>
              <td data-label="Enlace corto">
                <div className="urls__short">
                  <a href={url.shortUrl} target="_blank" rel="noopener noreferrer" className="urls__code">
                    /{url.code}
                  </a>
                  <CopyButton text={url.shortUrl} />
                </div>
              </td>
              <td data-label="Destino" className="urls__target">
                {isWebUrl(url.originalUrl) ? (
                  <a href={url.originalUrl} target="_blank" rel="noopener noreferrer" title={url.originalUrl}>
                    {url.originalUrl}
                  </a>
                ) : (
                  <span title={url.originalUrl}>{url.originalUrl}</span>
                )}
              </td>
              <td data-label="Clicks" className="num">
                {formatNumber(url.totalClicks)}
              </td>
              <td data-label="Último click">
                {url.lastClickAt ? (
                  <time dateTime={url.lastClickAt} title={formatDateTime(url.lastClickAt)}>
                    {formatRelative(url.lastClickAt)}
                  </time>
                ) : (
                  <span className="muted">Nunca</span>
                )}
              </td>
              <td data-label="Creado">
                <time dateTime={url.createdAt}>{formatDateTime(url.createdAt)}</time>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
