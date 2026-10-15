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
