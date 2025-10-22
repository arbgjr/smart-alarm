// Exportar o serviço e tipos existentes
export { default as RoutineService } from './routineService';
export type {
  RoutineDto,
  CreateRoutineRequest,
  UpdateRoutineRequest,
  RoutineListResponse,
  RoutineFilters,
  CreateRoutinePayload,
  UpdateRoutinePayload,
  PaginatedRoutinesResponse,
} from './routineService';

// Tipos relacionados a Steps não implementados ainda no backend
// export type { RoutineStepDto, CreateRoutineStepRequest, UpdateRoutineStepRequest } from './routineService';
