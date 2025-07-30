import React, { useState } from 'react';
import {
  Button,
  Input,
  Card,
  CardHeader,
  CardTitle,
  CardContent,
  LoadingSpinner
} from '../atoms';

export const ComponentShowcase: React.FC = () => {
  const [loading, setLoading] = useState(false);
  const [inputValue, setInputValue] = useState('');
  const [inputError, setInputError] = useState('');

  const handleButtonClick = () => {
    setLoading(true);
    setTimeout(() => setLoading(false), 2000);
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value;
    setInputValue(value);

    // Example validation
    if (value.length > 0 && value.length < 3) {
      setInputError('Must be at least 3 characters');
    } else {
      setInputError('');
    }
  };

  const PlusIcon = () => (
    <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
    </svg>
  );

  const UserIcon = () => (
    <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
    </svg>
  );

  return (
    <div className="min-h-screen bg-neutral-50 p-8">
      <div className="mx-auto max-w-4xl space-y-8">
        {/* Header */}
        <div className="text-center">
          <h1 className="text-3xl font-bold text-neutral-900 mb-2">
            Smart Alarm - Component Showcase
          </h1>
          <p className="text-neutral-600">
            Testing our atomic design components with accessibility features
          </p>
        </div>

        {/* Buttons Section */}
        <Card>
          <CardHeader>
            <CardTitle>Button Components</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              <div className="space-y-2">
                <h4 className="font-medium text-sm text-neutral-700">Variants</h4>
                <div className="space-y-2">
                  <Button onClick={handleButtonClick} loading={loading}>
                    Primary Button
                  </Button>
                  <Button variant="secondary">Secondary</Button>
                  <Button variant="outline">Outline</Button>
                  <Button variant="ghost">Ghost</Button>
                  <Button variant="destructive">Delete</Button>
                </div>
              </div>

              <div className="space-y-2">
                <h4 className="font-medium text-sm text-neutral-700">Sizes</h4>
                <div className="space-y-2">
                  <Button size="sm">Small</Button>
                  <Button size="default">Default</Button>
                  <Button size="lg">Large</Button>
                </div>
              </div>

              <div className="space-y-2">
                <h4 className="font-medium text-sm text-neutral-700">With Icons</h4>
                <div className="space-y-2">
                  <Button leftIcon={<PlusIcon />}>Add Alarm</Button>
                  <Button variant="outline" rightIcon={<UserIcon />}>Profile</Button>
                  <Button size="icon" aria-label="Add new item">
                    <PlusIcon />
                  </Button>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Input Section */}
        <Card>
          <CardHeader>
            <CardTitle>Input Components</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div className="space-y-4">
                <Input
                  label="Alarm Name"
                  placeholder="Enter alarm name"
                  value={inputValue}
                  onChange={handleInputChange}
                  errorMessage={inputError}
                  required
                />

                <Input
                  label="Email Address"
                  type="email"
                  placeholder="user@example.com"
                  leftIcon={<UserIcon />}
                  helperText="We'll use this for notifications"
                />

                <Input
                  label="Time"
                  type="time"
                  defaultValue="07:00"
                />
              </div>

              <div className="space-y-4">
                <Input
                  label="Success State"
                  variant="success"
                  defaultValue="Successfully saved!"
                  readOnly
                />

                <Input
                  label="Error State"
                  variant="error"
                  defaultValue="Invalid input"
                  errorMessage="This field contains an error"
                />

                <Input
                  label="Disabled"
                  placeholder="Disabled input"
                  disabled
                />
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Cards Section */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          <Card variant="elevated">
            <CardHeader>
              <CardTitle>Morning Routine</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-sm text-neutral-600">
                Your daily morning alarm with personalized settings.
              </p>
            </CardContent>
          </Card>

          <Card variant="success">
            <CardHeader>
              <CardTitle>System Status</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-sm text-green-700">
                All systems operational and ready.
              </p>
            </CardContent>
          </Card>

          <Card variant="warning">
            <CardHeader>
              <CardTitle>Attention Required</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-sm text-yellow-700">
                Please review your notification settings.
              </p>
            </CardContent>
          </Card>
        </div>

        {/* Loading States */}
        <Card>
          <CardHeader>
            <CardTitle>Loading States</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-1 md:grid-cols-4 gap-6 items-center">
              <div className="text-center">
                <LoadingSpinner size="sm" />
                <p className="text-xs text-neutral-500 mt-2">Small</p>
              </div>

              <div className="text-center">
                <LoadingSpinner size="default" />
                <p className="text-xs text-neutral-500 mt-2">Default</p>
              </div>

              <div className="text-center">
                <LoadingSpinner size="lg" />
                <p className="text-xs text-neutral-500 mt-2">Large</p>
              </div>

              <div className="text-center">
                <LoadingSpinner size="xl" showText label="Processing..." />
                <p className="text-xs text-neutral-500 mt-2">With Text</p>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Accessibility Information */}
        <Card variant="outlined">
          <CardHeader>
            <CardTitle>♿ Accessibility Features</CardTitle>
          </CardHeader>
          <CardContent>
            <ul className="space-y-2 text-sm text-neutral-600">
              <li>✅ Keyboard navigation support</li>
              <li>✅ Screen reader compatibility</li>
              <li>✅ Focus management and visual indicators</li>
              <li>✅ ARIA labels and live regions</li>
              <li>✅ Color contrast compliance (WCAG 2.1 AAA)</li>
              <li>✅ Semantic HTML structure</li>
            </ul>
          </CardContent>
        </Card>
      </div>
    </div>
  );
};
