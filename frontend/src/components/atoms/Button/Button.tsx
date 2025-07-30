import React from 'react';
import { cva, type VariantProps } from 'class-variance-authority';
import { cn } from '../../../utils/cn';

const buttonVariants = cva(
  // Base styles - accessibility and interaction
  [
    'inline-flex items-center justify-center rounded-md text-sm font-medium',
    'ring-offset-background transition-colors duration-200',
    'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2',
    'disabled:pointer-events-none disabled:opacity-50',
    'active:scale-95 transform transition-transform',
  ],
  {
    variants: {
      variant: {
        // Primary - main actions
        primary: [
          'bg-gradient-to-r from-primary-600 to-primary-700',
          'text-white shadow-md',
          'hover:from-primary-700 hover:to-primary-800',
          'hover:shadow-lg',
        ],
        // Secondary - secondary actions
        secondary: [
          'bg-secondary-100 text-secondary-700',
          'border border-secondary-200',
          'hover:bg-secondary-200 hover:border-secondary-300',
        ],
        // Destructive - dangerous actions
        destructive: [
          'bg-gradient-to-r from-red-600 to-red-700',
          'text-white shadow-md',
          'hover:from-red-700 hover:to-red-800',
          'hover:shadow-lg',
        ],
        // Outline - subtle actions
        outline: [
          'border border-neutral-200 bg-transparent',
          'text-neutral-700',
          'hover:bg-neutral-50 hover:border-neutral-300',
        ],
        // Ghost - minimal actions
        ghost: [
          'text-neutral-600',
          'hover:bg-neutral-100 hover:text-neutral-900',
        ],
        // Link - text-like actions
        link: [
          'text-primary-600 underline-offset-4',
          'hover:underline hover:text-primary-700',
        ],
      },
      size: {
        sm: 'h-9 rounded-md px-3 text-xs',
        default: 'h-10 px-4 py-2',
        lg: 'h-11 rounded-md px-8 text-base',
        icon: 'h-10 w-10',
      },
    },
    defaultVariants: {
      variant: 'primary',
      size: 'default',
    },
  }
);

export interface ButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement>,
    VariantProps<typeof buttonVariants> {
  /** Loading state shows spinner */
  loading?: boolean;
  /** Left icon component */
  leftIcon?: React.ReactNode;
  /** Right icon component */
  rightIcon?: React.ReactNode;
}

export const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  ({
    className,
    variant,
    size,
    loading = false,
    leftIcon,
    rightIcon,
    children,
    disabled,
    ...props
  }, ref) => {
    const isDisabled = disabled || loading;

    return (
      <button
        className={cn(buttonVariants({ variant, size, className }))}
        ref={ref}
        disabled={isDisabled}
        aria-disabled={isDisabled}
        {...props}
      >
        {loading && (
          <svg
            className="mr-2 h-4 w-4 animate-spin"
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
            <circle
              className="opacity-25"
              cx="12"
              cy="12"
              r="10"
              stroke="currentColor"
              strokeWidth="4"
            />
            <path
              className="opacity-75"
              fill="currentColor"
              d="m4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
            />
          </svg>
        )}

        {!loading && leftIcon && (
          <span className="mr-2" aria-hidden="true">
            {leftIcon}
          </span>
        )}

        {children}

        {!loading && rightIcon && (
          <span className="ml-2" aria-hidden="true">
            {rightIcon}
          </span>
        )}
      </button>
    );
  }
);

Button.displayName = 'Button';
