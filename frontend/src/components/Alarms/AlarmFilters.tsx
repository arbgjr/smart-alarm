import React from 'react';
import { Search, Filter, Calendar, Clock } from 'lucide-react';

interface AlarmFiltersProps {
  searchTerm: string;
  onSearchChange: (term: string) => void;
  statusFilter: 'all' | 'active' | 'inactive';
  onStatusFilterChange: (status: 'all' | 'active' | 'inactive') => void;
  typeFilter: 'all' | 'one-time' | 'recurring';
  onTypeFilterChange: (type: 'all' | 'one-time' | 'recurring') => void;
  sortBy: 'time' | 'name' | 'created' | 'status';
  onSortChange: (sort: 'time' | 'name' | 'created' | 'status') => void;
  sortOrder: 'asc' | 'desc';
  onSortOrderChange: (order: 'asc' | 'desc') => void;
}

export const AlarmFilters: React.FC<AlarmFiltersProps> = ({
  searchTerm,
  onSearchChange,
  statusFilter,
  onStatusFilterChange,
  typeFilter,
  onTypeFilterChange,
  sortBy,
  onSortChange,
  sortOrder,
  onSortOrderChange
}) => {
  return (
    <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-4 mb-6">
      <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-4">
        {/* Search */}
        <div className="relative flex-1 max-w-md">
          <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
            <Search className="h-5 w-5 text-gray-400" />
          </div>
          <input
            type="text"
            placeholder="Search alarms..."
            value={searchTerm}
            onChange={(e) => onSearchChange(e.target.value)}
            className="block w-full pl-10 pr-3 py-2 border border-gray-300 rounded-md leading-5 bg-white placeholder-gray-500 focus:outline-none focus:placeholder-gray-400 focus:ring-1 focus:ring-blue-500 focus:border-blue-500"
          />
        </div>

        {/* Filters */}
        <div className="flex flex-wrap items-center gap-4">
          {/* Status Filter */}
          <div className="flex items-center space-x-2">
            <Filter className="h-4 w-4 text-gray-500" />
            <select
              value={statusFilter}
              onChange={(e) => onStatusFilterChange(e.target.value as any)}
              className="text-sm border border-gray-300 rounded-md px-3 py-1 focus:outline-none focus:ring-1 focus:ring-blue-500 focus:border-blue-500"
            >
              <option value="all">All Status</option>
              <option value="active">Active</option>
              <option value="inactive">Inactive</option>
            </select>
          </div>

          {/* Type Filter */}
          <div className="flex items-center space-x-2">
            <Calendar className="h-4 w-4 text-gray-500" />
            <select
              value={typeFilter}
              onChange={(e) => onTypeFilterChange(e.target.value as any)}
              className="text-sm border border-gray-300 rounded-md px-3 py-1 focus:outline-none focus:ring-1 focus:ring-blue-500 focus:border-blue-500"
            >
              <option value="all">All Types</option>
              <option value="one-time">One-time</option>
              <option value="recurring">Recurring</option>
            </select>
          </div>

          {/* Sort */}
          <div className="flex items-center space-x-2">
            <Clock className="h-4 w-4 text-gray-500" />
            <select
              value={sortBy}
              onChange={(e) => onSortChange(e.target.value as any)}
              className="text-sm border border-gray-300 rounded-md px-3 py-1 focus:outline-none focus:ring-1 focus:ring-blue-500 focus:border-blue-500"
            >
              <option value="time">Sort by Time</option>
              <option value="name">Sort by Name</option>
              <option value="created">Sort by Created</option>
              <option value="status">Sort by Status</option>
            </select>
          </div>

          {/* Sort Order */}
          <button
            onClick={() => onSortOrderChange(sortOrder === 'asc' ? 'desc' : 'asc')}
            className="p-1 text-gray-500 hover:text-gray-700 transition-colors"
            title={`Sort ${sortOrder === 'asc' ? 'Descending' : 'Ascending'}`}
          >
            {sortOrder === 'asc' ? '↑' : '↓'}
          </button>
        </div>
      </div>

      {/* Active Filters Display */}
      {(searchTerm || statusFilter !== 'all' || typeFilter !== 'all') && (
        <div className="mt-3 pt-3 border-t border-gray-200">
          <div className="flex flex-wrap items-center gap-2">
            <span className="text-sm text-gray-600">Active filters:</span>

            {searchTerm && (
              <span className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
                Search: "{searchTerm}"
                <button
                  onClick={() => onSearchChange('')}
                  className="ml-1 text-blue-600 hover:text-blue-800"
                >
                  ×
                </button>
              </span>
            )}

            {statusFilter !== 'all' && (
              <span className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-green-100 text-green-800">
                Status: {statusFilter}
                <button
                  onClick={() => onStatusFilterChange('all')}
                  className="ml-1 text-green-600 hover:text-green-800"
                >
                  ×
                </button>
              </span>
            )}

            {typeFilter !== 'all' && (
              <span className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-purple-100 text-purple-800">
                Type: {typeFilter}
                <button
                  onClick={() => onTypeFilterChange('all')}
                  className="ml-1 text-purple-600 hover:text-purple-800"
                >
                  ×
                </button>
              </span>
            )}

            <button
              onClick={() => {
                onSearchChange('');
                onStatusFilterChange('all');
                onTypeFilterChange('all');
              }}
              className="text-xs text-gray-500 hover:text-gray-700 underline"
            >
              Clear all
            </button>
          </div>
        </div>
      )}
    </div>
  );
};
