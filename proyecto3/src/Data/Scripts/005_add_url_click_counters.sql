-- Contadores desnormalizados que se actualizan en la misma transacción que cada click.
-- Evitan recorrer todo el historial de clicks para los totales del dashboard.
ALTER TABLE urls ADD COLUMN total_clicks INTEGER NOT NULL DEFAULT 0;
ALTER TABLE urls ADD COLUMN last_click_at TEXT;

-- Por si ya hubiera clicks registrados antes de esta migración.
UPDATE urls SET
    total_clicks  = (SELECT COUNT(*)          FROM clicks WHERE clicks.url_id = urls.id),
    last_click_at = (SELECT MAX(clicked_at)   FROM clicks WHERE clicks.url_id = urls.id);
