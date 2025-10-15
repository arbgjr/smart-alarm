import React, { useState, useEffect } from 'react';
import { useMLDataCollection } from '@/utils/mlDataCollector';
import { useAlarmOptimization } from '@/utils/alarmOptimizer';
import { useUIStore } from '@/stores/uiStore';
import { 
  ChartBarIcon, 
  LightBulbIcon, 
  ClockIcon,
  SparklesIcon,
  ShieldCheckIcon,
  TrashIcon,
  CogIcon,
  BellIcon
} from '@heroicons/react/24/outline';

interface SleepRecommendation {
  id: string;
  type: 'timing' | 'consistency' | 'duration' | 'environment';
  title: string;
  description: string;
  impact: 'high' | 'medium' | 'low';
  confidence: number;
  actionable: boolean;
}

export const SleepInsights: React.FC = () => {
  const { 
    isEnabled, 
    pendingCount, 
    enableCollection, 
    disableCollection, 
    getLocalAnalytics,
    exportData,
    deleteData,
    forceSync
  } = useMLDataCollection();
  
  const { getSmartRecommendation, calculateOptimalTime } = useAlarmOptimization();
  
  const { openModal } = useUIStore();
  const [analytics, setAnalytics] = useState<any>(null);
  const [recommendations, setRecommendations] = useState<SleepRecommendation[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [showPrivacySettings, setShowPrivacySettings] = useState(false);

  useEffect(() => {
    if (isEnabled) {
      const data = getLocalAnalytics();
      setAnalytics(data);
      if (data) {
        generateRecommendations(data);
      }
    }
  }, [isEnabled, getLocalAnalytics]);

  const generateRecommendations = (data: any): void => {
    const recommendations: SleepRecommendation[] = [];

    // Consistency recommendations
    if (data.sleepConsistency < 0.7) {
      recommendations.push({
        id: 'consistency-low',
        type: 'consistency',
        title: 'Improve Sleep Consistency',
        description: `Your sleep schedule varies by ${Math.round((1 - data.sleepConsistency) * 60)} minutes on average. Try going to bed and waking up at the same time daily.`,
        impact: 'high',
        confidence: 0.85,
        actionable: true,
      });
    }

    // Duration recommendations
    if (data.avgSleepDuration < 7 || data.avgSleepDuration > 9) {
      const recommendation = data.avgSleepDuration < 7 ? 'increase' : 'optimize';
      recommendations.push({
        id: 'duration-optimize',
        type: 'duration',
        title: `${recommendation === 'increase' ? 'Increase' : 'Optimize'} Sleep Duration`,
        description: `You average ${data.avgSleepDuration.toFixed(1)} hours of sleep. ${recommendation === 'increase' ? 'Most adults need 7-9 hours.' : 'Consider if you feel rested or could optimize your schedule.'}`,
        impact: data.avgSleepDuration < 6.5 ? 'high' : 'medium',
        confidence: 0.9,
        actionable: true,
      });
    }

    // Optimal timing recommendations
    if (data.optimalAlarmWindow.confidence > 0.8) {
      recommendations.push({
        id: 'timing-optimal',
        type: 'timing',
        title: 'Optimal Alarm Window Detected',
        description: `Based on your patterns, your best wake-up window is ${data.optimalAlarmWindow.start}-${data.optimalAlarmWindow.end}.`,
        impact: 'medium',
        confidence: data.optimalAlarmWindow.confidence,
        actionable: true,
      });
    }

    setRecommendations(recommendations);
  };

  const handleEnableML = async (): Promise<void> => {
    enableCollection();
    setIsLoading(true);
    
    // Simulate loading analytics
    setTimeout(() => {
      const data = getLocalAnalytics();
      setAnalytics(data);
      if (data) {
        generateRecommendations(data);
      }
      setIsLoading(false);
    }, 1000);
  };

  const handleSyncData = async (): Promise<void> => {
    setIsLoading(true);
    const success = await forceSync();
    setIsLoading(false);
    
    if (success) {
      // Show success feedback
      console.log('Data synced successfully');
    }
  };

  const handleExportData = (): void => {
    const data = exportData();
    const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `smart-alarm-data-${new Date().toISOString().split('T')[0]}.json`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  };

  const handleDeleteData = (): void => {
    if (confirm('Are you sure you want to delete all collected data? This action cannot be undone.')) {
      deleteData();
      setAnalytics(null);
      setRecommendations([]);
    }
  };

  if (!isEnabled) {
    return (
      <div className="bg-white rounded-lg shadow-sm p-6 border border-gray-200">
        <div className="flex items-start space-x-4">
          <div className="flex-shrink-0">
            <SparklesIcon className="h-8 w-8 text-blue-500" />
          </div>
          <div className="flex-1">
            <h3 className="text-lg font-semibold text-gray-900 mb-2">
              Smart Sleep Insights
            </h3>
            <p className="text-gray-600 mb-4">
              Enable AI-powered sleep analysis to get personalized recommendations for better sleep and more effective alarms.
            </p>
            <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 mb-4">
              <div className="flex items-start space-x-2">
                <ShieldCheckIcon className="h-5 w-5 text-blue-600 mt-0.5 flex-shrink-0" />
                <div className="text-sm text-blue-800">
                  <p className="font-medium mb-1">Privacy First</p>
                  <ul className="space-y-1 text-blue-700">
                    <li>• Data is processed locally when possible</li>
                    <li>• You control what data is collected</li>
                    <li>• Export or delete your data anytime</li>
                    <li>• No data shared without explicit consent</li>
                  </ul>
                </div>
              </div>
            </div>
            <div className="flex space-x-3">
              <button
                onClick={handleEnableML}
                disabled={isLoading}
                className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 disabled:opacity-50 transition-colors"
              >
                {isLoading ? 'Setting up...' : 'Enable Smart Insights'}
              </button>
              <button
                onClick={() => setShowPrivacySettings(true)}
                className="text-gray-600 px-4 py-2 rounded-lg hover:bg-gray-100 transition-colors"
              >
                Privacy Settings
              </button>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Analytics Summary */}
      {analytics && (
        <div className="bg-white rounded-lg shadow-sm p-6 border border-gray-200">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-lg font-semibold text-gray-900 flex items-center">
              <ChartBarIcon className="h-5 w-5 mr-2 text-blue-500" />
              Sleep Pattern Analysis
            </h3>
            <div className="flex items-center space-x-2">
              {pendingCount > 0 && (
                <button
                  onClick={handleSyncData}
                  disabled={isLoading}
                  className="text-sm text-blue-600 hover:text-blue-700"
                >
                  Sync {pendingCount} data points
                </button>
              )}
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
            <div className="bg-blue-50 p-4 rounded-lg">
              <div className="text-blue-600 text-sm font-medium">Average Bedtime</div>
              <div className="text-2xl font-bold text-blue-900">{analytics.avgBedtime}</div>
            </div>
            <div className="bg-green-50 p-4 rounded-lg">
              <div className="text-green-600 text-sm font-medium">Average Wake Time</div>
              <div className="text-2xl font-bold text-green-900">{analytics.avgWakeupTime}</div>
            </div>
            <div className="bg-purple-50 p-4 rounded-lg">
              <div className="text-purple-600 text-sm font-medium">Sleep Duration</div>
              <div className="text-2xl font-bold text-purple-900">
                {analytics.avgSleepDuration.toFixed(1)}h
              </div>
            </div>
          </div>

          <div className="bg-gray-50 p-4 rounded-lg">
            <div className="flex items-center justify-between">
              <div>
                <div className="text-sm font-medium text-gray-600">Sleep Consistency Score</div>
                <div className="text-lg font-bold text-gray-900">
                  {Math.round(analytics.sleepConsistency * 100)}%
                </div>
              </div>
              <div className="w-24 h-2 bg-gray-200 rounded-full">
                <div 
                  className="h-2 bg-blue-500 rounded-full transition-all duration-500"
                  style={{ width: `${analytics.sleepConsistency * 100}%` }}
                />
              </div>
            </div>
          </div>

          {analytics.optimalAlarmWindow.confidence > 0.7 && (
            <div className="mt-4 bg-yellow-50 border border-yellow-200 rounded-lg p-4">
              <div className="flex items-start space-x-2">
                <ClockIcon className="h-5 w-5 text-yellow-600 mt-0.5" />
                <div>
                  <div className="text-sm font-medium text-yellow-800">
                    Optimal Alarm Window
                  </div>
                  <div className="text-sm text-yellow-700">
                    {analytics.optimalAlarmWindow.start} - {analytics.optimalAlarmWindow.end}
                    <span className="ml-2 text-xs">
                      ({Math.round(analytics.optimalAlarmWindow.confidence * 100)}% confidence)
                    </span>
                  </div>
                </div>
              </div>
            </div>
          )}
        </div>
      )}

      {/* Recommendations */}
      {recommendations.length > 0 && (
        <div className="bg-white rounded-lg shadow-sm p-6 border border-gray-200">
          <h3 className="text-lg font-semibold text-gray-900 mb-4 flex items-center">
            <LightBulbIcon className="h-5 w-5 mr-2 text-yellow-500" />
            Personalized Recommendations
          </h3>
          <div className="space-y-4">
            {recommendations.map((rec) => (
              <div
                key={rec.id}
                className={`border rounded-lg p-4 ${
                  rec.impact === 'high' 
                    ? 'border-red-200 bg-red-50' 
                    : rec.impact === 'medium'
                    ? 'border-yellow-200 bg-yellow-50'
                    : 'border-blue-200 bg-blue-50'
                }`}
              >
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <div className="flex items-center space-x-2 mb-1">
                      <h4 className="font-medium text-gray-900">{rec.title}</h4>
                      <span className={`text-xs px-2 py-1 rounded-full ${
                        rec.impact === 'high'
                          ? 'bg-red-100 text-red-700'
                          : rec.impact === 'medium'
                          ? 'bg-yellow-100 text-yellow-700'
                          : 'bg-blue-100 text-blue-700'
                      }`}>
                        {rec.impact} impact
                      </span>
                    </div>
                    <p className="text-sm text-gray-600">{rec.description}</p>
                    <div className="mt-2 text-xs text-gray-500">
                      Confidence: {Math.round(rec.confidence * 100)}%
                    </div>
                  </div>
                  {rec.actionable && (
                    <button
                      onClick={() => openModal('createAlarm')}
                      className="ml-4 text-sm bg-white border border-gray-300 px-3 py-1 rounded hover:bg-gray-50 transition-colors"
                    >
                      Apply
                    </button>
                  )}
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Smart Alarm Optimization */}
      {analytics && (
        <div className="bg-white rounded-lg shadow-sm p-6 border border-gray-200">
          <h3 className="text-lg font-semibold text-gray-900 mb-4 flex items-center">
            <CogIcon className="h-5 w-5 mr-2 text-purple-500" />
            Intelligent Alarm Optimization
          </h3>
          
          <div className="bg-purple-50 border border-purple-200 rounded-lg p-4 mb-4">
            <div className="flex items-start space-x-3">
              <BellIcon className="h-6 w-6 text-purple-600 mt-0.5 flex-shrink-0" />
              <div className="flex-1">
                <h4 className="font-medium text-purple-900 mb-2">
                  Smart Wake-up Timing
                </h4>
                <p className="text-sm text-purple-800 mb-3">
                  Our AI analyzes your sleep cycles to wake you during light sleep phases for easier, more refreshing mornings.
                </p>
                
                {analytics.optimalAlarmWindow.confidence > 0.6 && (
                  <div className="bg-white rounded-lg p-3 border border-purple-200">
                    <div className="flex items-center justify-between mb-2">
                      <span className="text-sm font-medium text-purple-900">
                        Optimal Wake Window
                      </span>
                      <span className="text-xs bg-purple-100 text-purple-700 px-2 py-1 rounded-full">
                        {Math.round(analytics.optimalAlarmWindow.confidence * 100)}% confidence
                      </span>
                    </div>
                    <div className="text-lg font-bold text-purple-900">
                      {analytics.optimalAlarmWindow.start} - {analytics.optimalAlarmWindow.end}
                    </div>
                    <p className="text-xs text-purple-700 mt-1">
                      Setting alarms within this window increases chances of waking up refreshed
                    </p>
                  </div>
                )}
              </div>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="bg-gray-50 rounded-lg p-4">
              <h5 className="font-medium text-gray-900 mb-2">Sleep Cycle Analysis</h5>
              <div className="space-y-2">
                <div className="flex justify-between text-sm">
                  <span className="text-gray-600">Estimated Cycles/Night:</span>
                  <span className="font-medium">{Math.round(analytics.avgSleepDuration / 1.5)}</span>
                </div>
                <div className="flex justify-between text-sm">
                  <span className="text-gray-600">Cycle Length:</span>
                  <span className="font-medium">~90 minutes</span>
                </div>
                <div className="flex justify-between text-sm">
                  <span className="text-gray-600">Sleep Efficiency:</span>
                  <span className={`font-medium ${analytics.sleepConsistency > 0.7 ? 'text-green-600' : 'text-yellow-600'}`}>
                    {analytics.sleepConsistency > 0.8 ? 'Excellent' : 
                     analytics.sleepConsistency > 0.6 ? 'Good' : 'Fair'}
                  </span>
                </div>
              </div>
            </div>

            <div className="bg-gray-50 rounded-lg p-4">
              <h5 className="font-medium text-gray-900 mb-2">Optimization Features</h5>
              <ul className="space-y-2 text-sm text-gray-600">
                <li className="flex items-center">
                  <div className="w-2 h-2 bg-green-500 rounded-full mr-2"></div>
                  Light sleep phase detection
                </li>
                <li className="flex items-center">
                  <div className="w-2 h-2 bg-blue-500 rounded-full mr-2"></div>
                  Personal sleep cycle mapping
                </li>
                <li className="flex items-center">
                  <div className="w-2 h-2 bg-purple-500 rounded-full mr-2"></div>
                  Chronotype adaptation
                </li>
                <li className="flex items-center">
                  <div className="w-2 h-2 bg-orange-500 rounded-full mr-2"></div>
                  Gradual optimization learning
                </li>
              </ul>
            </div>
          </div>

          <div className="mt-4 text-xs text-gray-500 bg-gray-50 rounded-lg p-3">
            <p className="mb-1">
              <strong>How it works:</strong> The system tracks your natural wake-up patterns and identifies when you naturally feel most alert.
            </p>
            <p>
              New alarms will automatically suggest optimal times within ±15 minutes of your desired time for the best waking experience.
            </p>
          </div>
        </div>
      )}

      {/* Privacy Controls */}
      <div className="bg-white rounded-lg shadow-sm p-6 border border-gray-200">
        <h3 className="text-lg font-semibold text-gray-900 mb-4 flex items-center">
          <ShieldCheckIcon className="h-5 w-5 mr-2 text-green-500" />
          Privacy & Data Controls
        </h3>
        <div className="flex flex-wrap gap-3">
          <button
            onClick={() => setShowPrivacySettings(true)}
            className="text-sm border border-gray-300 px-3 py-2 rounded hover:bg-gray-50 transition-colors"
          >
            Privacy Settings
          </button>
          <button
            onClick={handleExportData}
            className="text-sm border border-gray-300 px-3 py-2 rounded hover:bg-gray-50 transition-colors"
          >
            Export My Data
          </button>
          <button
            onClick={handleDeleteData}
            className="text-sm border border-red-300 text-red-700 px-3 py-2 rounded hover:bg-red-50 transition-colors"
          >
            <TrashIcon className="h-4 w-4 inline mr-1" />
            Delete All Data
          </button>
          <button
            onClick={disableCollection}
            className="text-sm text-gray-600 px-3 py-2 rounded hover:bg-gray-100 transition-colors"
          >
            Disable Insights
          </button>
        </div>
        
        {pendingCount > 0 && (
          <div className="mt-4 text-sm text-gray-600">
            {pendingCount} data points pending sync
          </div>
        )}
      </div>

      {/* Privacy Settings Modal would go here */}
    </div>
  );
};