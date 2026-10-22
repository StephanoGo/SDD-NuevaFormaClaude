# Frontend web

La interfaz estática del sistema vive en `wwwroot/` (HTML, CSS y JavaScript, sin proyecto npm ni build independiente). `CitasMedicas.Api` sirve estos archivos en desarrollo mediante su ruta física y los copia a su carpeta de publicación al compilar/publicar.

La API y sus endpoints permanecen en `src/CitasMedicas.Api`; este directorio contiene únicamente los recursos de presentación.
