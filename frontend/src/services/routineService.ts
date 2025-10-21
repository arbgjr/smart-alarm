import api from '../lib/api';

export interface RoutineDto {
  id: string;
  name: string;
  description?: string;
  isEnabled: boolean;
  alarmIds: string[];
}

export type PaginatedRoutinesResponse = PaginatedResponse<RoutineDto>;

/*
export interface RoutineStepDto {
  id: string;
  routineId: string;
  name: string;
  description?: string;
  stepType: string;
  configuration: Record<string, any>;
  order: number;
  isEnabled: boolean;
}
*/

export interface CreateRoutinePayload {
  name: string;
  description?: string;
  alarmIds?: string[];
}

export interface UpdateRoutinePayload extends CreateRoutinePayload {
  id: string;
}

export interface BulkUpdateRoutinesPayload {
  routineIds: string[];
  action: 'Enable' | 'Disable' | 'Delete';
}

/*

export interface UpdateRoutineRequest {
  name?: string;
  description?: string;
  isEnabled?: boolean;
}

export interface UpdateRoutineStepRequest {
  name?: string;
  description?: string;
  stepType?: string;
  configuration?: Record<string, any>;
  order?: number;
  isEnabled?: boolean;
}
*/

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

    const response = await api.get<PaginatedRoutinesResponse>(url);
    return response.data;
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
