/**
 * ApiClient - Centralized HTTP Client for Clinical Appointments API
 * Integrates with ASP.NET Core Minimal APIs and extracts ProblemDetails errors.
 */
class ApiError extends Error {
  constructor({ status, title, detail, code }) {
    super(detail || title || 'Error en el servidor');
    this.name = 'ApiError';
    this.status = status;
    this.title = title;
    this.detail = detail;
    this.code = code || 'UNKNOWN_ERROR';
  }
}

class ApiClient {
  constructor(baseUrl = '') {
    this.baseUrl = baseUrl;
  }

  async _request(endpoint, options = {}) {
    const url = `${this.baseUrl}${endpoint}`;
    const headers = {
      'Accept': 'application/json',
      'Content-Type': 'application/json; charset=utf-8',
      ...options.headers
    };

    const config = {
      ...options,
      headers
    };

    let response;
    try {
      response = await fetch(url, config);
    } catch (networkErr) {
      throw new ApiError({
        status: 0,
        title: 'Error de Red',
        detail: 'No se pudo conectar con el servidor clínico. Verifique su conexión.',
        code: 'NETWORK_ERROR'
      });
    }

    // Attempt to read JSON
    let data = null;
    const contentType = response.headers.get('content-type') || '';
    if (contentType.includes('application/json') || contentType.includes('problem+json')) {
      try {
        data = await response.json();
      } catch (e) {
        data = null;
      }
    }

    if (!response.ok) {
      const code = data?.code || data?.extensions?.code || `HTTP_${response.status}`;
      const title = data?.title || `Error ${response.status}`;
      const detail = data?.detail || 'Ocurrió un error inesperado al procesar la solicitud.';
      
      throw new ApiError({
        status: response.status,
        title,
        detail,
        code
      });
    }

    return data;
  }

  // --- Pacientes ---
  async registerPatient(patientData) {
    return await this._request('/api/patients', {
      method: 'POST',
      body: JSON.stringify(patientData)
    });
  }

  async searchPatientByDni(dni) {
    const encoded = encodeURIComponent(dni.trim());
    return await this._request(`/api/patients?dni=${encoded}`, {
      method: 'GET'
    });
  }

  async searchPatientsByLastName(lastName) {
    const encoded = encodeURIComponent(lastName.trim());
    return await this._request(`/api/patients?lastName=${encoded}`, {
      method: 'GET'
    });
  }

  // --- Agenda y Bloques ---
  async getDoctors(includeInactive = false) {
    return await this._request(`/api/doctors?includeInactive=${includeInactive}`, { method: 'GET' });
  }

  async getSpecialties(includeInactive = false) {
    return await this._request(`/api/specialties?includeInactive=${includeInactive}`, { method: 'GET' });
  }

  async addSpecialty(name) { return this._request('/api/specialties', { method: 'POST', body: JSON.stringify({ name }) }); }
  async updateSpecialty(id, name) { return this._request(`/api/specialties/${id}`, { method: 'PUT', body: JSON.stringify({ name }) }); }
  async deactivateSpecialty(id) { return this._request(`/api/specialties/${id}/deactivation`, { method: 'POST' }); }
  async addDoctor(data) { return this._request('/api/doctors', { method: 'POST', body: JSON.stringify(data) }); }
  async updateDoctor(id, data) { return this._request(`/api/doctors/${id}`, { method: 'PUT', body: JSON.stringify(data) }); }
  async deactivateDoctor(id) { return this._request(`/api/doctors/${id}/deactivation`, { method: 'POST' }); }

  async getClinicalHistory(patientId) {
    return this._request(`/api/patients/${patientId}/clinical-history`, { method: 'GET' });
  }

  async listAppointments(filters) {
    const query = new URLSearchParams();
    Object.entries(filters).forEach(([key, value]) => { if (value !== '' && value != null) query.set(key, value); });
    return this._request(`/api/appointments?${query}`, { method: 'GET' });
  }

  async cancelAppointment(id) {
    return this._request(`/api/appointments/${id}/cancellation`, { method: 'POST' });
  }

  async getDoctorSlots(doctorId, date) {
    const encodedDate = encodeURIComponent(date);
    return await this._request(`/api/doctors/${doctorId}/schedule-slots?date=${encodedDate}`, {
      method: 'GET'
    });
  }

  async createScheduleSlot(doctorId, slotData) {
    return await this._request(`/api/doctors/${doctorId}/schedule-slots`, {
      method: 'POST',
      body: JSON.stringify(slotData)
    });
  }

  // --- Citas Médicas y Atención ---
  async reserveAppointment(reservationData) {
    return await this._request('/api/appointments', {
      method: 'POST',
      body: JSON.stringify(reservationData)
    });
  }

  async getAppointmentById(appointmentId) {
    return await this._request(`/api/appointments/${appointmentId}`, {
      method: 'GET'
    });
  }

  async registerAttention(appointmentId, attentionData) {
    return await this._request(`/api/appointments/${appointmentId}/attention`, {
      method: 'POST',
      body: JSON.stringify(attentionData)
    });
  }
}

// Global instance
window.apiClient = new ApiClient();
