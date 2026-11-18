-- Una fila por visita a un código corto (cada redirect).
-- clicked_at usa siempre el formato de SqliteTime (ISO 8601 en UTC, "+00:00"), así se compara como texto.
-- referrer_host guarda solo el host del header Referer (nunca la URL completa ni la IP).
CREATE TABLE IF NOT EXISTS clicks (
    id            INTEGER PRIMARY KEY AUTOINCREMENT,
    url_id        INTEGER NOT NULL REFERENCES urls(id) ON DELETE CASCADE,
    clicked_at    TEXT    NOT NULL,
    referrer_host TEXT
);

-- Cubre las consultas del dashboard: búsqueda por URL + rango de fechas sin tocar la tabla.
CREATE INDEX IF NOT EXISTS ix_clicks_url_time ON clicks(url_id, clicked_at);
