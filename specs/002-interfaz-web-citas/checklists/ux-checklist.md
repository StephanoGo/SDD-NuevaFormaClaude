# Checklist de Calidad UI/UX y Accesibilidad

| Campo | Valor |
| :--- | :--- |
| **Feature** | `002-interfaz-web-citas` |
| **Tipo** | Criterios de Aceptación UI/UX, Accesibilidad y Usabilidad |
| **Fecha** | 2026-10-15 |
| **Evaluado contra** | [spec.md](../spec.md) · [plan.md](../plan.md) · WCAG 2.1 AA |

> **Convención**: `[ ]` pendiente de verificación · `[x]` verificado y conforme.

---

## 1. Sistema Visual y Accesibilidad (WCAG 2.1 Nivel AA)

- [x] **UX-01** ¿El ratio de contraste entre el texto principal (`#0F172A`) y el fondo (`#FFFFFF` / `#F8FAFC`) supera el umbral de 4.5:1 (alcanza 16:1)? [Accesibilidad]
- [x] **UX-02** ¿El botón primario (`#2563EB`) con texto blanco supera el ratio de 4.5:1 exigido por WCAG AA (alcanza 4.8:1)? [Accesibilidad]
- [x] **UX-03** ¿Las píldoras de estado (`Disponible`, `Pendiente`, `Reservado`, `Atendida`) combinan color de fondo, texto de alto contraste e identificador textual claro (no dependen únicamente del color)? [Accesibilidad]
- [x] **UX-04** ¿Todos los campos de entrada y botones interactivos disponen de un indicador de foco visible (`outline` de 2px con color primario) para navegación mediante teclado? [Accesibilidad]
- [x] **UX-05** ¿La tipografía es legible, con un tamaño mínimo de cuerpo de 14px (0.875rem) y 16px para inputs en dispositivos móviles para evitar zoom automático del navegador? [Ergonomía]

---

## 2. Diseño Responsivo y Adaptabilidad

- [x] **UX-06** ¿El diseño se visualiza correctamente en dispositivos móviles desde 360px de ancho sin desbordamiento horizontal (*horizontal scroll*)? [Responsive]
- [x] **UX-07** ¿La barra de navegación por pestañas (*tabs*) se colapsa o adapta ergonómicamente en pantallas pequeñas manteniendo el acceso con una sola mano? [Mobile UX]
- [x] **UX-08** ¿Los botones y áreas interactivas táctiles cumplen con la dimensión mínima recomendada de 44x44px en móviles? [Táctil]
- [x] **UX-09** ¿La cuadrícula de bloques de agenda mantiene alturas de celda uniformes y alineación fija para evitar saltos de maquetación (*Layout Shift*) al cargar datos? [Estabilidad Visual]

---

## 3. Formularios y Validaciones de Entrada

- [x] **UX-10** ¿El campo DNI restringe la entrada a exactamente 8 dígitos numéricos y muestra validación visual inmediata (borde verde/rojo)? [Validación]
- [x] **UX-11** ¿El campo de búsqueda por apellido valida el requisito de mínimo 2 caracteres antes de habilitar la acción de búsqueda? [Validación]
- [x] **UX-12** ¿Los campos obligatorios están claramente señalizados con asterisco o texto de ayuda antes de que el usuario envíe el formulario? [Claridad]
- [x] **UX-13** ¿Los botones de envío muestran un indicador de carga (`is-loading`) y quedan deshabilitados temporalmente mientras la petición HTTP está en vuelo? [Prevención de errores]
- [x] **UX-14** ¿El selector de horas (`startTime` y `endTime`) valida que el inicio sea estrictamente menor al fin antes de enviar el formulario? [Consistencia]

---

## 4. Gestión de Estados y Feedback al Usuario

- [x] **UX-15** ¿Las notificaciones tipo Toast aparecen en una zona no invasiva (esquina superior derecha) y desaparecen automáticamente tras 4 segundos? [Feedback]
- [x] **UX-16** ¿Los mensajes de error de negocio muestran el detalle en español comprensible junto con el código técnico (ejemplo: `SLOT_OVERLAP`, `PATIENT_DNI_DUPLICATE`)? [Trazabilidad]
- [x] **UX-17** ¿Existen estados vacíos (*Empty States*) ilustrativos cuando un médico no tiene bloques en una fecha o cuando una búsqueda de paciente no arroja coincidencias? [Usabilidad]
- [x] **UX-18** ¿Al consultar una cita en estado `Atendida`, los campos de diagnóstico y receta se bloquean en modo lectura y el botón de acción se oculta o deshabilita? [Inmutabilidad]
