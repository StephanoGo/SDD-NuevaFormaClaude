/**
 * Toast Notification Service
 * Accessible, floating alert manager with ProblemDetails error code support.
 */
class ToastManager {
  constructor() {
    this.container = null;
    this.ensureContainer();
  }

  ensureContainer() {
    if (!this.container) {
      let existing = document.querySelector('.toast-container');
      if (!existing) {
        existing = document.createElement('div');
        existing.className = 'toast-container';
        existing.setAttribute('aria-live', 'polite');
        existing.setAttribute('aria-atomic', 'true');
        document.body.appendChild(existing);
      }
      this.container = existing;
    }
  }

  show(message, type = 'info', title = '', code = null, duration = 4000) {
    this.ensureContainer();

    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    toast.setAttribute('role', type === 'error' ? 'alert' : 'status');

    let iconSvg = '';
    if (type === 'success') {
      iconSvg = '<svg class="toast-icon" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd"/></svg>';
    } else if (type === 'error') {
      iconSvg = '<svg class="toast-icon" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clip-rule="evenodd"/></svg>';
    } else {
      iconSvg = '<svg class="toast-icon" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clip-rule="evenodd"/></svg>';
    }

    const defaultTitle = type === 'success' ? 'Operación Exitosa' : type === 'error' ? 'Error Clínico' : 'Notificación';
    const displayTitle = title || defaultTitle;

    const codeHtml = code ? `<div class="toast-code">${this.escapeHtml(code)}</div>` : '';

    toast.innerHTML = `
      ${iconSvg}
      <div class="toast-content">
        <div class="toast-title">${this.escapeHtml(displayTitle)}</div>
        <div class="toast-message">${this.escapeHtml(message)}</div>
        ${codeHtml}
      </div>
      <button type="button" class="toast-close" aria-label="Cerrar notificación">
        <svg width="14" height="14" viewBox="0 0 14 14" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><path d="M1 1l12 12M13 1L1 13"/></svg>
      </button>
    `;

    this.container.appendChild(toast);

    let dismissTimer = null;
    const startTimer = () => {
      if (duration > 0) {
        dismissTimer = setTimeout(() => closeToast(), duration);
      }
    };
    const clearTimer = () => {
      if (dismissTimer) clearTimeout(dismissTimer);
    };

    const closeToast = () => {
      clearTimer();
      toast.classList.add('toast-closing');
      setTimeout(() => {
        if (toast.parentNode) {
          toast.parentNode.removeChild(toast);
        }
      }, 250);
    };

    toast.querySelector('.toast-close').addEventListener('click', closeToast);
    toast.addEventListener('mouseenter', clearTimer);
    toast.addEventListener('mouseleave', startTimer);

    startTimer();
  }

  success(message, title = 'Operación Exitosa') {
    this.show(message, 'success', title);
  }

  error(message, title = 'Error', code = null) {
    this.show(message, 'error', title, code, 6000);
  }

  warning(message, title = 'Advertencia') {
    this.show(message, 'warning', title);
  }

  escapeHtml(str) {
    if (!str) return '';
    return String(str)
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
  }
}

// Global instance
window.Toast = new ToastManager();
