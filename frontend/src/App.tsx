import { Routes, Route, Navigate } from 'react-router-dom';

function App() {
  return (
    <div className="app min-h-screen bg-gray-50">
      <Routes>
        {/* Temporary placeholder routes */}
        <Route path="/" element={
          <div className="flex items-center justify-center min-h-screen">
            <div className="text-center">
              <h1 className="text-4xl font-bold text-gray-900 mb-4">
                Smart Alarm
              </h1>
              <p className="text-lg text-gray-600 mb-8">
                Accessibility-first alarm management system
              </p>
              <div className="space-y-4">
                <div className="bg-green-100 border border-green-400 text-green-700 px-4 py-3 rounded">
                  ✅ Frontend environment setup complete
                </div>
                <div className="bg-blue-100 border border-blue-400 text-blue-700 px-4 py-3 rounded">
                  🔧 React + TypeScript + Vite + Tailwind CSS configured
                </div>
                <div className="bg-purple-100 border border-purple-400 text-purple-700 px-4 py-3 rounded">
                  ♿ WCAG 2.1 AAA accessibility features ready
                </div>
              </div>
            </div>
          </div>
        } />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </div>
  );
}

export default App;
