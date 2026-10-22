# Contrato: Gestión web de catálogos

Se reutilizan íntegramente rutas y payloads en [`003-persistencia-sql-server`](../../003-persistencia-sql-server/contracts/persistence-and-catalog-contracts.md): listados `GET /api/specialties`, `GET /api/doctors` con `includeInactive`; `POST`/`PUT` para altas y ediciones; `POST /{id}/deactivation` para desactivación. Sin contrato HTTP nuevo, DELETE no disponible.
