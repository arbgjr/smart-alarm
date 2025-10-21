import { toast } from 'react-hot-toast';

/**
 * Serviço centralizado para exibir notificações (toasts) na aplicação.
 * Abstrai a biblioteca subjacente para facilitar a manutenção.
 */
export const notificationService = {
  /**
   * Exibe uma notificação de sucesso.
   */
  success: (message: string) => toast.success(message),

  /**
   * Exibe uma notificação de erro.
   */
  error: (message: string) => toast.error(message),

  // Futuramente, podemos adicionar outros tipos como info, warning, loading, etc.
};
