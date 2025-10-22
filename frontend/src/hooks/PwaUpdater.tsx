import { useEffect } from 'react';

export function PwaUpdater() {
  // PWA functionality temporarily disabled to avoid dependency issues
  // Will be re-enabled when virtual:pwa-register/react is properly configured

  useEffect(() => {
    console.log('PWA updater loaded (disabled)');
  }, []);

  return null; // Este componente não renderiza nada na UI
}
