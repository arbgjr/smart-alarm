import React, { useState, useEffect } from 'react';
import { User, Mail, Phone, MapPin, Shield, Eye, EyeOff } from 'lucide-react';

interface UserProfileSettingsProps {
  user: any;
  onSettingsChange: () => void;
}

interface ProfileData {
  name: string;
  email: string;
  phone: string;
  timezone: string;
  dateFormat: string;
  timeFormat: '12h' | '24h';
  language: string;
  avatar?: string;
}

interface SecuritySettings {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
  twoFactorEnabled: boolean;
}

export const UserProfileSettings: React.FC<UserProfileSettingsProps> = ({
  user,
  onSettingsChange
}) => {
  const [profileData, setProfileData] = useState<ProfileData>({
    name: user?.name || '',
    email: user?.email || '',
    phone: user?.phone || '',
    timezone: Intl.DateTimeFormat().resolvedOptions().timeZone,
    dateFormat: 'MM/DD/YYYY',
    timeFormat: '12h',
    language: 'en',
    avatar: user?.avatar
  });

  const [securitySettings, setSecuritySettings] = useState<SecuritySettings>({
    currentPassword: '',
    newPassword: '',
    confirmPassword: '',
    twoFactorEnabled: false
  });

  const [showPasswords, setShowPasswords] = useState({
    current: false,
    new: false,
    confirm: false
  });

  const [activeSection, setActiveSection] = useState<'profile' | 'security'>('profile');

  useEffect(() => {
    if (user) {
      setProfileData(prev => ({
        ...prev,
        name: user.name || '',
        email: user.email || '',
        phone: user.phone || '',
        avatar: user.avatar
      }));
    }
  }, [user]);

  const handleProfileChange = (field: keyof ProfileData, value: string) => {
    setProfileData(prev => ({ ...prev, [field]: value }));
    onSettingsChange();
  };

  const handleSecurityChange = (field: keyof SecuritySettings, value: string | boolean) => {
    setSecuritySettings(prev => ({ ...prev, [field]: value }));
    onSettingsChange();
  };

  const handleAvatarUpload = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      const reader = new FileReader();
      reader.onload = (e) => {
        const result = e.target?.result as string;
        setProfileData(prev => ({ ...prev, avatar: result }));
        onSettingsChange();
      };
      reader.readAsDataURL(file);
    }
  };

  const timezones = [
    'America/New_York',
    'America/Chicago',
    'America/Denver',
    'America/Los_Angeles',
    'Europe/London',
    'Europe/Paris',
    'Europe/Berlin',
    'Asia/Tokyo',
    'Asia/Shanghai',
    'Australia/Sydney'
  ];

  return (
    <div className="space-y-6">
      {/* Section Tabs */}
      <div className="border-b border-gray-200">
        <nav className="-mb-px flex space-x-8">
          <button
            onClick={() => setActiveSection('profile')}
            className={`py-2 px-1 border-b-2 font-medium text-sm transition-colors ${
              activeSection === 'profile'
                ? 'border-blue-500 text-blue-600'
                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
            }`}
          >
            Profile Information
          </button>
          <button
            onClick={() => setActiveSection('security')}
            className={`py-2 px-1 border-b-2 font-medium text-sm transition-colors ${
              activeSection === 'security'
                ? 'border-blue-500 text-blue-600'
                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
            }`}
          >
            Security & Privacy
          </button>
        </nav>
      </div>

      {activeSection === 'profile' && (
        <div className="bg-white shadow-sm rounded-lg border border-gray-200">
          <div className="px-6 py-4 border-b border-gray-200">
            <h3 className="text-lg font-medium text-gray-900">Profile Information</h3>
            <p className="text-sm text-gray-600 mt-1">
              Update your personal information and preferences
            </p>
          </div>

          <div className="p-6 space-y-6">
            {/* Avatar Upload */}
            <div className="flex items-center space-x-6">
              <div className="flex-shrink-0">
                {profileData.avatar ? (
                  <img
                    src={profileData.avatar}
                    alt="Profile"
                    className="w-20 h-20 rounded-full object-cover"
                  />
                ) : (
                  <div className="w-20 h-20 bg-gray-300 rounded-full flex items-center justify-center">
                    <User className="w-8 h-8 text-gray-600" />
                  </div>
                )}
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Profile Picture
                </label>
                <input
                  type="file"
                  accept="image/*"
                  onChange={handleAvatarUpload}
                  className="block w-full text-sm text-gray-500 file:mr-4 file:py-2 file:px-4 file:rounded-md file:border-0 file:text-sm file:font-medium file:bg-blue-50 file:text-blue-700 hover:file:bg-blue-100"
                />
              </div>
            </div>

            {/* Basic Information */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  <User className="w-4 h-4 inline mr-2" />
                  Full Name
                </label>
                <input
                  type="text"
                  value={profileData.name}
                  onChange={(e) => handleProfileChange('name', e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                  placeholder="Enter your full name"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  <Mail className="w-4 h-4 inline mr-2" />
                  Email Address
                </label>
                <input
                  type="email"
                  value={profileData.email}
                  onChange={(e) => handleProfileChange('email', e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                  placeholder="Enter your email"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  <Phone className="w-4 h-4 inline mr-2" />
                  Phone Number
                </label>
                <input
                  type="tel"
                  value={profileData.phone}
                  onChange={(e) => handleProfileChange('phone', e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                  placeholder="Enter your phone number"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  <MapPin className="w-4 h-4 inline mr-2" />
                  Timezone
                </label>
                <select
                  value={profileData.timezone}
                  onChange={(e) => handleProfileChange('timezone', e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                >
                  {timezones.map(tz => (
                    <option key={tz} value={tz}>{tz}</option>
                  ))}
                </select>
              </div>
            </div>

            {/* Preferences */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Date Format
                </label>
                <select
                  value={profileData.dateFormat}
                  onChange={(e) => handleProfileChange('dateFormat', e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                >
                  <option value="MM/DD/YYYY">MM/DD/YYYY</option>
                  <option value="DD/MM/YYYY">DD/MM/YYYY</option>
                  <option value="YYYY-MM-DD">YYYY-MM-DD</option>
                </select>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Time Format
                </label>
                <select
                  value={profileData.timeFormat}
                  onChange={(e) => handleProfileChange('timeFormat', e.target.value as '12h' | '24h')}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                >
                  <option value="12h">12 Hour (AM/PM)</option>
                  <option value="24h">24 Hour</option>
                </select>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Language
                </label>
                <select
                  value={profileData.language}
                  onChange={(e) => handleProfileChange('language', e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                >
                  <option value="en">English</option>
                  <option value="es">Español</option>
                  <option value="fr">Français</option>
                  <option value="de">Deutsch</option>
                  <option value="pt">Português</option>
                </select>
              </div>
            </div>
          </div>
        </div>
      )}

      {activeSection === 'security' && (
        <div className="bg-white shadow-sm rounded-lg border border-gray-200">
          <div className="px-6 py-4 border-b border-gray-200">
            <h3 className="text-lg font-medium text-gray-900">Security & Privacy</h3>
            <p className="text-sm text-gray-600 mt-1">
              Manage your password and security settings
            </p>
          </div>

          <div className="p-6 space-y-6">
            {/* Change Password */}
            <div>
              <h4 className="text-md font-medium text-gray-900 mb-4">Change Password</h4>
              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Current Password
                  </label>
                  <div className="relative">
                    <input
                      type={showPasswords.current ? 'text' : 'password'}
                      value={securitySettings.currentPassword}
                      onChange={(e) => handleSecurityChange('currentPassword', e.target.value)}
                      className="w-full px-3 py-2 pr-10 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                      placeholder="Enter current password"
                    />
                    <button
                      type="button"
                      onClick={() => setShowPasswords(prev => ({ ...prev, current: !prev.current }))}
                      className="absolute inset-y-0 right-0 pr-3 flex items-center"
                    >
                      {showPasswords.current ? (
                        <EyeOff className="w-4 h-4 text-gray-400" />
                      ) : (
                        <Eye className="w-4 h-4 text-gray-400" />
                      )}
                    </button>
                  </div>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      New Password
                    </label>
                    <div className="relative">
                      <input
                        type={showPasswords.new ? 'text' : 'password'}
                        value={securitySettings.newPassword}
                        onChange={(e) => handleSecurityChange('newPassword', e.target.value)}
                        className="w-full px-3 py-2 pr-10 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                        placeholder="Enter new password"
                      />
                      <button
                        type="button"
                        onClick={() => setShowPasswords(prev => ({ ...prev, new: !prev.new }))}
                        className="absolute inset-y-0 right-0 pr-3 flex items-center"
                      >
                        {showPasswords.new ? (
                          <EyeOff className="w-4 h-4 text-gray-400" />
                        ) : (
                          <Eye className="w-4 h-4 text-gray-400" />
                        )}
                      </button>
                    </div>
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      Confirm New Password
                    </label>
                    <div className="relative">
                      <input
                        type={showPasswords.confirm ? 'text' : 'password'}
                        value={securitySettings.confirmPassword}
                        onChange={(e) => handleSecurityChange('confirmPassword', e.target.value)}
                        className="w-full px-3 py-2 pr-10 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                        placeholder="Confirm new password"
                      />
                      <button
                        type="button"
                        onClick={() => setShowPasswords(prev => ({ ...prev, confirm: !prev.confirm }))}
                        className="absolute inset-y-0 right-0 pr-3 flex items-center"
                      >
                        {showPasswords.confirm ? (
                          <EyeOff className="w-4 h-4 text-gray-400" />
                        ) : (
                          <Eye className="w-4 h-4 text-gray-400" />
                        )}
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            {/* Two-Factor Authentication */}
            <div className="pt-6 border-t border-gray-200">
              <div className="flex items-center justify-between">
                <div>
                  <h4 className="text-md font-medium text-gray-900">Two-Factor Authentication</h4>
                  <p className="text-sm text-gray-600 mt-1">
                    Add an extra layer of security to your account
                  </p>
                </div>
                <div className="flex items-center">
                  <input
                    type="checkbox"
                    id="twoFactor"
                    checked={securitySettings.twoFactorEnabled}
                    onChange={(e) => handleSecurityChange('twoFactorEnabled', e.target.checked)}
                    className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                  />
                  <label htmlFor="twoFactor" className="ml-2 block text-sm text-gray-900">
                    Enable 2FA
                  </label>
                </div>
              </div>
            </div>

            {/* Account Actions */}
            <div className="pt-6 border-t border-gray-200">
              <h4 className="text-md font-medium text-gray-900 mb-4">Account Actions</h4>
              <div className="space-y-3">
                <button className="w-full text-left px-4 py-3 border border-gray-300 rounded-md hover:bg-gray-50 transition-colors">
                  <div className="flex items-center justify-between">
                    <div>
                      <p className="text-sm font-medium text-gray-900">Download Account Data</p>
                      <p className="text-xs text-gray-600">Export all your data in JSON format</p>
                    </div>
                    <Shield className="w-5 h-5 text-gray-400" />
                  </div>
                </button>

                <button className="w-full text-left px-4 py-3 border border-red-300 rounded-md hover:bg-red-50 transition-colors">
                  <div className="flex items-center justify-between">
                    <div>
                      <p className="text-sm font-medium text-red-900">Delete Account</p>
                      <p className="text-xs text-red-600">Permanently delete your account and all data</p>
                    </div>
                    <Shield className="w-5 h-5 text-red-400" />
                  </div>
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
