import React from 'react';

type FilterOption = {
  value: string;
  label: string;
};

interface FilterPanelProps {
  options: FilterOption[];
  value: string;
  onChange: (value: string) => void;
}

export function FilterPanel({ options, value, onChange }: FilterPanelProps) {
  return (
    <div className="flex space-x-1 rounded-lg bg-gray-200 p-1" role="group">
      {options.map((option) => (
        <button
          key={option.value}
          onClick={() => onChange(option.value)}
          className={`w-full rounded-md px-3 py-1.5 text-sm font-medium transition-colors focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 ${
            value === option.value
              ? 'bg-white text-gray-900 shadow'
              : 'text-gray-600 hover:bg-white/60'
          }`}
        >
          {option.label}
        </button>
      ))}
    </div>
  );
}
