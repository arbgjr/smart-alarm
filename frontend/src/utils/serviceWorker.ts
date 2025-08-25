import { Workbox } from 'workbox-window';

let wb: Workbox | null = null;

export const initServiceWorker = async (): Promise<void> => {
  if ('serviceWorker' in navigator && import.meta.env.PROD) {
    wb = new Workbox('/sw.js');
    
    // Event listeners
    wb.addEventListener('controlling', () => {
      console.log('Service Worker is controlling the page');
      // Refresh the page to use the new service worker
      window.location.reload();
    });

    wb.addEventListener('waiting', () => {
      console.log('New Service Worker is waiting');
      // Show user a message about update available
      showUpdateAvailable();
    });

    wb.addEventListener('installed', () => {
      console.log('Service Worker installed');
    });

    try {
      const registration = await wb.register();
      console.log('Service Worker registered successfully:', registration);
    } catch (error) {
      console.error('Service Worker registration failed:', error);
    }
  }
};

export const updateServiceWorker = async (): Promise<void> => {
  if (wb) {
    // Send SKIP_WAITING message to the waiting service worker
    wb.messageSkipWaiting();
  }
};

const showUpdateAvailable = (): void => {
  // Create update notification
  const updateNotification = document.createElement('div');
  updateNotification.className = 'fixed top-4 right-4 bg-blue-600 text-white px-6 py-3 rounded-lg shadow-lg z-50 flex items-center gap-3';
  updateNotification.innerHTML = `
    <span>Nova versão disponível!</span>
    <button id="update-btn" class="bg-white text-blue-600 px-3 py-1 rounded text-sm hover:bg-blue-50">
      Atualizar
    </button>
    <button id="dismiss-btn" class="text-white hover:text-blue-200">
      ✕
    </button>
  `;

  document.body.appendChild(updateNotification);

  // Handle update button click
  const updateBtn = document.getElementById('update-btn');
  const dismissBtn = document.getElementById('dismiss-btn');

  updateBtn?.addEventListener('click', () => {
    updateServiceWorker();
    document.body.removeChild(updateNotification);
  });

  dismissBtn?.addEventListener('click', () => {
    document.body.removeChild(updateNotification);
  });

  // Auto dismiss after 30 seconds
  setTimeout(() => {
    if (document.body.contains(updateNotification)) {
      document.body.removeChild(updateNotification);
    }
  }, 30000);
};

export const isServiceWorkerSupported = (): boolean => {
  return 'serviceWorker' in navigator;
};

export const checkForUpdates = async (): Promise<void> => {
  if (wb) {
    try {
      await wb.update();
    } catch (error) {
      console.error('Failed to check for Service Worker updates:', error);
    }
  }
};