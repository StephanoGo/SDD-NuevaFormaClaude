---
name: speckit-checklist
description: Auditar la calidad de los requisitos y añadir comprobaciones de dominio o UX focalizadas para una feature.
disable-model-invocation: true
metadata:
  opencode/slash: "true"
---

Crear checklists de calidad para la feature en $ARGUMENTS (o la feature actual si se omite).

Leer la spec de la feature, el plan, el modelo de datos, los contratos, las specs de los módulos afectados y el código relevante. No editar código ni inferir decisiones de negocio. La descripción del proyecto especifica un checklist formal de 16 comprobaciones, pero no proporcionó el texto original. Las 16 comprobaciones siguientes son una línea base propuesta derivada de los objetivos aportados (requisitos atómicos, verificables e inequívocos); identificarlas como propuestas en `requirements.md` hasta que el propietario del proyecto las confirme. No convertir estas comprobaciones de proceso en comportamiento de producto. Marcar cada una como pasa/falla/necesita aclaración y enlazarla con los IDs de requisito:

1. Todo requisito tiene un ID único y estable.
2. Cada requisito describe un único comportamiento atómico.
3. El actor o componente responsable es identificable.
4. El disparador o la condición es explícito.
5. El resultado esperado es explícito.
6. El enunciado tiene una sola interpretación inequívoca.
7. Los términos vagos tienen definiciones medibles o se eliminan.
8. Los términos de dominio son consistentes entre la spec y la línea base del módulo.
9. El alcance y el fuera de alcance son explícitos.
10. Se indican las precondiciones y el estado relevante.
11. El comportamiento de éxito tiene criterios de aceptación observables.
12. Se cubren la validación relevante y el comportamiento de fallo.
13. La propiedad de los datos y los límites de los módulos afectados están claros.
14. Las transiciones de estado e invariantes son consistentes con el modelo.
15. Todo requisito se mapea a al menos un escenario Dado/Cuando/Entonces.
16. Los requisitos funcionales no prescriben detalles de implementación innecesarios.

Añadir un checklist de dominio conciso y separado para invariantes clínicas/de negocio, o un checklist UX para interacción/accesibilidad cuando sea relevante. Afirmar solo reglas de dominio presentes en specs aprobadas; marcar las reglas inciertas como preguntas, no como fallos de implementación. Reportar hallazgos críticos y el siguiente comando.
