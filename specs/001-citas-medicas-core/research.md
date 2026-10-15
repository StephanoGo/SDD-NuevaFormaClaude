# Decisiones técnicas vigentes del Core

Resumen de decisiones verificadas en la implementación. El código actual es la fuente de verdad si este resumen queda desactualizado.

- API con ASP.NET Core Minimal APIs; reglas en Domain/Application.
- Arquitectura por capas: Domain, Application, Infrastructure y Api.
- Persistencia actual en memoria; los datos se pierden al reiniciar.
- Operaciones críticas de reserva y registro de atención son atómicas en el repositorio.
- Errores de negocio usan `Result` y códigos estables; la API los traduce a HTTP ProblemDetails.
- La clínica usa `TimeProvider` y zona `America/Lima`.
- Las pruebas viven en `tests/CitasMedicas.Tests`; producción no incorpora paquetes externos sin justificarlo en una spec.

Las alternativas y análisis de la entrega original están en el historial de Git. Una decisión técnica nueva se registra en el plan breve del cambio que la necesite.
