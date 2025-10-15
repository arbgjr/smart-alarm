import React, { useState, useEffect } from 'react';
import { X, Calendar, Globe, Plus, Trash2 } from 'lucide-react';

interface Holiday {
  id?: string;
  name: string;
  date: string;
  isRecurring: boolean;
  country?: string;
  region?: string;
  type: 'national' | 'regional' | 'personal' | 'religious';
}

interface HolidayPreference {
  disableAlarms: boolean;
  modifyTime?: string;
  affectedAlarmIds?: string[];
}

interface HolidayConfigModalProps {
  isOpen: boolean;
  holidays: Holiday[];
  preferences: HolidayPreference;
  alarms: Array<{ id: string; name: string; time: string }>;
  onSave: (holidays: Holiday[], preferences: HolidayPreference) => void;
  onCancel: () => void;
}

export const HolidayConfigModal: React.FC<HolidayConfigModalProps> = ({
  isOpen,
  holidays,
  preferences,
  alarms,
  onSave,
  onCancel
}) => {
  const [holidayList, setHolidayList] = useState<Holiday[]>(holidays);
  const [holidayPrefs, setHolidayPrefs] = useState<HolidayPreference>(preferences);
  const [newHoliday, setNewHoliday] = useState<Omit<Holiday, 'id'>>({
    name: '',
    date: new Date().toISOString().split('T')[0],
    isRecurring: false,
    country: '',
    region: '',
    type: 'personal'
  });

  useEffect(() => {
    setHolidayList(holidays);
    setHolidayPrefs(preferences);
  }, [holidays, preferences, isOpen]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSave(holidayList, holidayPrefs);
  };

  const addHoliday = () => {
    if (newHoliday.name && newHoliday.date) {
      setHolidayList(prev => [...prev, { ...newHoliday, id: Date.now().toString() }]);
      setNewHoliday({
        name: '',
        date: new Date().toISOString().split('T')[0],
        isRecurring: false,
        country: '',
        region: '',
        type: 'personal'
      });
    }
  };

  const removeHoliday = (id: string) => {
    setHolidayList(prev => prev.filter(h => h.id !== id));
  };

  const toggleAlarmSelection = (alarmId: string) => {
    setHolidayPrefs(prev => ({
      ...prev,
      affectedAlarmIds: prev.affectedAlarmIds?.includes(alarmId)
        ? prev.affectedAlarmIds.filter(id => id !== alarmId)
        : [...(prev.affectedAlarmIds || []), alarmId]
    }));
  };

  const loadCommonHolidays = () => {
    const commonHolidays: Omit<Holiday, 'id'>[] = [
      { name: 'New Year\'s Day', date: '2024-01-01', isRecurring: true, type: 'national' },
      { name: 'Independence Day', date: '2024-07-04', isRecurring: true, type: 'national' },
      { name: 'Christmas Day', date: '2024-12-25', isRecurring: true, type: 'national' },
      { name: 'Thanksgiving', date: '2024-11-28', isRecurring: true, type: 'national' },
      { name: 'Labor Day', date: '2024-09-02', isRecurring: true, type: 'national' }
    ];

    const newHolidays = commonHolidays.map(h => ({ ...h, id: Date.now().toString() + Math.random() }));
    setHolidayList(prev => [...prev, ...newHolidays]);
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-lg shadow-xl max-w-4xl w-full max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between p-6 border-b border-gray-200">
          <h2 className="text-xl font-semibold text-gray-900">
            Holiday Configuration
          </h2>
          <button
            onClick={onCancel}
            className="text-gray-400 hover:text-gray-600 transition-colors"
          >
            <X className="w-6 h-6" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="p-6 space-y-6">
          {/* Holiday Preferences */}
          <div className="bg-gray-50 rounded-lg p-4">
            <h3 className="text-lg font-medium text-gray-900 mb-4">Holiday Preferences</h3>

            <div className="space-y-4">
              <div className="flex items-center">
                <input
                  type="checkbox"
                  id="disableAlarms"
                  checked={holidayPrefs.disableAlarms}
                  onChange={(e) => setHolidayPrefs(prev => ({ ...prev, disableAlarms: e.target.checked }))}
                  className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                />
                <label htmlFor="disableAlarms" className="ml-2 block text-sm text-gray-900">
                  Disable alarms on holidays
                </label>
              </div>

              {!holidayPrefs.disableAlarms && (
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Modify alarm time on holidays
                  </label>
                  <input
                    type="time"
                    value={holidayPrefs.modifyTime || ''}
                    onChange={(e) => setHolidayPrefs(prev => ({ ...prev, modifyTime: e.target.value }))}
                    className="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                  />
                </div>
              )}

              {!holidayPrefs.disableAlarms && (
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Affected Alarms
                  </label>
                  <div className="max-h-32 overflow-y-auto border border-gray-300 rounded-md p-3 space-y-2">
                    {alarms.map((alarm) => (
                      <label key={alarm.id} className="flex items-center space-x-3 cursor-pointer">
                        <input
                          type="checkbox"
                          checked={holidayPrefs.affectedAlarmIds?.includes(alarm.id) || false}
                          onChange={() => toggleAlarmSelection(alarm.id)}
                          className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                        />
                        <div className="flex-1">
                          <span className="text-sm font-medium text-gray-900">{alarm.name}</span>
                          <span className="text-sm text-gray-600 ml-2">({alarm.time})</span>
                        </div>
                      </label>
                    ))}
                  </div>
                </div>
              )}
            </div>
          </div>

          {/* Add New Holiday */}
          <div className="bg-blue-50 rounded-lg p-4">
            <h3 className="text-lg font-medium text-gray-900 mb-4">Add New Holiday</h3>

            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 mb-4">
              <input
                type="text"
                placeholder="Holiday name"
                value={newHoliday.name}
                onChange={(e) => setNewHoliday(prev => ({ ...prev, name: e.target.value }))}
                className="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              />

              <input
                type="date"
                value={newHoliday.date}
                onChange={(e) => setNewHoliday(prev => ({ ...prev, date: e.target.value }))}
                className="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              />

              <select
                value={newHoliday.type}
                onChange={(e) => setNewHoliday(prev => ({ ...prev, type: e.target.value as any }))}
                className="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              >
                <option value="personal">Personal</option>
                <option value="national">National</option>
                <option value="regional">Regional</option>
                <option value="religious">Religious</option>
              </select>
            </div>

            <div className="flex items-center justify-between">
              <label className="flex items-center">
                <input
                  type="checkbox"
                  checked={newHoliday.isRecurring}
                  onChange={(e) => setNewHoliday(prev => ({ ...prev, isRecurring: e.target.checked }))}
                  className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                />
                <span className="ml-2 text-sm text-gray-900">Recurring annually</span>
              </label>

              <div className="flex space-x-2">
                <button
                  type="button"
                  onClick={loadCommonHolidays}
                  className="inline-flex items-center px-3 py-1 text-sm bg-gray-600 text-white rounded-md hover:bg-gray-700 transition-colors"
                >
                  <Globe className="w-4 h-4 mr-1" />
                  Load Common
                </button>
                <button
                  type="button"
                  onClick={addHoliday}
                  className="inline-flex items-center px-3 py-1 text-sm bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors"
                >
                  <Plus className="w-4 h-4 mr-1" />
                  Add Holiday
                </button>
              </div>
            </div>
          </div>

          {/* Holiday List */}
          <div>
            <h3 className="text-lg font-medium text-gray-900 mb-4">Configured Holidays</h3>

            {holidayList.length === 0 ? (
              <div className="text-center py-8 text-gray-500">
                <Calendar className="w-8 h-8 mx-auto mb-2 text-gray-300" />
                <p>No holidays configured yet.</p>
              </div>
            ) : (
              <div className="space-y-2 max-h-60 overflow-y-auto">
                {holidayList.map((holiday) => (
                  <div key={holiday.id} className="flex items-center justify-between p-3 border border-gray-200 rounded-md">
                    <div className="flex-1">
                      <div className="flex items-center space-x-3">
                        <span className="font-medium text-gray-900">{holiday.name}</span>
                        <span className="text-sm text-gray-600">
                          {new Date(holiday.date).toLocaleDateString()}
                        </span>
                        <span className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${
                          holiday.type === 'national' ? 'bg-red-100 text-red-800' :
                          holiday.type === 'regional' ? 'bg-blue-100 text-blue-800' :
                          holiday.type === 'religious' ? 'bg-purple-100 text-purple-800' :
                          'bg-gray-100 text-gray-800'
                        }`}>
                          {holiday.type}
                        </span>
                        {holiday.isRecurring && (
                          <span className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-green-100 text-green-800">
                            Recurring
                          </span>
                        )}
                      </div>
                    </div>
                    <button
                      type="button"
                      onClick={() => removeHoliday(holiday.id!)}
                      className="p-1 text-red-600 hover:text-red-800 transition-colors"
                    >
                      <Trash2 className="w-4 h-4" />
                    </button>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Actions */}
          <div className="flex items-center justify-end space-x-3 pt-6 border-t border-gray-200">
            <button
              type="button"
              onClick={onCancel}
              className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              className="px-4 py-2 text-sm font-medium text-white bg-blue-600 border border-transparent rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors"
            >
              Save Configuration
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
