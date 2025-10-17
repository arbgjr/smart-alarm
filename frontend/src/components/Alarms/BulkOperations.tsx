import React, { useState } from 'react';
import {
  CheckSquare,
  Square,
  Power,
  PowerOff,
  Trash2,
  Copy,
  Download,
  Upload,
  MoreHorizontal,
  X
} from 'lucide-react';

interface BulkOperationsProps {
  selectedAlarms: string[];
  totalAlarms: number;
  onSelectAll: () => void;
  onDeselectAll: () => void;
  onBulkEnable: () => void;
  onBulkDisable: () => void;
  onBulkDelete: () => void;
  onBulkDuplicate: () => void;
  onExportSelected: () => void;
  onImportAlarms: () => void;
  className?: string;
}

export const BulkOperations: React.FC<BulkOperationsProps> = ({
  selectedAlarms,
  totalAlarms,
  onSelectAll,
  onDeselectAll,
  onBulkEnable,
  onBulkDisable,
  onBulkDelete,
  onBulkDuplicate,
  onExportSelected,
  onImportAlarms,
  className = ''
}) => {
  const [showActions, setShowActions] = useState(false);
  const hasSelection = selectedAlarms.length > 0;
  const isAllSelected = selectedAlarms.length === totalAlarms && totalAlarms > 0;

  const handleSelectToggle = () => {
    if (isAllSelected) {
      onDeselectAll();
    } else {
      onSelectAll();
    }
  };

  return (
    <div className={`bg-white border border-gray-200 rounded-lg p-4 ${className}`}>
      <div className="flex items-center justify-between">
        {/* Selection Controls */}
        <div className="flex items-center space-x-4">
          <button
            onClick={handleSelectToggle}
            className="flex items-center space-x-2 text-sm text-gray-600 hover:text-gray-900 transition-colors"
          >
            {isAllSelected ? (
              <CheckSquare className="w-4 h-4" />
            ) : (
              <Square className="w-4 h-4" />
            )}
            <span>
              {hasSelection
                ? `${selectedAlarms.length} of ${totalAlarms} selected`
                : `Select all ${totalAlarms} alarms`
              }
            </span>
          </button>

          {hasSelection && (
            <button
              onClick={onDeselectAll}
              className="flex items-center space-x-1 text-sm text-gray-500 hover:text-gray-700 transition-colors"
            >
              <X className="w-3 h-3" />
              <span>Clear</span>
            </button>
          )}
        </div>

        {/* Action Buttons */}
        <div className="flex items-center space-x-2">
          {/* Import/Export Actions */}
          <div className="flex items-center space-x-1 border-r border-gray-200 pr-2">
            <button
              onClick={onImportAlarms}
              className="inline-flex items-center px-3 py-1 text-sm text-gray-600 hover:text-gray-900 hover:bg-gray-50 rounded-md transition-colors"
              title="Import alarms from CSV"
            >
              <Upload className="w-4 h-4 mr-1" />
              Import
            </button>

            <button
              onClick={onExportSelected}
              disabled={!hasSelection}
              className={`inline-flex items-center px-3 py-1 text-sm rounded-md transition-colors ${
                hasSelection
                  ? 'text-gray-600 hover:text-gray-900 hover:bg-gray-50'
                  : 'text-gray-400 cursor-not-allowed'
              }`}
              title="Export selected alarms to CSV"
            >
              <Download className="w-4 h-4 mr-1" />
              Export
            </button>
          </div>

          {/* Bulk Actions */}
          {hasSelection && (
            <>
              <button
                onClick={onBulkEnable}
                className="inline-flex items-center px-3 py-1 text-sm text-green-600 hover:text-green-700 hover:bg-green-50 rounded-md transition-colors"
                title="Enable selected alarms"
              >
                <Power className="w-4 h-4 mr-1" />
                Enable
              </button>

              <button
                onClick={onBulkDisable}
                className="inline-flex items-center px-3 py-1 text-sm text-yellow-600 hover:text-yellow-700 hover:bg-yellow-50 rounded-md transition-colors"
                title="Disable selected alarms"
              >
                <PowerOff className="w-4 h-4 mr-1" />
                Disable
              </button>

              <button
                onClick={onBulkDuplicate}
                className="inline-flex items-center px-3 py-1 text-sm text-blue-600 hover:text-blue-700 hover:bg-blue-50 rounded-md transition-colors"
                title="Duplicate selected alarms"
              >
                <Copy className="w-4 h-4 mr-1" />
                Duplicate
              </button>

              <div className="relative">
                <button
                  onClick={() => setShowActions(!showActions)}
                  className="inline-flex items-center px-2 py-1 text-sm text-gray-600 hover:text-gray-900 hover:bg-gray-50 rounded-md transition-colors"
                >
                  <MoreHorizontal className="w-4 h-4" />
                </button>

                {showActions && (
                  <div className="absolute right-0 mt-2 w-48 bg-white rounded-md shadow-lg border border-gray-200 z-10">
                    <div className="py-1">
                      <button
                        onClick={() => {
                          onBulkDelete();
                          setShowActions(false);
                        }}
                        className="flex items-center w-full px-4 py-2 text-sm text-red-600 hover:bg-red-50"
                      >
                        <Trash2 className="w-4 h-4 mr-2" />
                        Delete Selected ({selectedAlarms.length})
                      </button>
                    </div>
                  </div>
                )}
              </div>
            </>
          )}
        </div>
      </div>

      {/* Selection Summary */}
      {hasSelection && (
        <div className="mt-3 pt-3 border-t border-gray-200">
          <div className="flex items-center justify-between text-sm text-gray-600">
            <span>
              {selectedAlarms.length} alarm{selectedAlarms.length !== 1 ? 's' : ''} selected
            </span>
            <div className="flex items-center space-x-4">
              <span>Quick actions:</span>
              <button
                onClick={onBulkEnable}
                className="text-green-600 hover:text-green-700 font-medium"
              >
                Enable All
              </button>
              <button
                onClick={onBulkDisable}
                className="text-yellow-600 hover:text-yellow-700 font-medium"
              >
                Disable All
              </button>
              <button
                onClick={onBulkDelete}
                className="text-red-600 hover:text-red-700 font-medium"
              >
                Delete All
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Click outside to close actions menu */}
      {showActions && (
        <div
          className="fixed inset-0 z-0"
          onClick={() => setShowActions(false)}
        />
      )}
    </div>
  );
};
