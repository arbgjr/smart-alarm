import React, { ReactNode } from 'react';
// import { useViewport } from '../../utils/responsive';

interface ResponsiveLayoutProps {
  children: ReactNode;
  className?: string;
  maxWidth?: 'sm' | 'md' | 'lg' | 'xl' | '2xl' | 'full';
  padding?: 'none' | 'sm' | 'md' | 'lg';
  centered?: boolean;
}

export const ResponsiveLayout: React.FC<ResponsiveLayoutProps> = ({
  children,
  className = '',
  maxWidth = 'full',
  padding = 'md',
  centered = false
}) => {
  // const viewport = useViewport();

  const getMaxWidthClass = () => {
    switch (maxWidth) {
      case 'sm': return 'max-w-sm';
      case 'md': return 'max-w-md';
      case 'lg': return 'max-w-lg';
      case 'xl': return 'max-w-xl';
      case '2xl': return 'max-w-2xl';
      case 'full': return 'max-w-7xl';
      default: return 'max-w-7xl';
    }
  };

  const getPaddingClass = () => {
    switch (padding) {
      case 'none': return '';
      case 'sm': return 'px-2 sm:px-4';
      case 'md': return 'px-4 sm:px-6 lg:px-8';
      case 'lg': return 'px-6 sm:px-8 lg:px-12';
      default: return 'px-4 sm:px-6 lg:px-8';
    }
  };

  const baseClasses = [
    'w-full',
    getMaxWidthClass(),
    getPaddingClass(),
    centered ? 'mx-auto' : '',
    className
  ].filter(Boolean).join(' ');

  return (
    <div className={baseClasses}>
      {children}
    </div>
  );
};

interface ResponsiveGridProps {
  children: ReactNode;
  columns?: {
    sm?: number;
    md?: number;
    lg?: number;
    xl?: number;
  };
  gap?: 'sm' | 'md' | 'lg';
  className?: string;
}

export const ResponsiveGrid: React.FC<ResponsiveGridProps> = ({
  children,
  columns = { sm: 1, md: 2, lg: 3, xl: 4 },
  gap = 'md',
  className = ''
}) => {
  const getGridClass = () => {
    const colClasses = [];

    if (columns.sm) colClasses.push(`grid-cols-${columns.sm}`);
    if (columns.md) colClasses.push(`md:grid-cols-${columns.md}`);
    if (columns.lg) colClasses.push(`lg:grid-cols-${columns.lg}`);
    if (columns.xl) colClasses.push(`xl:grid-cols-${columns.xl}`);

    return colClasses.join(' ');
  };

  const getGapClass = () => {
    switch (gap) {
      case 'sm': return 'gap-2 md:gap-3';
      case 'md': return 'gap-4 md:gap-6';
      case 'lg': return 'gap-6 md:gap-8';
      default: return 'gap-4 md:gap-6';
    }
  };

  return (
    <div className={`grid ${getGridClass()} ${getGapClass()} ${className}`}>
      {children}
    </div>
  );
};

interface ResponsiveStackProps {
  children: ReactNode;
  direction?: 'vertical' | 'horizontal' | 'responsive';
  spacing?: 'sm' | 'md' | 'lg';
  align?: 'start' | 'center' | 'end' | 'stretch';
  justify?: 'start' | 'center' | 'end' | 'between' | 'around';
  className?: string;
}

export const ResponsiveStack: React.FC<ResponsiveStackProps> = ({
  children,
  direction = 'vertical',
  spacing = 'md',
  align = 'stretch',
  justify = 'start',
  className = ''
}) => {
  const getDirectionClass = () => {
    switch (direction) {
      case 'vertical': return 'flex flex-col';
      case 'horizontal': return 'flex flex-row';
      case 'responsive': return 'flex flex-col md:flex-row';
      default: return 'flex flex-col';
    }
  };

  const getSpacingClass = () => {
    if (direction === 'horizontal' || direction === 'responsive') {
      switch (spacing) {
        case 'sm': return 'space-x-2 md:space-x-3 space-y-2 md:space-y-0';
        case 'md': return 'space-x-4 md:space-x-6 space-y-4 md:space-y-0';
        case 'lg': return 'space-x-6 md:space-x-8 space-y-6 md:space-y-0';
        default: return 'space-x-4 md:space-x-6 space-y-4 md:space-y-0';
      }
    } else {
      switch (spacing) {
        case 'sm': return 'space-y-2';
        case 'md': return 'space-y-4';
        case 'lg': return 'space-y-6';
        default: return 'space-y-4';
      }
    }
  };

  const getAlignClass = () => {
    switch (align) {
      case 'start': return 'items-start';
      case 'center': return 'items-center';
      case 'end': return 'items-end';
      case 'stretch': return 'items-stretch';
      default: return 'items-stretch';
    }
  };

  const getJustifyClass = () => {
    switch (justify) {
      case 'start': return 'justify-start';
      case 'center': return 'justify-center';
      case 'end': return 'justify-end';
      case 'between': return 'justify-between';
      case 'around': return 'justify-around';
      default: return 'justify-start';
    }
  };

  return (
    <div className={`${getDirectionClass()} ${getSpacingClass()} ${getAlignClass()} ${getJustifyClass()} ${className}`}>
      {children}
    </div>
  );
};

interface ResponsiveCardProps {
  children: ReactNode;
  padding?: 'sm' | 'md' | 'lg';
  shadow?: 'none' | 'sm' | 'md' | 'lg';
  border?: boolean;
  rounded?: 'none' | 'sm' | 'md' | 'lg';
  className?: string;
}

export const ResponsiveCard: React.FC<ResponsiveCardProps> = ({
  children,
  padding = 'md',
  shadow = 'sm',
  border = true,
  rounded = 'md',
  className = ''
}) => {
  const getPaddingClass = () => {
    switch (padding) {
      case 'sm': return 'p-3 md:p-4';
      case 'md': return 'p-4 md:p-6';
      case 'lg': return 'p-6 md:p-8';
      default: return 'p-4 md:p-6';
    }
  };

  const getShadowClass = () => {
    switch (shadow) {
      case 'none': return '';
      case 'sm': return 'shadow-sm';
      case 'md': return 'shadow-md';
      case 'lg': return 'shadow-lg';
      default: return 'shadow-sm';
    }
  };

  const getRoundedClass = () => {
    switch (rounded) {
      case 'none': return '';
      case 'sm': return 'rounded-sm';
      case 'md': return 'rounded-lg';
      case 'lg': return 'rounded-xl';
      default: return 'rounded-lg';
    }
  };

  const baseClasses = [
    'bg-white',
    getPaddingClass(),
    getShadowClass(),
    border ? 'border border-gray-200' : '',
    getRoundedClass(),
    'transition-shadow hover:shadow-md',
    className
  ].filter(Boolean).join(' ');

  return (
    <div className={baseClasses}>
      {children}
    </div>
  );
};

interface ResponsiveTextProps {
  children: ReactNode;
  variant?: 'h1' | 'h2' | 'h3' | 'h4' | 'body' | 'caption' | 'small';
  color?: 'primary' | 'secondary' | 'muted' | 'error' | 'success' | 'warning';
  align?: 'left' | 'center' | 'right';
  className?: string;
}

export const ResponsiveText: React.FC<ResponsiveTextProps> = ({
  children,
  variant = 'body',
  color = 'primary',
  align = 'left',
  className = ''
}) => {
  const getVariantClass = () => {
    switch (variant) {
      case 'h1': return 'text-2xl md:text-3xl lg:text-4xl font-bold';
      case 'h2': return 'text-xl md:text-2xl lg:text-3xl font-bold';
      case 'h3': return 'text-lg md:text-xl lg:text-2xl font-semibold';
      case 'h4': return 'text-base md:text-lg lg:text-xl font-semibold';
      case 'body': return 'text-sm md:text-base';
      case 'caption': return 'text-xs md:text-sm';
      case 'small': return 'text-xs';
      default: return 'text-sm md:text-base';
    }
  };

  const getColorClass = () => {
    switch (color) {
      case 'primary': return 'text-gray-900';
      case 'secondary': return 'text-gray-700';
      case 'muted': return 'text-gray-600';
      case 'error': return 'text-red-600';
      case 'success': return 'text-green-600';
      case 'warning': return 'text-yellow-600';
      default: return 'text-gray-900';
    }
  };

  const getAlignClass = () => {
    switch (align) {
      case 'left': return 'text-left';
      case 'center': return 'text-center';
      case 'right': return 'text-right';
      default: return 'text-left';
    }
  };

  const Tag = variant.startsWith('h') ? variant as keyof JSX.IntrinsicElements : 'p';

  return (
    <Tag className={`${getVariantClass()} ${getColorClass()} ${getAlignClass()} ${className}`}>
      {children}
    </Tag>
  );
};
