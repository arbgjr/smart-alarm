import { useEffect } from 'react';
import { useRegisterSW } from 'virtual:pwa-register/react';
import { toast } from 'react-hot-toast';

export function PwaUpdater() {
  const {
    offlineReady: [offlineReady, setOfflineReady],
    needRefresh: [needRefresh, setNeedRefresh],
    updateServiceWorker,
  } = useRegisterSW({
    onRegistered(r) {
      console.log('Service Worker registrado:', r);
    },
    onRegisterError(error) {
      console.error('Erro no registro do Service Worker:', error);
    },
  });

  useEffect(() => {
    if (offlineReady) {
      toast.success('App pronto para funcionar offline!');
      setOfflineReady(false);
    } else if (needRefresh) {
      toast(
        (t) => (
          <div className="flex flex-col items-center gap-2">
            <span>Nova versão disponível!</span>
            <button
              className="w-full rounded-md bg-indigo-600 px-3 py-1.5 text-sm font-semibold text-white shadow-sm hover:bg-indigo-500"
              onClick={() => {
                updateServiceWorker(true);
                toast.dismiss(t.id);
              }}
            >
              Atualizar
            </button>
          </div>
        ),
        { duration: Infinity } // Mantém o toast até ser dispensado
      );
    }
  }, [offlineReady, needRefresh, setOfflineReady, setNeedRefresh, updateServiceWorker]);

  return null; // Este componente não renderiza nada na UI
}
