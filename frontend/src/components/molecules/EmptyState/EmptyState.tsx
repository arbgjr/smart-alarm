import React from 'react';
import { AlertCircle, RefreshCw } from 'lucide-react';
import { Button } from '../../atoms/Button/Button';

interface EmptyStateProps {
  icon?: React.ReactNode;
  title: string;
  description?: string;
  action?: {
    label: string;
    onClick: () => void;
  };
  className?: string;
}

export const EmptyState: React.FC<EmptyStateProps> = ({
  icon,
  title,
  description,
  action,
  className = ''
}) => {
  return (
    <div className={`flex flex-col items-center justify-center py-12 px-4 text-center ${className}`}>
      {icon && (
        <div className="mb-4 text-gray-400 dark:text-gray-500">
          {icon}
        </div>
      )}

      <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">
        {title}
      </h3>

      {description && (
        <p className="text-gray-600 dark:text-gray-400 mb-6 max-w-md">
          {description}
        </p>
      )}

      {action && (
        <Button
          variant="primary"
          onClick={action.onClick}
          className="min-w-[120px]"
        >
          {action.label}
        </Button>
      )}
    </div>
  );
};

// Pre-configured empty states for common scenarios
export const EmptyAlarmState: React.FC<{
  onCreateAlarm?: () => void;
}> = ({ onCreateAlarm }) => (
  <EmptyState
    icon={<AlertCircle size={48} />}
    title="No alarms yet"
    description="Create your first alarm to get started with Smart Alarm. You can set up reminders, recurring alarms, and more."
    action={onCreateAlarm ? {
      label: "Create Alarm",
      onClick: onCreateAlarm
    } : undefined}
  />
);

export const EmptyRoutineState: React.FC<{
  onCreateRoutine?: () => void;
}> = ({ onCreateRoutine }) => (
  <EmptyState
    icon={<RefreshCw size={48} />}
    title="No routines yet"
    description="Create your first routine to automate your daily tasks. Routines can include multiple alarms and actions."
    action={onCreateRoutine ? {
      label: "Create Routine",
      onClick: onCreateRoutine
    } : undefined}
  />
);

export const EmptySearchState: React.FC<{
  searchTerm: string;
  onClearSearch?: () => void;
}> = ({ searchTerm, onClearSearch }) => (
  <EmptyState
    icon={<AlertCircle size={48} />}
    title={`No results for "${searchTerm}"`}
    description="Try adjusting your search criteria or browse all items."
    action={onClearSearch ? {
      label: "Clear Search",
      onClick: onClearSearch
    } : undefined}
  />
);

export const LoadingFailedState: React.FC<{
  onRetry?: () => void;
  error?: string;
}> = ({ onRetry, error }) => (
  <EmptyState
    icon={<AlertCircle size={48} className="text-red-500" />}
    title="Failed to load data"
    description={error || "Something went wrong while loading. Please try again."}
    action={onRetry ? {
      label: "Try Again",
      onClick: onRetry
    } : undefined}
  />
);
