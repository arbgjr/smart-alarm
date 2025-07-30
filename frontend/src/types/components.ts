import { ReactNode } from 'react';

// Component prop types and common interfaces
export interface BaseComponentProps {
  className?: string;
  children?: ReactNode;
  id?: string;
  'data-testid'?: string;
}

// Button variants and states
export interface ButtonProps extends BaseComponentProps {
  variant?: 'primary' | 'secondary' | 'tertiary' | 'danger' | 'ghost';
  size?: 'sm' | 'md' | 'lg' | 'xl';
  disabled?: boolean;
  loading?: boolean;
  fullWidth?: boolean;
  type?: 'button' | 'submit' | 'reset';
  onClick?: () => void;
  ariaLabel?: string;
  icon?: ReactNode;
  iconPosition?: 'left' | 'right';
}

// Input field types
export interface InputProps extends BaseComponentProps {
  type?: 'text' | 'email' | 'password' | 'number' | 'tel' | 'url' | 'search';
  value?: string;
  defaultValue?: string;
  placeholder?: string;
  disabled?: boolean;
  required?: boolean;
  error?: string;
  hint?: string;
  label?: string;
  name?: string;
  autoComplete?: string;
  maxLength?: number;
  minLength?: number;
  pattern?: string;
  onChange?: (value: string) => void;
  onBlur?: () => void;
  onFocus?: () => void;
}

// Form types
export interface FormFieldProps extends BaseComponentProps {
  label?: string;
  error?: string;
  hint?: string;
  required?: boolean;
  disabled?: boolean;
}

// Modal and overlay types
export interface ModalProps extends BaseComponentProps {
  open: boolean;
  onClose: () => void;
  title?: string;
  size?: 'sm' | 'md' | 'lg' | 'xl' | 'full';
  closable?: boolean;
  maskClosable?: boolean;
  keyboard?: boolean; // ESC key to close
}

// Card component
export interface CardProps extends BaseComponentProps {
  variant?: 'default' | 'outlined' | 'elevated' | 'filled';
  padding?: 'none' | 'sm' | 'md' | 'lg';
  clickable?: boolean;
  onClick?: () => void;
}

// List and table types
export interface ListItemProps extends BaseComponentProps {
  selected?: boolean;
  disabled?: boolean;
  onClick?: () => void;
  startIcon?: ReactNode;
  endIcon?: ReactNode;
}

export interface TableColumn<T = any> {
  key: string;
  title: string;
  dataIndex?: keyof T;
  render?: (value: any, record: T, index: number) => ReactNode;
  sortable?: boolean;
  width?: string | number;
  align?: 'left' | 'center' | 'right';
  fixed?: 'left' | 'right';
}

export interface TableProps<T = any> extends BaseComponentProps {
  columns: TableColumn<T>[];
  data: T[];
  loading?: boolean;
  pagination?: {
    current: number;
    pageSize: number;
    total: number;
    onChange: (page: number, pageSize: number) => void;
  };
  rowKey?: string | ((record: T) => string);
  onRowClick?: (record: T) => void;
  selectedRowKeys?: string[];
  onSelectionChange?: (selectedKeys: string[]) => void;
}

// Navigation types
export interface NavigationItem {
  key: string;
  label: string;
  icon?: ReactNode;
  path?: string;
  children?: NavigationItem[];
  badge?: string | number;
  disabled?: boolean;
}

// Toast/notification types
export interface Toast {
  id: string;
  type: 'success' | 'error' | 'warning' | 'info';
  title: string;
  description?: string;
  duration?: number; // ms, 0 for persistent
  action?: {
    label: string;
    onClick: () => void;
  };
}

// Loading states
export interface LoadingState {
  loading: boolean;
  error?: string | null;
}

// Common component states
export type ComponentSize = 'sm' | 'md' | 'lg' | 'xl';
export type ComponentVariant = 'primary' | 'secondary' | 'tertiary' | 'danger' | 'ghost';
export type ThemeColor = 'primary' | 'secondary' | 'success' | 'warning' | 'error' | 'info';

// Accessibility helpers
export interface AriaProps {
  'aria-label'?: string;
  'aria-labelledby'?: string;
  'aria-describedby'?: string;
  'aria-expanded'?: boolean;
  'aria-hidden'?: boolean;
  'aria-disabled'?: boolean;
  'aria-pressed'?: boolean;
  'aria-selected'?: boolean;
  'aria-checked'?: boolean;
  'aria-current'?: 'page' | 'step' | 'location' | 'date' | 'time' | 'true' | 'false';
  role?: string;
  tabIndex?: number;
}

// Combined props for accessibility-ready components
export interface AccessibleComponentProps extends BaseComponentProps, AriaProps {}

// Time and date formatting
export interface TimeDisplayProps {
  time: string | Date;
  format?: '12h' | '24h';
  showSeconds?: boolean;
  showTimezone?: boolean;
  relative?: boolean; // "in 5 minutes", "2 hours ago"
}

// Form validation types
export interface ValidationRule {
  required?: boolean;
  minLength?: number;
  maxLength?: number;
  pattern?: RegExp;
  min?: number;
  max?: number;
  email?: boolean;
  custom?: (value: any) => string | boolean;
}

export interface FormField {
  name: string;
  value: any;
  error?: string;
  touched: boolean;
  rules?: ValidationRule[];
}

// Generic event handlers
export type ChangeHandler<T = string> = (value: T) => void;
export type ClickHandler = () => void;
export type SubmitHandler<T = any> = (data: T) => void | Promise<void>;
