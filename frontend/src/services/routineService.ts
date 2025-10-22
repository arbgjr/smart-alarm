import api from '../lib/api';

// Alinhado com RoutineDto do backend
export interface RoutineDto {
  id: string;
  name: string;
  description: string;
  alarmIds: string[]; // Mantendo plural conforme APIs Create/Update
  isActive: boolean; // Alinhado com backend (era isEnabled)
  actions: string[]; // Adicionado conforme backend
  createdAt: string;
  updatedAt: string; // Adicionado conforme backend
}

export interface PaginatedRoutinesResponse extends PaginatedResponse<RoutineDto> {
  routines: RoutineDto[]; // Alias para items - resolve erro de 'routines' não existir
}

// Interfaces para Steps - removidas pois não implementadas no backend ainda
// Funcionalidade de steps será implementada futuramente

// Alinhado com CreateRoutineDto do backend
export interface CreateRoutinePayload {
  name: string;
  description?: string;
  alarmIds?: string[];
}

// Alinhado com UpdateRoutineDto do backend
export interface UpdateRoutinePayload {
  id: string;
  name: string;
  description?: string;
  alarmIds: string[];
  isActive: boolean;
}

export interface BulkUpdateRoutinesPayload {
  routineIds: string[];
  action: 'Enable' | 'Disable' | 'Delete';
}

// Aliases para compatibilidade com componentes existentes
export type CreateRoutineRequest = CreateRoutinePayload;
export type UpdateRoutineRequest = Omit<UpdateRoutinePayload, 'id'>;
export type RoutineListResponse = PaginatedRoutinesResponse;

// Interfaces para Steps - removidas pois não implementadas no backend
// CreateRoutineStepRequest será implementado quando Steps for adicionado ao backend

export interface PaginatedResponse<T> {
  items: T[];
  pageNumber: number;
  totalPages: number;
  totalCount: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface RoutineFilters {
  isEnabled?: boolean;
  search?: string;
  page?: number;
  pageNumber?: number; // Adicionado para resolver erro TS
  pageSize?: number;
}

const BASE_PATH = '/api/v1/routines';

export const routineService = {
  async getRoutines(filters: RoutineFilters = {}): Promise<PaginatedRoutinesResponse> {
    const params = new URLSearchParams();

    if (filters.isEnabled !== undefined) {
      params.append('isEnabled', filters.isEnabled.toString());
    }
    if (filters.search) {
      params.append('search', filters.search);
    }
    if (filters.pageNumber) {
      params.append('pageNumber', filters.pageNumber.toString());
    }
    if (filters.pageSize) {
      params.append('pageSize', filters.pageSize.toString());
    }

    const queryString = params.toString();
    const url = queryString ? `${BASE_PATH}?${queryString}` : BASE_PATH;

    const response = await api.get<PaginatedResponse<RoutineDto>>(url);

    // Adiciona alias 'routines' para compatibilidade com componentes
    return {
      ...response.data,
      routines: response.data.items,
    };
  },

  async getRoutine(id: string): Promise<RoutineDto> {
    const response = await api.get<RoutineDto>(`${BASE_PATH}/${id}`);
    return response.data;
  },

  async createRoutine(payload: CreateRoutinePayload): Promise<RoutineDto> {
    const response = await api.post<RoutineDto>(BASE_PATH, payload);
    return response.data;
  },

  async updateRoutine(id: string, payload: UpdateRoutinePayload): Promise<RoutineDto> {
    const response = await api.put<RoutineDto>(`${BASE_PATH}/${id}`, payload);
    return response.data;
  },

  async deleteRoutine(id: string): Promise<void> {
    await api.delete(`${BASE_PATH}/${id}`);
  },

  async enableRoutine(id: string): Promise<RoutineDto> {
    const response = await api.post<RoutineDto>(`${BASE_PATH}/${id}/activate`);
    return response.data;
  },

  async disableRoutine(id: string): Promise<RoutineDto> {
    const response = await api.post<RoutineDto>(`${BASE_PATH}/${id}/deactivate`);
    return response.data;
  },

  async bulkUpdateRoutines(payload: BulkUpdateRoutinesPayload): Promise<void> {
    await api.post(`${BASE_PATH}/bulk-update`, payload);
  },
};

// Export default para resolver erro TS2305
export default routineService;
