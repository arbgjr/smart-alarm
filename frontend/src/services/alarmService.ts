import api from '../lib/api';

export interface AlarmDto {
  id: string;
  name: string;
  description?: string;
  triggerTime: string;
  isEnabled: boolean;
  isRecurring: boolean;
  recurrencePattern?: string;
  userId: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateAlarmRequest {
  name: string;
  description?: string;
  triggerTime: string;
  isRecurring?: boolean;
  recurrencePattern?: string;
}

export interface UpdateAlarmRequest {
  name?: string;
  description?: string;
  triggerTime?: string;
  isEnabled?: boolean;
  isRecurring?: boolean;
  recurrencePattern?: string;
}

export interface PaginatedResponse<T> {
  data: T[];
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  totalElements: number;
  hasNext: boolean;
  hasPrevious: boolean;
}

export interface AlarmQueryParams {
  pageNumber?: number;
  pageSize?: number;
  isEnabled?: boolean;
  sortBy?: 'name' | 'triggerTime' | 'createdAt';
  sortDirection?: 'asc' | 'desc';
}

class AlarmService {
  private readonly baseUrl = '/api/alarms';

  async getAlarms(params?: AlarmQueryParams): Promise<PaginatedResponse<AlarmDto>> {
    const searchParams = new URLSearchParams();

    if (params?.pageNumber) searchParams.append('pageNumber', params.pageNumber.toString());
    if (params?.pageSize) searchParams.append('pageSize', params.pageSize.toString());
    if (params?.isEnabled !== undefined) searchParams.append('isEnabled', params.isEnabled.toString());
    if (params?.sortBy) searchParams.append('sortBy', params.sortBy);
    if (params?.sortDirection) searchParams.append('sortDirection', params.sortDirection);

    const url = searchParams.toString()
      ? `${this.baseUrl}?${searchParams.toString()}`
      : this.baseUrl;

    const response = await api.get<PaginatedResponse<AlarmDto>>(url);
    return response.data;
  }

  async getAlarm(id: string): Promise<AlarmDto> {
    const response = await api.get<AlarmDto>(`${this.baseUrl}/${id}`);
    return response.data;
  }

  async createAlarm(alarm: CreateAlarmRequest): Promise<AlarmDto> {
    const response = await api.post<AlarmDto>(this.baseUrl, alarm);
    return response.data;
  }

  async updateAlarm(id: string, alarm: UpdateAlarmRequest): Promise<AlarmDto> {
    const response = await api.put<AlarmDto>(`${this.baseUrl}/${id}`, alarm);
    return response.data;
  }

  async deleteAlarm(id: string): Promise<void> {
    await api.delete(`${this.baseUrl}/${id}`);
  }

  async enableAlarm(id: string): Promise<AlarmDto> {
    const response = await api.put<AlarmDto>(`${this.baseUrl}/${id}/enable`);
    return response.data;
  }

  async disableAlarm(id: string): Promise<AlarmDto> {
    const response = await api.put<AlarmDto>(`${this.baseUrl}/${id}/disable`);
    return response.data;
  }

  async triggerAlarm(id: string): Promise<void> {
    await api.post(`${this.baseUrl}/${id}/trigger`);
  }

  async getActiveAlarms(): Promise<AlarmDto[]> {
    const response = await this.getAlarms({ isEnabled: true, pageSize: 100 });
    return response.data;
  }

  async getTodaysAlarms(): Promise<AlarmDto[]> {
    const today = new Date();
    const startOfDay = new Date(today.getFullYear(), today.getMonth(), today.getDate());
    const endOfDay = new Date(today.getFullYear(), today.getMonth(), today.getDate() + 1);

    const response = await this.getAlarms({
      isEnabled: true,
      pageSize: 100,
      sortBy: 'triggerTime',
      sortDirection: 'asc'
    });

    // Filter for today's alarms on client side
    // In a real implementation, you might want to add date range parameters to the API
    return response.data.filter(alarm => {
      const triggerTime = new Date(alarm.triggerTime);
      return triggerTime >= startOfDay && triggerTime < endOfDay;
    });
  }
}

export const alarmService = new AlarmService();
