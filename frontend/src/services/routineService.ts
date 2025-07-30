import api from '../lib/api';

export interface RoutineDto {
  id: string;
  name: string;
  description?: string;
  isEnabled: boolean;
  userId: string;
  createdAt: string;
  updatedAt: string;
  steps: RoutineStepDto[];
}

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

export interface CreateRoutineRequest {
  name: string;
  description?: string;
  isEnabled?: boolean;
  steps?: CreateRoutineStepRequest[];
}

export interface CreateRoutineStepRequest {
  name: string;
  description?: string;
  stepType: string;
  configuration: Record<string, any>;
  order: number;
  isEnabled?: boolean;
}

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

export interface RoutineListResponse {
  routines: RoutineDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface RoutineFilters {
  isEnabled?: boolean;
  search?: string;
  page?: number;
  pageSize?: number;
}

export class RoutineService {
  private static readonly BASE_PATH = '/api/v1/routines';

  /**
   * Get user's routines with optional filtering and pagination
   */
  static async getRoutines(filters: RoutineFilters = {}): Promise<RoutineListResponse> {
    const params = new URLSearchParams();

    if (filters.isEnabled !== undefined) {
      params.append('isEnabled', filters.isEnabled.toString());
    }
    if (filters.search) {
      params.append('search', filters.search);
    }
    if (filters.page) {
      params.append('page', filters.page.toString());
    }
    if (filters.pageSize) {
      params.append('pageSize', filters.pageSize.toString());
    }

    const queryString = params.toString();
    const url = queryString ? `${this.BASE_PATH}?${queryString}` : this.BASE_PATH;

    const response = await api.get<RoutineListResponse>(url);
    return response.data;
  }

  /**
   * Get active (enabled) routines for dashboard
   */
  static async getActiveRoutines(): Promise<RoutineDto[]> {
    const response = await this.getRoutines({ isEnabled: true, pageSize: 10 });
    return response.routines;
  }

  /**
   * Get a specific routine by ID
   */
  static async getRoutine(id: string): Promise<RoutineDto> {
    const response = await api.get<RoutineDto>(`${this.BASE_PATH}/${id}`);
    return response.data;
  }

  /**
   * Create a new routine
   */
  static async createRoutine(request: CreateRoutineRequest): Promise<RoutineDto> {
    const response = await api.post<RoutineDto>(this.BASE_PATH, request);
    return response.data;
  }

  /**
   * Update an existing routine
   */
  static async updateRoutine(id: string, request: UpdateRoutineRequest): Promise<RoutineDto> {
    const response = await api.put<RoutineDto>(`${this.BASE_PATH}/${id}`, request);
    return response.data;
  }

  /**
   * Delete a routine
   */
  static async deleteRoutine(id: string): Promise<void> {
    await api.delete(`${this.BASE_PATH}/${id}`);
  }

  /**
   * Enable a routine
   */
  static async enableRoutine(id: string): Promise<RoutineDto> {
    const response = await api.patch<RoutineDto>(`${this.BASE_PATH}/${id}/enable`);
    return response.data;
  }

  /**
   * Disable a routine
   */
  static async disableRoutine(id: string): Promise<RoutineDto> {
    const response = await api.patch<RoutineDto>(`${this.BASE_PATH}/${id}/disable`);
    return response.data;
  }

  /**
   * Execute a routine manually
   */
  static async executeRoutine(id: string): Promise<void> {
    await api.post(`${this.BASE_PATH}/${id}/execute`);
  }

  // Routine Steps methods

  /**
   * Get all steps for a routine
   */
  static async getRoutineSteps(routineId: string): Promise<RoutineStepDto[]> {
    const response = await api.get<RoutineStepDto[]>(`${this.BASE_PATH}/${routineId}/steps`);
    return response.data;
  }

  /**
   * Create a new step for a routine
   */
  static async createRoutineStep(routineId: string, request: CreateRoutineStepRequest): Promise<RoutineStepDto> {
    const response = await api.post<RoutineStepDto>(`${this.BASE_PATH}/${routineId}/steps`, request);
    return response.data;
  }

  /**
   * Update a routine step
   */
  static async updateRoutineStep(
    routineId: string,
    stepId: string,
    request: UpdateRoutineStepRequest
  ): Promise<RoutineStepDto> {
    const response = await api.put<RoutineStepDto>(`${this.BASE_PATH}/${routineId}/steps/${stepId}`, request);
    return response.data;
  }

  /**
   * Delete a routine step
   */
  static async deleteRoutineStep(routineId: string, stepId: string): Promise<void> {
    await api.delete(`${this.BASE_PATH}/${routineId}/steps/${stepId}`);
  }

  /**
   * Reorder routine steps
   */
  static async reorderSteps(routineId: string, stepIds: string[]): Promise<RoutineStepDto[]> {
    const response = await api.patch<RoutineStepDto[]>(`${this.BASE_PATH}/${routineId}/steps/reorder`, {
      stepIds
    });
    return response.data;
  }
}

export default RoutineService;
