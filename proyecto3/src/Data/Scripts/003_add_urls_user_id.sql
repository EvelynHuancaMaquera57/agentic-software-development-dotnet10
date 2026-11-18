-- Dueño de cada URL. NULL solo en URLs creadas antes de que existiera la autenticación:
-- esas no pertenecen a nadie y por tanto nadie puede borrarlas.
ALTER TABLE urls ADD COLUMN user_id INTEGER REFERENCES users(id);

CREATE INDEX IF NOT EXISTS ix_urls_user_id ON urls(user_id);
