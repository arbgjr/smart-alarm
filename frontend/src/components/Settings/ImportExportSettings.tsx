import React, { useState, useRef } from 'react';
import { Download, Upload, FileText, Calendar, Settings, Database, AlertCircle, CheckCircle, RefreshCw } from 'lucide-react';

interface ImportExportSettingsProps {
  onSettingsChange: () => void;
}

interface ExportOptions {
  alarms: boolean;
  routines: boolean;
  settings: boolean;
  history: boolean;
  format: 'json' | 'csv' | 'ical';
  dateRange: 'all' | 'last_month' | 'last_year' | 'custom';
  customStartDate?: string;
  customEndDate?: string;
}

interface ImportResult {
  success: boolean;
  message: string;
  imported: {
    alarms: number;
    routines: number;
    settings: number;
  };
  errors: string[];
}

export const ImportExportSettings: React.FC<ImportExportSettingsProps> = ({
  onSettingsChange
}) => {
  const [exportOptions, setExportOptions] = useState<ExportOptions>({
    alarms: true,
    routines: true,
    settings: false,
    history: false,
    format: 'json',
    dateRange: 'all'
  });

  const [isExporting, setIsExporting] = useState(false);
  const [isImporting, setIsImporting] = useState(false);
  const [importResult, setImportResult] = useState<ImportResult | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleExportOptionChange = (key: keyof ExportOptions, value: any) => {
    setExportOptions(prev => ({ ...prev, [key]: value }));
    onSettingsChange();
  };

  const handleExport = async () => {
    setIsExporting(true);

    try {
      // Simulate export process
      await new Promise(resolve => setTimeout(resolve, 2000));

      const exportData = {
        metadata: {
          exportDate: new Date().toISOString(),
          version: '1.0.0',
          options: exportOptions
        },
        data: {
          alarms: exportOptions.alarms ? generateMockAlarms() : [],
          routines: exportOptions.routines ? generateMockRoutines() : [],
          settings: exportOptions.settings ? generateMockSettings() : {},
          history: exportOptions.history ? generateMockHistory() : []
        }
      };

      // Create and download file
      const blob = new Blob([JSON.stringify(exportData, null, 2)], {
        type: exportOptions.format === 'json' ? 'application/json' : 'text/csv'
      });

      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `smart-alarm-export-${new Date().toISOString().split('T')[0]}.${exportOptions.format}`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);

    } catch (error) {
      console.error('Export failed:', error);
    } finally {
      setIsExporting(false);
    }
  };

  const handleImport = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    setIsImporting(true);
    setImportResult(null);

    try {
      const text = await file.text();
      let data;

      if (file.name.endsWith('.json')) {
        data = JSON.parse(text);
      } else if (file.name.endsWith('.csv')) {
        // Simple CSV parsing for demo
        data = { alarms: [], routines: [], settings: {} };
      } else {
        throw new Error('Unsupported file format');
      }

      // Simulate import process
      await new Promise(resolve => setTimeout(resolve, 1500));

      const result: ImportResult = {
        success: true,
        message: 'Import completed successfully',
        imported: {
          alarms: data.data?.alarms?.length || 0,
          routines: data.data?.routines?.length || 0,
          settings: Object.keys(data.data?.settings || {}).length
        },
        errors: []
      };

      setImportResult(result);
      onSettingsChange();

    } catch (error) {
      setImportResult({
        success: false,
        message: 'Import failed',
        imported: { alarms: 0, routines: 0, settings: 0 },
        errors: [error instanceof Error ? error.message : 'Unknown error']
      });
    } finally {
      setIsImporting(false);
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    }
  };

  const generateMockAlarms = () => [
    { id: '1', name: 'Morning Alarm', time: '07:00', isActive: true },
    { id: '2', name: 'Workout Reminder', time: '18:00', isActive: true }
  ];

  const generateMockRoutines = () => [
    { id: '1', name: 'Weekday Morning', pattern: 'weekly', daysOfWeek: [1, 2, 3, 4, 5] }
  ];

  const generateMockSettings = () => ({
    notifications: { pushEnabled: true, emailEnabled: false },
    preferences: { timezone: 'America/New_York', timeFormat: '12h' }
  });

  const generateMockHistory = () => [
    { date: '2024-01-15', triggered: 5, dismissed: 4, snoozed: 1 }
  ];

  const exportFormats = [
    { value: 'json', label: 'JSON', description: 'Complete data with full structure' },
    { value: 'csv', label: 'CSV', description: 'Spreadsheet-compatible format' },
    { value: 'ical', label: 'iCal', description: 'Calendar format for alarms' }
  ];

  const dateRanges = [
    { value: 'all', label: 'All Time' },
    { value: 'last_month', label: 'Last Month' },
    { value: 'last_year', label: 'Last Year' },
    { value: 'custom', label: 'Custom Range' }
  ];

  return (
    <div className="space-y-6">
      {/* Export Data */}
      <div className="bg-white shadow-sm rounded-lg border border-gray-200">
        <div className="px-6 py-4 border-b border-gray-200">
          <h3 className="text-lg font-medium text-gray-900">Export Data</h3>
          <p className="text-sm text-gray-600 mt-1">
            Download your alarm data for backup or transfer to another device
          </p>
        </div>

        <div className="p-6 space-y-6">
          {/* What to Export */}
          <div>
            <h4 className="text-md font-medium text-gray-900 mb-4">What to Export</h4>
            <div className="space-y-3">
              <label className="flex items-center space-x-3">
                <input
                  type="checkbox"
                  checked={exportOptions.alarms}
                  onChange={(e) => handleExportOptionChange('alarms', e.target.checked)}
                  className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                />
                <div className="flex items-center space-x-2">
                  <Calendar className="w-4 h-4 text-blue-600" />
                  <span className="text-sm font-medium text-gray-900">Alarms</span>
                </div>
                <span className="text-sm text-gray-600">All your alarm configurations</span>
              </label>

              <label className="flex items-center space-x-3">
                <input
                  type="checkbox"
                  checked={exportOptions.routines}
                  onChange={(e) => handleExportOptionChange('routines', e.target.checked)}
                  className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                />
                <div className="flex items-center space-x-2">
                  <RefreshCw className="w-4 h-4 text-green-600" />
                  <span className="text-sm font-medium text-gray-900">Routines</span>
                </div>
                <span className="text-sm text-gray-600">Recurring alarm patterns</span>
              </label>

              <label className="flex items-center space-x-3">
                <input
                  type="checkbox"
                  checked={exportOptions.settings}
                  onChange={(e) => handleExportOptionChange('settings', e.target.checked)}
                  className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                />
                <div className="flex items-center space-x-2">
                  <Settings className="w-4 h-4 text-purple-600" />
                  <span className="text-sm font-medium text-gray-900">Settings</span>
                </div>
                <span className="text-sm text-gray-600">App preferences and configurations</span>
              </label>

              <label className="flex items-center space-x-3">
                <input
                  type="checkbox"
                  checked={exportOptions.history}
                  onChange={(e) => handleExportOptionChange('history', e.target.checked)}
                  className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                />
                <div className="flex items-center space-x-2">
                  <Database className="w-4 h-4 text-orange-600" />
                  <span className="text-sm font-medium text-gray-900">History</span>
                </div>
                <span className="text-sm text-gray-600">Alarm usage statistics and logs</span>
              </label>
            </div>
          </div>

          {/* Export Format */}
          <div>
            <h4 className="text-md font-medium text-gray-900 mb-4">Export Format</h4>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
              {exportFormats.map(format => (
                <label key={format.value} className="cursor-pointer">
                  <input
                    type="radio"
                    name="format"
                    value={format.value}
                    checked={exportOptions.format === format.value}
                    onChange={(e) => handleExportOptionChange('format', e.target.value)}
                    className="sr-only"
                  />
                  <div className={`p-3 border rounded-lg transition-colors ${
                    exportOptions.format === format.value
                      ? 'border-blue-500 bg-blue-50'
                      : 'border-gray-300 hover:border-gray-400'
                  }`}>
                    <div className="flex items-center space-x-2 mb-1">
                      <FileText className="w-4 h-4" />
                      <span className="text-sm font-medium">{format.label}</span>
                    </div>
                    <p className="text-xs text-gray-600">{format.description}</p>
                  </div>
                </label>
              ))}
            </div>
          </div>

          {/* Date Range */}
          {exportOptions.history && (
            <div>
              <h4 className="text-md font-medium text-gray-900 mb-4">Date Range</h4>
              <div className="space-y-3">
                <select
                  value={exportOptions.dateRange}
                  onChange={(e) => handleExportOptionChange('dateRange', e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                >
                  {dateRanges.map(range => (
                    <option key={range.value} value={range.value}>
                      {range.label}
                    </option>
                  ))}
                </select>

                {exportOptions.dateRange === 'custom' && (
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">
                        Start Date
                      </label>
                      <input
                        type="date"
                        value={exportOptions.customStartDate || ''}
                        onChange={(e) => handleExportOptionChange('customStartDate', e.target.value)}
                        className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                      />
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">
                        End Date
                      </label>
                      <input
                        type="date"
                        value={exportOptions.customEndDate || ''}
                        onChange={(e) => handleExportOptionChange('customEndDate', e.target.value)}
                        className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                      />
                    </div>
                  </div>
                )}
              </div>
            </div>
          )}

          {/* Export Button */}
          <div className="pt-4 border-t border-gray-200">
            <button
              onClick={handleExport}
              disabled={isExporting || (!exportOptions.alarms && !exportOptions.routines && !exportOptions.settings && !exportOptions.history)}
              className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              {isExporting ? (
                <>
                  <RefreshCw className="w-4 h-4 mr-2 animate-spin" />
                  Exporting...
                </>
              ) : (
                <>
                  <Download className="w-4 h-4 mr-2" />
                  Export Data
                </>
              )}
            </button>
          </div>
        </div>
      </div>

      {/* Import Data */}
      <div className="bg-white shadow-sm rounded-lg border border-gray-200">
        <div className="px-6 py-4 border-b border-gray-200">
          <h3 className="text-lg font-medium text-gray-900">Import Data</h3>
          <p className="text-sm text-gray-600 mt-1">
            Restore your alarm data from a backup file
          </p>
        </div>

        <div className="p-6 space-y-6">
          {/* Import Instructions */}
          <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
            <div className="flex items-start space-x-3">
              <AlertCircle className="w-5 h-5 text-blue-600 mt-0.5" />
              <div>
                <h4 className="text-sm font-medium text-blue-900">Before You Import</h4>
                <ul className="text-sm text-blue-800 mt-2 space-y-1">
                  <li>• Importing will merge with your existing data</li>
                  <li>• Duplicate alarms will be skipped</li>
                  <li>• Settings will overwrite current preferences</li>
                  <li>• Create a backup before importing if needed</li>
                </ul>
              </div>
            </div>
          </div>

          {/* File Upload */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Select Import File
            </label>
            <div className="flex items-center space-x-4">
              <input
                ref={fileInputRef}
                type="file"
                accept=".json,.csv"
                onChange={handleImport}
                disabled={isImporting}
                className="block w-full text-sm text-gray-500 file:mr-4 file:py-2 file:px-4 file:rounded-md file:border-0 file:text-sm file:font-medium file:bg-blue-50 file:text-blue-700 hover:file:bg-blue-100 disabled:opacity-50"
              />
              {isImporting && (
                <div className="flex items-center space-x-2 text-blue-600">
                  <RefreshCw className="w-4 h-4 animate-spin" />
                  <span className="text-sm">Importing...</span>
                </div>
              )}
            </div>
            <p className="text-xs text-gray-600 mt-1">
              Supported formats: JSON, CSV (max 10MB)
            </p>
          </div>

          {/* Import Result */}
          {importResult && (
            <div className={`border rounded-lg p-4 ${
              importResult.success
                ? 'border-green-200 bg-green-50'
                : 'border-red-200 bg-red-50'
            }`}>
              <div className="flex items-start space-x-3">
                {importResult.success ? (
                  <CheckCircle className="w-5 h-5 text-green-600 mt-0.5" />
                ) : (
                  <AlertCircle className="w-5 h-5 text-red-600 mt-0.5" />
                )}
                <div className="flex-1">
                  <h4 className={`text-sm font-medium ${
                    importResult.success ? 'text-green-900' : 'text-red-900'
                  }`}>
                    {importResult.message}
                  </h4>

                  {importResult.success && (
                    <div className="mt-2 text-sm text-green-800">
                      <p>Successfully imported:</p>
                      <ul className="list-disc list-inside mt-1 space-y-1">
                        <li>{importResult.imported.alarms} alarms</li>
                        <li>{importResult.imported.routines} routines</li>
                        <li>{importResult.imported.settings} settings</li>
                      </ul>
                    </div>
                  )}

                  {importResult.errors.length > 0 && (
                    <div className="mt-2 text-sm text-red-800">
                      <p>Errors encountered:</p>
                      <ul className="list-disc list-inside mt-1 space-y-1">
                        {importResult.errors.map((error, index) => (
                          <li key={index}>{error}</li>
                        ))}
                      </ul>
                    </div>
                  )}
                </div>
              </div>
            </div>
          )}

          {/* Supported Formats */}
          <div>
            <h4 className="text-md font-medium text-gray-900 mb-3">Supported Import Formats</h4>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div className="border border-gray-200 rounded-lg p-3">
                <div className="flex items-center space-x-2 mb-2">
                  <FileText className="w-4 h-4 text-blue-600" />
                  <span className="text-sm font-medium">JSON Format</span>
                </div>
                <p className="text-xs text-gray-600">
                  Complete Smart Alarm export files with full data structure and metadata
                </p>
              </div>

              <div className="border border-gray-200 rounded-lg p-3">
                <div className="flex items-center space-x-2 mb-2">
                  <FileText className="w-4 h-4 text-green-600" />
                  <span className="text-sm font-medium">CSV Format</span>
                </div>
                <p className="text-xs text-gray-600">
                  Spreadsheet exports with alarm data (limited functionality)
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
