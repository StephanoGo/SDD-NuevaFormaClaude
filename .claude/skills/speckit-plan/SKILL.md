---
name: speckit-plan
description: Producir un diseño técnico conciso, modelo de datos y contratos para una feature especificada.
disable-model-invocation: true
metadata:
  opencode/slash: "true"
---

Planificar la feature en $ARGUMENTS (o la feature actual si se omite).

1. Leer su `spec.md`, la constitución, las specs de los módulos afectados, el código actual y solo los contratos compartidos relevantes.
2. Detenerse y reportar bloqueos si quedan aclaraciones de negocio sin resolver. No resolverlas por suposición.
3. Crear `plan.md` y `data-model.md` concisos. Explicar la arquitectura elegida, los módulos afectados, la propiedad de los datos, las invariantes, el impacto en persistencia, el comportamiento de errores y el enfoque de verificación. Contrastar la tecnología propuesta con `.specify/memory/constitution.md`.
4. Crear `contracts/` con contratos precisos HTTP/JSON y de repositorio para las interfaces que la feature cambia. Si ninguna cambia, añadir una nota breve que referencie el contrato compartido y afirme que no se introduce una interfaz nueva. Reutilizar los contratos compartidos por referencia en lugar de copiarlos.
5. Mantener fuera las alternativas descartadas y los detalles de implementación, salvo que expliquen una decisión relevante. No cambiar `src/` ni `tests/`.
6. Reportar los artefactos creados, las decisiones aún abiertas y el siguiente comando.
