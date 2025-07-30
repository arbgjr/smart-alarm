import React from 'react';
import { cva, type VariantProps } from 'class-variance-authority';
import { cn } from '../../../utils/cn';

const spinnerVariants = cva(
  [
    'animate-spin rounded-full border-solid',
    'border-current border-r-transparent',
  ],
  {
    variants: {
      size: {
        sm: 'h-4 w-4 border-2',
        default: 'h-6 w-6 border-2',
        lg: 'h-8 w-8 border-2',
        xl: 'h-12 w-12 border-4',
      },
      variant: {
        primary: 'text-primary-600',
        secondary: 'text-secondary-600',
        neutral: 'text-neutral-600',
        white: 'text-white',
      },
    },
    defaultVariants: {
      size: 'default',
      variant: 'primary',
    },
  }
);

export interface LoadingSpinnerProps
  extends Omit<React.HTMLAttributes<HTMLDivElement>, 'children'>,
    VariantProps<typeof spinnerVariants> {
  /** Loading message for screen readers */
  label?: string;
  /** Whether to show loading text */
  showText?: boolean;
}

export const LoadingSpinner = React.forwardRef<HTMLDivElement, LoadingSpinnerProps>(
  ({
    className,
    size,
    variant,
    label = 'Loading...',
    showText = false,
    ...props
  }, ref) => {
    return (
      <div
        ref={ref}
        className={cn('flex items-center justify-center', className)}
        {...props}
      >
        <div className="flex items-center space-x-2">
          <div
            className={cn(spinnerVariants({ size, variant }))}
            role="status"
            aria-label={label}
            aria-live="polite"
          >
            <span className="sr-only">{label}</span>
          </div>

          {showText && (
            <span className="text-sm text-neutral-600" aria-hidden="true">
              {label}
            </span>
          )}
        </div>
      </div>
    );
  }
);

LoadingSpinner.displayName = 'LoadingSpinner';

// Full page loading component
export interface FullPageLoadingProps {
  /** Loading message */
  message?: string;
  /** Custom spinner size */
  size?: VariantProps<typeof spinnerVariants>['size'];
}

export const FullPageLoading: React.FC<FullPageLoadingProps> = ({
  message = 'Loading application...',
  size = 'xl',
}) => {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-white/80 backdrop-blur-sm">
      <div className="text-center">
        <LoadingSpinner
          size={size}
          variant="primary"
          label={message}
          className="mb-4"
        />
        <p className="text-sm text-neutral-600">{message}</p>
      </div>
    </div>
  );
};
