CREATE TABLE IF NOT EXISTS users (
    id            INTEGER PRIMARY KEY AUTOINCREMENT,
    email         TEXT    NOT NULL UNIQUE COLLATE NOCASE, -- único sin distinguir mayúsculas
    password_hash TEXT    NOT NULL,                       -- salida de PasswordHasher<T>, nunca el password
    name          TEXT    NOT NULL,
    created_at    TEXT    NOT NULL                        -- ISO 8601 en UTC
);
