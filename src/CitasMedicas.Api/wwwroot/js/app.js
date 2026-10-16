/**
 * App Orchestrator - Medical Appointments Management SPA
 * Manages tabs, agenda, patient registration & booking, and clinical attention.
 */

// --- Application State ---
const state = {
  selectedPatient: null,
  activeTab: 'view-agenda',
  doctors: [],
  currentDoctorId: null,
  currentDate: getTodayDateString(),
  currentAppointment: null
};

// --- Helper Functions ---
function getTodayDateString() {
  const now = new Date();
  const year = now.getFullYear();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function escapeHtml(str) {
  if (!str) return '';
  return String(str)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}

function patientFullName(patient) {
  if (!patient) return '';
  return `${patient.firstName || ''} ${patient.lastName || ''}`.trim();
}

async function withButtonLoading(button, action) {
  if (!button) return await action();
  button.disabled = true;
  button.classList.add('is-loading');
  try {
    return await action();
  } finally {
    button.disabled = false;
    button.classList.remove('is-loading');
  }
}

// --- Initialization ---
document.addEventListener('DOMContentLoaded', () => {
  initTabs();
  initAgendaView();
  initBookingView();
  initAttentionView();
  initSlotModal();
  const initialTarget = targetForLocation(location.pathname) || 'view-agenda';
  activateTab(initialTarget, { updateUrl: location.pathname !== '/' });
  refreshDoctorCatalog();
});

// ============================================================================
// 0. Enrutado de la SPA (rutas de cliente)
// ============================================================================
const ROUTES = {
  '/agenda': 'view-agenda',
  '/citas': 'view-reservas',
  '/reservas': 'view-reservas',
  '/pacientes': 'view-reservas',
  '/atencion': 'view-atencion'
};
const DEFAULT_ROUTE = '/agenda';

function routeForTarget(targetId) {
  const entry = Object.entries(ROUTES).find(([, value]) => value === targetId);
  return entry ? entry[0] : DEFAULT_ROUTE;
}

function targetForLocation(pathname) {
  const normalized = (pathname || '/').replace(/\/+$/, '') || '/';
  return ROUTES[normalized] || null;
}

// ============================================================================
// 1. Tab Navigation
// ============================================================================
function activateTab(targetId, options = {}) {
  const tab = document.querySelector(`.nav-tab[data-target="${targetId}"]`);
  const section = document.getElementById(targetId);
  if (!tab || !section) return;

  document.querySelectorAll('.nav-tab').forEach(t => {
    t.classList.remove('active');
    t.setAttribute('aria-selected', 'false');
  });
  document.querySelectorAll('.view-section').forEach(s => s.classList.remove('active'));

  tab.classList.add('active');
  tab.setAttribute('aria-selected', 'true');
  section.classList.add('active');
  state.activeTab = targetId;

  if (options.updateUrl === true) {
    const path = routeForTarget(targetId);
    if (location.pathname !== path) {
      history.pushState({ targetId }, '', path);
    }
  }

  // Auto-refresh when entering tabs
  if (targetId === 'view-agenda') {
    loadAgendaSlots();
  } else if (targetId === 'view-reservas') {
    loadBookingSlots();
  }
}

function initTabs() {
  document.querySelectorAll('.nav-tab').forEach(tab => {
    tab.addEventListener('click', () => {
      const targetId = tab.getAttribute('data-target');
      if (targetId) activateTab(targetId, { updateUrl: true });
    });
  });

  window.addEventListener('popstate', () => {
    const targetId = targetForLocation(location.pathname) || 'view-agenda';
    activateTab(targetId, { updateUrl: false });
  });
}

// ============================================================================
// 2. Vista 1: Agenda Médica
// ============================================================================
function initAgendaView() {
  const doctorSelect = document.getElementById('agenda-doctor-select');
  const dateInput = document.getElementById('agenda-date-input');
  const refreshBtn = document.getElementById('btn-refresh-agenda');

  dateInput.value = state.currentDate;

  doctorSelect.addEventListener('change', (e) => {
    state.currentDoctorId = parseInt(e.target.value, 10);
    updateDoctorBanner();
    loadAgendaSlots();
  });

  dateInput.addEventListener('change', (e) => {
    state.currentDate = e.target.value;
    loadAgendaSlots();
  });

  refreshBtn.addEventListener('click', () => {
    loadAgendaSlots();
  });

}

async function refreshDoctorCatalog() {
  try {
    state.doctors = await apiClient.getDoctors();
    const options = state.doctors.length
      ? state.doctors.map(doc => `<option value="${doc.id}">${escapeHtml(`${doc.firstName} ${doc.lastName}`)} — ${escapeHtml(doc.specialty)}</option>`).join('')
      : '<option value="">No hay médicos activos</option>';
    for (const id of ['agenda-doctor-select', 'booking-doctor-select']) {
      const select = document.getElementById(id);
      if (!select) continue;
      select.innerHTML = options;
      if (state.doctors.length) {
        if (!state.doctors.some(doc => doc.id === state.currentDoctorId)) state.currentDoctorId = state.doctors[0].id;
        select.value = String(state.currentDoctorId);
      }
      select.disabled = state.doctors.length === 0;
    }
    const createSlotButton = document.getElementById('btn-open-create-slot');
    if (createSlotButton) createSlotButton.disabled = state.doctors.length === 0;
    updateDoctorBanner();
    if (state.currentDoctorId !== null) {
      await loadAgendaSlots();
      await loadBookingSlots();
    }
  } catch (error) {
    Toast.error(error.detail, 'Catálogo de médicos', error.code);
  }
}

function updateDoctorBanner() {
  const banner = document.getElementById('doctor-status-banner');
  const doctor = state.doctors.find(d => d.id === state.currentDoctorId);
  if (!doctor || !banner) return;

  if (doctor.isActive) {
    banner.innerHTML = `
      <div style="font-size: var(--font-size-sm); color: var(--color-text-muted); display: flex; align-items: center; gap: var(--spacing-2);">
        <span>Especialidad: <strong>${escapeHtml(doctor.specialty)}</strong></span>
        <span class="badge badge-available">
          <span class="badge-dot"></span> Activo para citas
        </span>
      </div>
    `;
  } else {
    banner.innerHTML = `
      <div style="background-color: #fef2f2; border: 1px solid #fecaca; padding: var(--spacing-3); border-radius: var(--radius-md); font-size: var(--font-size-sm); color: #991b1b; display: flex; align-items: center; gap: var(--spacing-2);">
        <svg width="18" height="18" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clip-rule="evenodd"/></svg>
        <span><strong>Atención:</strong> Este médico especialista está actualmente <strong>inactivo</strong> en el sistema clínico. No se podrán crear nuevos bloques ni reservas.</span>
      </div>
    `;
  }
}

async function loadAgendaSlots() {
  const container = document.getElementById('slots-container');
  const countBadge = document.getElementById('agenda-slots-count');
  if (!container) return;

  // Aún no hay médico seleccionado (el catálogo se carga en refreshDoctorCatalog).
  if (state.currentDoctorId === null || Number.isNaN(state.currentDoctorId)) {
    if (countBadge) countBadge.textContent = '—';
    container.innerHTML = `
      <div class="empty-state">
        <svg class="empty-state-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
          <circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/>
        </svg>
        <div class="empty-state-title">Selecciona un médico</div>
        <div class="empty-state-description">Elige un profesional de la salud para consultar sus bloques horarios.</div>
      </div>
    `;
    return;
  }

  container.innerHTML = `
    <div style="text-align: center; padding: var(--spacing-6); color: var(--color-text-muted);">
      <div class="empty-state-title">Cargando bloques horarios...</div>
    </div>
  `;

  try {
    const slots = await apiClient.getDoctorSlots(state.currentDoctorId, state.currentDate);
    renderSlots(slots);
  } catch (error) {
    container.innerHTML = `
      <div class="empty-state">
        <div class="empty-state-title" style="color: var(--status-reserved-text);">Error al consultar la agenda</div>
        <div class="empty-state-description">${escapeHtml(error.detail || error.message)}</div>
      </div>
    `;
    countBadge.textContent = 'Error';
    Toast.error(error.detail, 'Error en Agenda', error.code);
  }
}

function renderSlots(slots) {
  const container = document.getElementById('slots-container');
  const countBadge = document.getElementById('agenda-slots-count');
  if (!container) return;

  if (!slots || slots.length === 0) {
    countBadge.textContent = '0 bloques';
    countBadge.className = 'badge badge-pending';
    container.innerHTML = `
      <div class="empty-state">
        <svg class="empty-state-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
          <circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/>
        </svg>
        <div class="empty-state-title">No hay bloques registrados</div>
        <div class="empty-state-description">No se encontraron bloques para este médico en la fecha seleccionada. Pulsa en "+ Nuevo Bloque" para programar horarios.</div>
      </div>
    `;
    return;
  }

  countBadge.textContent = `${slots.length} ${slots.length === 1 ? 'bloque' : 'bloques'}`;
  countBadge.className = 'badge badge-available';

  // Ordenar cronológicamente por startTime
  const sorted = [...slots].sort((a, b) => a.startTime.localeCompare(b.startTime));

  container.innerHTML = `
    <div class="slots-grid">
      ${sorted.map(slot => {
        const isAvail = slot.status === 'Available';
        const statusBadge = isAvail
          ? '<span class="badge badge-available"><span class="badge-dot"></span> Disponible</span>'
          : '<span class="badge badge-reserved"><span class="badge-dot"></span> Reservado</span>';

        return `
          <div class="slot-card" data-slot-id="${slot.id}">
            <div class="slot-card-header">
              <span class="slot-time">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>
                ${escapeHtml(slot.startTime)} - ${escapeHtml(slot.endTime)}
              </span>
              ${statusBadge}
            </div>
            <div class="slot-body">
              <div class="text-xs text-muted">ID Bloque: #${slot.id}</div>
              ${!isAvail ? '<div class="slot-patient">Turno Ocupado</div>' : '<div class="text-xs" style="color: var(--status-available-text); margin-top: 4px;">Listo para agendar</div>'}
            </div>
          </div>
        `;
      }).join('')}
    </div>
  `;
}

// ============================================================================
// 3. Modal Crear Bloque
// ============================================================================
function initSlotModal() {
  const modal = document.getElementById('modal-create-slot');
  const openBtn = document.getElementById('btn-open-create-slot');
  const closeBtn = document.getElementById('btn-close-modal-slot');
  const cancelBtn = document.getElementById('btn-cancel-modal-slot');
  const form = document.getElementById('form-create-slot');
  const doctorInput = document.getElementById('slot-modal-doctor-name');
  const dateInput = document.getElementById('slot-modal-date');
  const errorText = document.getElementById('slot-modal-time-error');
  const previewText = document.getElementById('slot-time-preview');
  const submitBtn = document.getElementById('btn-submit-create-slot');

  const startHour = document.getElementById('slot-start-hour');
  const startMinute = document.getElementById('slot-start-minute');
  const startAmpm = document.getElementById('slot-start-ampm');
  const endHour = document.getElementById('slot-end-hour');
  const endMinute = document.getElementById('slot-end-minute');
  const endAmpm = document.getElementById('slot-end-ampm');
  const durationChips = Array.from(modal.querySelectorAll('.chip[data-minutes]'));

  // Rellenar desplegables (formato de 12 horas)
  const hourOptions = Array.from({ length: 12 }, (_, i) => String(i + 1).padStart(2, '0'));
  const minuteOptions = Array.from({ length: 12 }, (_, i) => String(i * 5).padStart(2, '0'));
  const fillSelect = (select, options) => {
    select.innerHTML = options.map(v => `<option value="${v}">${v}</option>`).join('');
  };
  [startHour, endHour].forEach(s => fillSelect(s, hourOptions));
  [startMinute, endMinute].forEach(s => fillSelect(s, minuteOptions));

  // --- Conversión entre 12 h (con AM/PM) y 24 h ---
  const readTime = (hourSel, minuteSel, ampmSel) => {
    let h = parseInt(hourSel.value, 10) % 12;
    if (ampmSel.value === 'PM') h += 12;
    return { h, m: parseInt(minuteSel.value, 10) };
  };
  const setTime = (hourSel, minuteSel, ampmSel, h24, m) => {
    let h = h24 % 12;
    if (h === 0) h = 12;
    hourSel.value = String(h).padStart(2, '0');
    minuteSel.value = String(m).padStart(2, '0');
    ampmSel.value = h24 >= 12 ? 'PM' : 'AM';
  };
  const to24 = (h24, m) => `${String(h24).padStart(2, '0')}:${String(m).padStart(2, '0')}`;
  const format12 = (h24, m) => {
    let h = h24 % 12;
    if (h === 0) h = 12;
    return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')} ${h24 >= 12 ? 'PM' : 'AM'}`;
  };
  const toMinutes = t => t.h * 60 + t.m;

  const clearChipActive = () => durationChips.forEach(c => c.classList.remove('active'));

  const updatePreview = () => {
    const s = readTime(startHour, startMinute, startAmpm);
    const e = readTime(endHour, endMinute, endAmpm);
    const diff = toMinutes(e) - toMinutes(s);
    previewText.textContent = diff > 0
      ? `${format12(s.h, s.m)} — ${format12(e.h, e.m)}  (${diff} min)`
      : `${format12(s.h, s.m)} — ${format12(e.h, e.m)}  (rango inválido)`;
  };

  // Cambio manual de hora: quita la duración rápida activa y refresca la vista previa
  [startHour, startMinute, startAmpm, endHour, endMinute, endAmpm].forEach(el => {
    el.addEventListener('change', () => {
      clearChipActive();
      updatePreview();
    });
  });

  // Duración rápida: calcula automáticamente la hora fin
  durationChips.forEach(chip => {
    chip.addEventListener('click', () => {
      const minutes = parseInt(chip.dataset.minutes, 10);
      const s = readTime(startHour, startMinute, startAmpm);
      let endTotal = toMinutes(s) + minutes;
      if (endTotal > 23 * 60 + 59) endTotal = 23 * 60 + 59;
      setTime(endHour, endMinute, endAmpm, Math.floor(endTotal / 60), endTotal % 60);
      clearChipActive();
      chip.classList.add('active');
      updatePreview();
    });
  });

  const openModal = () => {
    const doctor = state.doctors.find(d => d.id === state.currentDoctorId);
    doctorInput.value = doctor ? `${doctor.firstName} ${doctor.lastName} (${doctor.specialty})` : '';
    dateInput.value = state.currentDate;
    setTime(startHour, startMinute, startAmpm, 9, 0);
    setTime(endHour, endMinute, endAmpm, 9, 30);
    clearChipActive();
    errorText.textContent = '';
    updatePreview();
    modal.classList.add('is-open');
    startHour.focus();
  };

  const closeModal = () => {
    modal.classList.remove('is-open');
  };

  openBtn.addEventListener('click', openModal);
  closeBtn.addEventListener('click', closeModal);
  cancelBtn.addEventListener('click', closeModal);

  // Close on backdrop click
  modal.addEventListener('click', (e) => {
    if (e.target === modal) closeModal();
  });

  // Close on Escape key
  window.addEventListener('keydown', (e) => {
    if (e.key === 'Escape' && modal.classList.contains('is-open')) {
      closeModal();
    }
  });

  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    errorText.textContent = '';

    const s = readTime(startHour, startMinute, startAmpm);
    const eTime = readTime(endHour, endMinute, endAmpm);
    const startTime = to24(s.h, s.m);
    const endTime = to24(eTime.h, eTime.m);
    const date = dateInput.value;

    // Client validation: startTime < endTime
    if (toMinutes(s) >= toMinutes(eTime)) {
      errorText.textContent = 'La hora de inicio debe ser estrictamente menor que la hora de fin.';
      startHour.focus();
      return;
    }

    await withButtonLoading(submitBtn, async () => {
      try {
        await apiClient.createScheduleSlot(state.currentDoctorId, {
          date,
          startTime,
          endTime
        });

        Toast.success(`Bloque horario (${startTime} - ${endTime}) registrado correctamente.`);
        closeModal();
        // Update current date if changed in modal
        state.currentDate = date;
        document.getElementById('agenda-date-input').value = date;
        await loadAgendaSlots();
      } catch (error) {
        errorText.textContent = error.detail || error.message;
        Toast.error(error.detail, 'Error al crear bloque', error.code);
      }
    });
  });
}

// ============================================================================
// 4. Vista 2: Pacientes y Reserva
// ============================================================================
function initBookingView() {
  const tabDni = document.getElementById('tab-search-dni');
  const tabLastName = document.getElementById('tab-search-lastname');
  const formDni = document.getElementById('form-search-dni');
  const formLastName = document.getElementById('form-search-lastname');
  const inputDni = document.getElementById('input-search-dni');
  const inputLastName = document.getElementById('input-search-lastname');
  const btnSearchDni = document.getElementById('btn-search-dni');
  const btnSearchLastName = document.getElementById('btn-search-lastname');

  const toggleNewBtn = document.getElementById('btn-toggle-new-patient');
  const panelNew = document.getElementById('panel-new-patient');
  const cancelNewBtn = document.getElementById('btn-cancel-new-patient');
  const formRegister = document.getElementById('form-register-patient');
  const btnSubmitPatient = document.getElementById('btn-submit-patient');

  const bookingDoctorSelect = document.getElementById('booking-doctor-select');
  const bookingDateInput = document.getElementById('booking-date-input');
  const bookingSlotSelect = document.getElementById('booking-slot-select');
  const bookingReasonInput = document.getElementById('booking-reason-input');
  const btnConfirmBooking = document.getElementById('btn-confirm-booking');

  // Switch search tabs
  tabDni.addEventListener('click', () => {
    tabDni.classList.add('active');
    tabDni.setAttribute('aria-selected', 'true');
    tabLastName.classList.remove('active');
    tabLastName.setAttribute('aria-selected', 'false');
    formDni.style.display = 'flex';
    formLastName.style.display = 'none';
    inputDni.focus();
  });

  tabLastName.addEventListener('click', () => {
    tabLastName.classList.add('active');
    tabLastName.setAttribute('aria-selected', 'true');
    tabDni.classList.remove('active');
    tabDni.setAttribute('aria-selected', 'false');
    formLastName.style.display = 'flex';
    formDni.style.display = 'none';
    inputLastName.focus();
  });

  // Toggle New Patient Form
  toggleNewBtn.addEventListener('click', () => {
    const isHidden = panelNew.style.display === 'none';
    panelNew.style.display = isHidden ? 'block' : 'none';
    toggleNewBtn.textContent = isHidden ? '− Cerrar Formulario' : '+ Nuevo Paciente';
    if (isHidden) {
      document.getElementById('reg-patient-dni').focus();
    }
  });

  cancelNewBtn.addEventListener('click', () => {
    panelNew.style.display = 'none';
    toggleNewBtn.textContent = '+ Nuevo Paciente';
    formRegister.reset();
  });

  // Register Patient Form Submit
  formRegister.addEventListener('submit', async (e) => {
    e.preventDefault();
    const dni = document.getElementById('reg-patient-dni').value.trim();
    const firstName = document.getElementById('reg-patient-firstname').value.trim();
    const lastName = document.getElementById('reg-patient-lastname').value.trim();
    const email = document.getElementById('reg-patient-email').value.trim() || null;
    const phone = document.getElementById('reg-patient-phone').value.trim();
    const dniError = document.getElementById('reg-patient-dni-error');

    dniError.textContent = '';

    if (!/^\d{8}$/.test(dni)) {
      dniError.textContent = 'El DNI debe contener exactamente 8 dígitos numéricos.';
      return;
    }

    if (!phone) {
      Toast.warning('El teléfono es obligatorio.');
      document.getElementById('reg-patient-phone').focus();
      return;
    }

    await withButtonLoading(btnSubmitPatient, async () => {
      try {
        const patient = await apiClient.registerPatient({
          dni,
          firstName,
          lastName,
          email,
          phone
        });

        Toast.success(`Paciente ${patientFullName(patient)} registrado correctamente.`);
        selectPatient(patient);
        formRegister.reset();
        panelNew.style.display = 'none';
        toggleNewBtn.textContent = '+ Nuevo Paciente';
      } catch (error) {
        if (error.code === 'PATIENT_DNI_DUPLICATE') {
          dniError.textContent = 'El DNI ya se encuentra registrado por otro paciente.';
        }
        Toast.error(error.detail, 'Error al registrar paciente', error.code);
      }
    });
  });

  // Search by DNI
  formDni.addEventListener('submit', async (e) => {
    e.preventDefault();
    const dni = inputDni.value.trim();
    if (!/^\d{8}$/.test(dni)) {
      Toast.warning('El DNI debe contener exactamente 8 dígitos numéricos.');
      return;
    }

    await withButtonLoading(btnSearchDni, async () => {
      try {
        const results = await apiClient.searchPatientByDni(dni);
        renderPatientSearchResults(results);
      } catch (error) {
        renderPatientSearchResults([]);
        Toast.error(error.detail, 'Búsqueda por DNI', error.code);
      }
    });
  });

  // Search by Last Name
  formLastName.addEventListener('submit', async (e) => {
    e.preventDefault();
    const lastName = inputLastName.value.trim();
    if (lastName.length < 2) {
      Toast.warning('El apellido debe tener al menos 2 caracteres.');
      return;
    }

    await withButtonLoading(btnSearchLastName, async () => {
      try {
        const results = await apiClient.searchPatientsByLastName(lastName);
        renderPatientSearchResults(results);
      } catch (error) {
        renderPatientSearchResults([]);
        Toast.error(error.detail, 'Búsqueda por Apellido', error.code);
      }
    });
  });

  // Booking Doctors & Date setup
  bookingDateInput.value = state.currentDate;

  bookingDoctorSelect.addEventListener('change', () => {
    loadBookingSlots();
  });

  bookingDateInput.addEventListener('change', () => {
    loadBookingSlots();
  });

  bookingSlotSelect.addEventListener('change', () => {
    checkBookingReady();
  });

  bookingReasonInput.addEventListener('input', () => {
    checkBookingReady();
  });

  // Confirm Booking
  btnConfirmBooking.addEventListener('click', async () => {
    if (!state.selectedPatient) {
      Toast.warning('Debes seleccionar un paciente primero.');
      return;
    }

    const slotId = parseInt(bookingSlotSelect.value, 10);
    if (!slotId) {
      Toast.warning('Debes seleccionar un bloque horario disponible.');
      return;
    }

    const reason = bookingReasonInput.value.trim();
    if (!reason) {
      Toast.warning('El motivo de la cita es obligatorio.');
      bookingReasonInput.focus();
      return;
    }

    await withButtonLoading(btnConfirmBooking, async () => {
      try {
        const appointment = await apiClient.reserveAppointment({
          scheduleSlotId: slotId,
          patientId: state.selectedPatient.id,
          reason
        });

        Toast.success(`¡Cita #${appointment.id} reservada con éxito para ${patientFullName(state.selectedPatient)}!`);
        bookingReasonInput.value = '';
        
        // Refresh slots in booking & agenda
        await loadBookingSlots();
        if (state.activeTab === 'view-agenda') {
          await loadAgendaSlots();
        }
      } catch (error) {
        Toast.error(error.detail, 'Error al reservar cita', error.code);
      }
    });
  });

  loadBookingSlots();
}

function renderPatientSearchResults(patients) {
  const container = document.getElementById('patient-search-results');
  if (!container) return;

  if (!patients || patients.length === 0) {
    container.innerHTML = `
      <div class="empty-state" style="padding: var(--spacing-4);">
        <div class="empty-state-title">No se encontraron pacientes</div>
        <div class="empty-state-description">Verifica los datos ingresados o utiliza la opción "+ Nuevo Paciente" para registrarlo.</div>
      </div>
    `;
    return;
  }

  container.innerHTML = patients.map(p => `
    <div class="patient-result-card ${state.selectedPatient?.id === p.id ? 'is-selected' : ''}" data-patient-id="${p.id}">
      <div>
        <div class="patient-info-name">${escapeHtml(patientFullName(p))}</div>
        <div class="patient-info-meta">DNI: ${escapeHtml(p.dni)} | Tel: ${escapeHtml(p.phone || 'No registrado')}</div>
      </div>
      <button type="button" class="btn btn-primary btn-sm btn-select-patient" data-patient='${JSON.stringify(p)}'>
        Seleccionar
      </button>
    </div>
  `).join('');

  container.querySelectorAll('.btn-select-patient').forEach(btn => {
    btn.addEventListener('click', (e) => {
      const p = JSON.parse(e.currentTarget.getAttribute('data-patient'));
      selectPatient(p);
    });
  });
}

function selectPatient(patient) {
  state.selectedPatient = patient;
  const display = document.getElementById('selected-patient-display');
  if (display) {
    display.innerHTML = `
      <div class="selected-patient-box">
        <div>
          <div style="font-weight: 600; font-size: var(--font-size-sm); color: var(--color-primary);">${escapeHtml(patientFullName(patient))}</div>
          <div class="text-xs text-muted">DNI: ${escapeHtml(patient.dni)} | Tel: ${escapeHtml(patient.phone || 'N/A')}</div>
        </div>
        <button type="button" class="btn btn-outline btn-sm" id="btn-deselect-patient" style="min-height: 32px; padding: 2px 8px;">
          Cambiar
        </button>
      </div>
    `;

    document.getElementById('btn-deselect-patient').addEventListener('click', () => {
      state.selectedPatient = null;
      display.innerHTML = `
        <div class="empty-state" style="padding: var(--spacing-4); border: 1px dashed var(--color-border); border-radius: var(--radius-md);">
          <span class="text-sm text-muted">Selecciona un paciente en el panel izquierdo para continuar.</span>
        </div>
      `;
      checkBookingReady();
    });
  }

  // Highlight in search results if visible
  document.querySelectorAll('.patient-result-card').forEach(card => {
    if (parseInt(card.getAttribute('data-patient-id'), 10) === patient.id) {
      card.classList.add('is-selected');
    } else {
      card.classList.remove('is-selected');
    }
  });

  checkBookingReady();
}

async function loadBookingSlots() {
  const doctorSelect = document.getElementById('booking-doctor-select');
  const dateInput = document.getElementById('booking-date-input');
  const slotSelect = document.getElementById('booking-slot-select');
  const countBadge = document.getElementById('booking-slots-count');
  if (!doctorSelect || !dateInput || !slotSelect) return;

  const doctorId = parseInt(doctorSelect.value, 10);
  const date = dateInput.value;

  // Catálogo de médicos aún no disponible; refreshDoctorCatalog volverá a cargar.
  if (!Number.isInteger(doctorId)) {
    slotSelect.innerHTML = '<option value="">-- Selecciona un horario --</option>';
    slotSelect.disabled = true;
    return;
  }

  slotSelect.disabled = true;
  slotSelect.innerHTML = '<option value="">Cargando bloques disponibles...</option>';

  try {
    const slots = await apiClient.getDoctorSlots(doctorId, date);
    const available = (slots || []).filter(s => s.status === 'Available');

    countBadge.textContent = `${available.length} disp.`;

    if (available.length === 0) {
      slotSelect.innerHTML = '<option value="">No hay bloques disponibles en esta fecha</option>';
      slotSelect.disabled = true;
    } else {
      slotSelect.innerHTML = [
        '<option value="">-- Selecciona un horario --</option>',
        ...available.map(s => `<option value="${s.id}">${s.startTime} - ${s.endTime}</option>`)
      ].join('');
      slotSelect.disabled = false;
    }
  } catch (error) {
    slotSelect.innerHTML = '<option value="">Error al cargar horarios</option>';
    slotSelect.disabled = true;
    countBadge.textContent = '0 disp.';
  }

  checkBookingReady();
}

function checkBookingReady() {
  const btn = document.getElementById('btn-confirm-booking');
  const slotSelect = document.getElementById('booking-slot-select');
  const reasonInput = document.getElementById('booking-reason-input');
  if (!btn) return;

  const hasPatient = !!state.selectedPatient;
  const hasSlot = !!slotSelect && !!slotSelect.value;
  const hasReason = !!reasonInput && !!reasonInput.value.trim();

  btn.disabled = !(hasPatient && hasSlot && hasReason);
}

// ============================================================================
// 5. Vista 3: Atención Clínica
// ============================================================================
function initAttentionView() {
  const formSearchAppt = document.getElementById('form-search-appointment');
  const inputApptId = document.getElementById('input-appointment-id');
  const btnLoadAppt = document.getElementById('btn-load-appointment');

  const containerDetail = document.getElementById('appointment-detail-container');
  const emptyState = document.getElementById('appointment-empty-state');
  const bannerAttended = document.getElementById('attended-banner');

  const formAttention = document.getElementById('form-register-attention');
  const inputDiagnosis = document.getElementById('attention-diagnosis');
  const inputPrescription = document.getElementById('attention-prescription');
  const btnSubmitAttention = document.getElementById('btn-submit-attention');

  formSearchAppt.addEventListener('submit', async (e) => {
    e.preventDefault();
    const apptId = parseInt(inputApptId.value, 10);
    if (!apptId || apptId <= 0) {
      Toast.warning('Ingresa un ID de cita válido mayor a 0.');
      return;
    }

    await withButtonLoading(btnLoadAppt, async () => {
      try {
        const appt = await apiClient.getAppointmentById(apptId);
        state.currentAppointment = appt;
        renderAppointmentDetails(appt);
      } catch (error) {
        state.currentAppointment = null;
        containerDetail.style.display = 'none';
        emptyState.style.display = 'block';
        Toast.error(error.detail, 'Consulta de Cita', error.code);
      }
    });
  });

  formAttention.addEventListener('submit', async (e) => {
    e.preventDefault();
    if (!state.currentAppointment) return;

    const diagnosis = inputDiagnosis.value.trim();
    const prescription = inputPrescription.value.trim() || null;

    if (!diagnosis) {
      Toast.warning('El diagnóstico clínico es obligatorio.');
      inputDiagnosis.focus();
      return;
    }

    await withButtonLoading(btnSubmitAttention, async () => {
      try {
        await apiClient.registerAttention(state.currentAppointment.id, {
          diagnosis,
          prescription
        });

        Toast.success(`Atención clínica de la Cita #${state.currentAppointment.id} registrada con éxito.`);

        // Re-load the appointment to show updated Attended state and lock fields
        const updated = await apiClient.getAppointmentById(state.currentAppointment.id);
        state.currentAppointment = updated;
        renderAppointmentDetails(updated);
      } catch (error) {
        Toast.error(error.detail, 'Error al registrar atención', error.code);
      }
    });
  });
}

function renderAppointmentDetails(appt) {
  const containerDetail = document.getElementById('appointment-detail-container');
  const emptyState = document.getElementById('appointment-empty-state');
  const bannerAttended = document.getElementById('attended-banner');

  const summaryId = document.getElementById('summary-appt-id');
  const summaryBadge = document.getElementById('summary-appt-badge');
  const summaryPatient = document.getElementById('summary-patient-name');
  const summaryDni = document.getElementById('summary-patient-dni');
  const summaryDoctor = document.getElementById('summary-doctor-name');
  const summaryTime = document.getElementById('summary-slot-time');
  const summaryReason = document.getElementById('summary-reason');

  const inputDiagnosis = document.getElementById('attention-diagnosis');
  const inputPrescription = document.getElementById('attention-prescription');
  const btnSubmitAttention = document.getElementById('btn-submit-attention');

  emptyState.style.display = 'none';
  containerDetail.style.display = 'block';

  summaryId.textContent = appt.id;
  summaryPatient.textContent = appt.patient?.fullName || '—';
  summaryDni.textContent = appt.patient?.dni || '—';
  summaryDoctor.textContent = appt.doctor?.fullName || '—';
  summaryTime.textContent = appt.slot
    ? `${appt.slot.date} (${appt.slot.startTime} - ${appt.slot.endTime})`
    : '—';
  summaryReason.textContent = appt.reason;

  const isAttended = appt.status === 'Attended';

  if (isAttended) {
    summaryBadge.innerHTML = '<span class="badge badge-attended"><span class="badge-dot"></span> Atendida</span>';
    bannerAttended.style.display = 'flex';

    inputDiagnosis.value = appt.diagnosis || '';
    inputPrescription.value = appt.prescription || '';
    inputDiagnosis.disabled = true;
    inputPrescription.disabled = true;
    btnSubmitAttention.disabled = true;
    btnSubmitAttention.style.display = 'none';
  } else {
    summaryBadge.innerHTML = '<span class="badge badge-pending"><span class="badge-dot"></span> Pendiente</span>';
    bannerAttended.style.display = 'none';

    inputDiagnosis.value = '';
    inputPrescription.value = '';
    inputDiagnosis.disabled = false;
    inputPrescription.disabled = false;
    btnSubmitAttention.disabled = false;
    btnSubmitAttention.style.display = 'inline-flex';
    inputDiagnosis.focus();
  }
}
