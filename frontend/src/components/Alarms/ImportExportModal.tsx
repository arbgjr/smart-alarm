import React, { useState, useRef } from 'react';
import { X, Upload, Download, FileText, AlertCircle, CheckCircle, Clock } from 'lucide-react';

interface ImportExportModalProps {
  isOpen: boolean;
  mode: 'import' | 'export';
  selectedAlarms?: any[];
  onImport?: (alarms: any[]) => void;
  onExport?: (format: 'csv' | 'json') => void;
  onCancel: () => void;
}

interface ImportResult {
  success: boolean;
  imported: number;
  errors: string[];
  warnings: string[];
}

export const ImportExportModal: React.FC<ImportExportModalProps> = ({
  isOpen,
  mode,
  selectedAlarms = [],
  onImport,
  onExport,
  onCancel
}) => {
  const [importFile, setImportFile] = useState<File | null>(null);
  const [importResult, setImportResult] = useState<ImportResult | null>(null);
  const [isProcessing, setIsProcessing] = useState(false);
  const [exportFormat, setExportFormat] = useState<'csv' | 'json'>('csv');
  const [includeInactive, setIncludeInactive] = useState(true);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleFileSelect = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      setImportFile(file);
      setImportResult(null);
    }
  };

  const handleImport = async () => {
    if (!importFile || !onImport) return;

    setIsProcessing(true);
    try {
      const text = await importFile.text();
      let alarms: any[] = [];
      const errors: string[] = [];
      const warnings: string[] = [];

      if (importFile.name.endsWith('.csv')) {
        // Parse CSV
        const lines = text.split('\n').filter(line => line.trim());
        const headers = lines[0].split(',').map(h => h.trim().toLowerCase());

        // Validate headers
        const requiredHeaders = ['name', 'time'];
        const missingHeaders = requiredHeaders.filter(h => !headers.includes(h));
        if (missingHeaders.length > 0) {
          errors.push(`Missing required columns: ${missingHeaders.join(', ')}`);
        } else {
          for (let i = 1; i < lines.length; i++) {
            const values = lines[i].split(',').map(v => v.trim());
            const alarm: any = {};

            headers.forEach((header, index) => {
              alarm[header] = values[index] || '';
            });

            // Validate alarm data
            if (!alarm.name) {
              warnings.push(`Row ${i + 1}: Missing alarm name`);
              continue;
            }

            if (!alarm.time || !/^\d{2}:\d{2}$/.test(alarm.time)) {
              warnings.push(`Row ${i + 1}: Invalid time format (use HH:MM)`);
              continue;
            }

            // Set defaults
            alarm.isActive = alarm.isactive === 'true' || alarm.isactive === '1' || true;
            alarm.isRecurring = alarm.isrecurring === 'true' || alarm.isrecurring === '1' || false;
            alarm.description = alarm.description || '';

            if (alarm.daysofweek) {
              alarm.daysOfWeek = alarm.daysofweek.split(';').map((d: string) => parseInt(d)).filter((d: number) => !isNaN(d));
            }

            alarms.push(alarm);
          }
        }
      } else if (importFile.name.endsWith('.json')) {
        // Parse JSON
        try {
          const data = JSON.parse(text);
          if (Array.isArray(data)) {
            alarms = data;
          } else if (data.alarms && Array.isArray(data.alarms)) {
            alarms = data.alarms;
          } else {
            errors.push('Invalid JSON format. Expected array of alarms or object with alarms property.');
          }
        } catch (e) {
          errors.push('Invalid JSON file format.');
        }
      } else {
        errors.push('Unsupported file format. Please use CSV or JSON files.');
      }

      const result: ImportResult = {
        success: errors.length === 0,
        imported: alarms.length,
        errors,
        warnings
      };

      setImportResult(result);

      if (result.success && alarms.length > 0) {
        onImport(alarms);
      }
    } catch (error) {
      setImportResult({
        success: false,
        imported: 0,
        errors: ['Failed to process file: ' + (error as Error).message],
        warnings: []
      });
    } finally {
      setIsProcessing(false);
    }
  };

  const handleExport = () => {
    if (onExport) {
      onExport(exportFormat);
    }
  };

  const generateSampleCSV = () => {
    const sampleData = [
      'name,time,description,isActive,isRecurring,daysOfWeek',
      'Morning Alarm,07:00,Wake up for work,true,true,1;2;3;4;5',
      'Weekend Sleep In,09:00,Relaxed weekend wake up,true,true,0;6',
      'Medication Reminder,20:00,Take evening medication,true,false,'
    ].join('\n');

    const blob = new Blob([sampleData], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'sample-alarms.csv';
    a.click();
    URL.revokeObjectURL(url);
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-lg shadow-xl max-w-2xl w-full max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between p-6 border-b border-gray-200">
          <h2 className="text-xl font-semibold text-gray-900">
            {mode === 'import' ? 'Import Alarms' : 'Export Alarms'}
          </h2>
          <button
            onClick={onCancel}
            className="text-gray-400 hover:text-gray-600 transition-colors"
          >
            <X className="w-6 h-6" />
          </button>
        </div>

        <div className="p-6">
          {mode === 'import' ? (
            <div className="space-y-6">
              {/* File Upload */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Select File to Import
                </label>
                <div className="mt-1 flex justify-center px-6 pt-5 pb-6 border-2 border-gray-300 border-dashed rounded-md hover:border-gray-400 transition-colors">
                  <div className="space-y-1 text-center">
                    <Upload className="mx-auto h-12 w-12 text-gray-400" />
                    <div className="flex text-sm text-gray-600">
                      <label
                        htmlFor="file-upload"
                        className="relative cursor-pointer bg-white rounded-md font-medium text-blue-600 hover:text-blue-500 focus-within:outline-none focus-within:ring-2 focus-within:ring-offset-2 focus-within:ring-blue-500"
                      >
                        <span>Upload a file</span>
                        <input
                          id="file-upload"
                          ref={fileInputRef}
                          name="file-upload"
                          type="file"
                          accept=".csv,.json"
                          className="sr-only"
                          onChange={handleFileSelect}
                        />
                      </label>
                      <p className="pl-1">or drag and drop</p>
                    </div>
                    <p className="text-xs text-gray-500">CSV or JSON files up to 10MB</p>
                  </div>
                </div>

                {importFile && (
                  <div className="mt-3 flex items-center space-x-2 text-sm text-gray-600">
                    <FileText className="w-4 h-4" />
                    <span>{importFile.name}</span>
                    <span>({(importFile.size / 1024).toFixed(1)} KB)</span>
                  </div>
                )}
              </div>

              {/* Sample File */}
              <div className="bg-blue-50 rounded-lg p-4">
                <h3 className="text-sm font-medium text-blue-900 mb-2">Need a template?</h3>
                <p className="text-sm text-blue-700 mb-3">
                  Download a sample CSV file to see the expected format.
                </p>
                <button
                  onClick={generateSampleCSV}
                  className="inline-flex items-center px-3 py-1 text-sm bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors"
                >
                  <Download className="w-4 h-4 mr-1" />
                  Download Sample CSV
                </button>
              </div>

              {/* Import Results */}
              {importResult && (
                <div className={`rounded-lg p-4 ${
                  importResult.success ? 'bg-green-50' : 'bg-red-50'
                }`}>
                  <div className="flex items-center space-x-2 mb-2">
                    {importResult.success ? (
                      <CheckCircle className="w-5 h-5 text-green-600" />
                    ) : (
                      <AlertCircle className="w-5 h-5 text-red-600" />
                    )}
                    <h3 className={`text-sm font-medium ${
                      importResult.success ? 'text-green-900' : 'text-red-900'
                    }`}>
                      {importResult.success ? 'Import Successful' : 'Import Failed'}
                    </h3>
                  </div>

                  {importResult.success && (
                    <p className="text-sm text-green-700">
                      Successfully imported {importResult.imported} alarm{importResult.imported !== 1 ? 's' : ''}.
                    </p>
                  )}

                  {importResult.errors.length > 0 && (
                    <div className="mt-2">
                      <h4 className="text-sm font-medium text-red-900">Errors:</h4>
                      <ul className="mt-1 text-sm text-red-700 list-disc list-inside">
                        {importResult.errors.map((error, index) => (
                          <li key={index}>{error}</li>
                        ))}
                      </ul>
                    </div>
                  )}

                  {importResult.warnings.length > 0 && (
                    <div className="mt-2">
                      <h4 className="text-sm font-medium text-yellow-900">Warnings:</h4>
                      <ul className="mt-1 text-sm text-yellow-700 list-disc list-inside">
                        {importResult.warnings.map((warning, index) => (
                          <li key={index}>{warning}</li>
                        ))}
                      </ul>
                    </div>
                  )}
                </div>
              )}

              {/* Actions */}
              <div className="flex items-center justify-end space-x-3 pt-4 border-t border-gray-200">
                <button
                  onClick={onCancel}
                  className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 transition-colors"
                >
                  Cancel
                </button>
                <button
                  onClick={handleImport}
                  disabled={!importFile || isProcessing}
                  className="inline-flex items-center px-4 py-2 text-sm font-medium text-white bg-blue-600 border border-transparent rounded-md hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                >
                  {isProcessing ? (
                    <>
                      <Clock className="w-4 h-4 mr-2 animate-spin" />
                      Processing...
                    </>
                  ) : (
                    <>
                      <Upload className="w-4 h-4 mr-2" />
                      Import Alarms
                    </>
                  )}
                </button>
              </div>
            </div>
          ) : (
            <div className="space-y-6">
              {/* Export Options */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Export Format
                </label>
                <div className="space-y-2">
                  <label className="flex items-center">
                    <input
                      type="radio"
                      name="format"
                      value="csv"
                      checked={exportFormat === 'csv'}
                      onChange={(e) => setExportFormat(e.target.value as 'csv')}
                      className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300"
                    />
                    <span className="ml-2 text-sm text-gray-900">CSV (Comma Separated Values)</span>
                  </label>
                  <label className="flex items-center">
                    <input
                      type="radio"
                      name="format"
                      value="json"
                      checked={exportFormat === 'json'}
                      onChange={(e) => setExportFormat(e.target.value as 'json')}
                      className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300"
                    />
                    <span className="ml-2 text-sm text-gray-900">JSON (JavaScript Object Notation)</span>
                  </label>
                </div>
              </div>

              {/* Export Options */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Export Options
                </label>
                <div className="space-y-2">
                  <label className="flex items-center">
                    <input
                      type="checkbox"
                      checked={includeInactive}
                      onChange={(e) => setIncludeInactive(e.target.checked)}
                      className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                    />
                    <span className="ml-2 text-sm text-gray-900">Include inactive alarms</span>
                  </label>
                </div>
              </div>

              {/* Export Summary */}
              <div className="bg-gray-50 rounded-lg p-4">
                <h3 className="text-sm font-medium text-gray-900 mb-2">Export Summary</h3>
                <div className="text-sm text-gray-600 space-y-1">
                  <p>Selected alarms: {selectedAlarms.length}</p>
                  <p>Format: {exportFormat.toUpperCase()}</p>
                  <p>Include inactive: {includeInactive ? 'Yes' : 'No'}</p>
                </div>
              </div>

              {/* Actions */}
              <div className="flex items-center justify-end space-x-3 pt-4 border-t border-gray-200">
                <button
                  onClick={onCancel}
                  className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 transition-colors"
                >
                  Cancel
                </button>
                <button
                  onClick={handleExport}
                  className="inline-flex items-center px-4 py-2 text-sm font-medium text-white bg-blue-600 border border-transparent rounded-md hover:bg-blue-700 transition-colors"
                >
                  <Download className="w-4 h-4 mr-2" />
                  Export Alarms
                </button>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
