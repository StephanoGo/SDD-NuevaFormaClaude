# Constitución del Proyecto

1. **Simplicidad del Stack**: Prohibido agregar paquetes NuGet o librerías externas no declaradas en el plan.
2. **Spec Manda**: Toda implementación debe responder directamente a un ID de requisito en `spec.md`.
3. **Separación de Responsabilidades**: Las reglas clínicas y de agenda residen en la capa de Dominio/Servicio; jamás en controladores o vistas.
4. **Persistencia Controlada**: Todo acceso a datos se realiza a través de interfaces de repositorio con transacciones atómicas.
5. **Verificación Automatizada**: Ninguna tarea se marca como completada sin pasar `dotnet test` y `dotnet build` con 0 errores.