import React, { useState } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { Button } from '../../atoms/Button/Button';
import { Input } from '../../atoms/Input/Input';
import { Card, CardHeader, CardTitle, CardContent, CardFooter } from '../../atoms/Card/Card';
import { useAuth } from '../../../hooks/useAuth';
import { LoginRequest } from '../../../types/auth';

interface LocationState {
  from?: { pathname: string };
}

export const LoginForm: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const state = location.state as LocationState;
  const from = state?.from?.pathname || '/dashboard';

  const {
    login,
    authenticateWithFIDO2,
    isLoggingIn,
    isAuthenticatingFIDO2,
    loginError,
    fido2Error,
    loginSuccess
  } = useAuth();

  const [formData, setFormData] = useState<LoginRequest>({
    email: '',
    password: '',
  });

  const [errors, setErrors] = useState<Record<string, string>>({});
  const [showPassword, setShowPassword] = useState(false);
  const [rememberMe, setRememberMe] = useState(false);

  // Validate form
  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!formData.email) {
      newErrors.email = 'Email é obrigatório';
    } else if (!/\S+@\S+\.\S+/.test(formData.email)) {
      newErrors.email = 'Email inválido';
    }

    if (!formData.password) {
      newErrors.password = 'Senha é obrigatória';
    } else if (formData.password.length < 6) {
      newErrors.password = 'Senha deve ter pelo menos 6 caracteres';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  // Handle form submission
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) return;

    login(formData, {
      onSuccess: () => {
        navigate(from, { replace: true });
      },
    });
  };

  // Handle FIDO2 authentication
  const handleFIDO2Login = async () => {
    if (!formData.email) {
      setErrors({ email: 'Email é obrigatório para autenticação FIDO2' });
      return;
    }

    authenticateWithFIDO2(undefined, {
      onSuccess: () => {
        navigate(from, { replace: true });
      },
    });
  };

  // Update form data
  const updateFormData = (field: keyof LoginRequest) => (
    e: React.ChangeEvent<HTMLInputElement>
  ) => {
    const value = e.target.type === 'checkbox' ? e.target.checked : e.target.value;
    setFormData(prev => ({ ...prev, [field]: value }));

    // Clear field error when user starts typing
    if (errors[field]) {
      setErrors(prev => ({ ...prev, [field]: '' }));
    }
  };

  // Icons
  const EyeIcon = () => (
    <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
        d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
        d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
    </svg>
  );

  const EyeSlashIcon = () => (
    <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
        d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.878 9.878L3 3m6.878 6.878L21 21" />
    </svg>
  );

  const EmailIcon = () => (
    <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
        d="M16 12a4 4 0 10-8 0 4 4 0 008 0zm0 0v1.5a2.5 2.5 0 005 0V12a9 9 0 10-9 9m4.5-1.206a8.959 8.959 0 01-4.5 1.207" />
    </svg>
  );

  const KeyIcon = () => (
    <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
        d="M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z" />
    </svg>
  );

  React.useEffect(() => {
    if (loginSuccess) {
      navigate(from, { replace: true });
    }
  }, [loginSuccess, navigate, from]);

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-primary-50 via-white to-secondary-50 px-4">
      <Card className="w-full max-w-md">
        <CardHeader className="text-center">
          <CardTitle className="text-2xl font-bold text-neutral-900">
            🚨 Smart Alarm
          </CardTitle>
          <p className="text-sm text-neutral-600 mt-2">
            Entre na sua conta para gerenciar seus alarmes
          </p>
        </CardHeader>

        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            {/* Email Input */}
            <Input
              label="Email"
              type="email"
              placeholder="seu@email.com"
              value={formData.email}
              onChange={updateFormData('email')}
              errorMessage={errors.email}
              leftIcon={<EmailIcon />}
              required
              autoComplete="email"
            />

            {/* Password Input */}
            <Input
              label="Senha"
              type={showPassword ? 'text' : 'password'}
              placeholder="••••••••"
              value={formData.password}
              onChange={updateFormData('password')}
              errorMessage={errors.password}
              leftIcon={<KeyIcon />}
              rightIcon={
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="hover:text-neutral-600 transition-colors"
                  aria-label={showPassword ? 'Ocultar senha' : 'Mostrar senha'}
                >
                  {showPassword ? <EyeSlashIcon /> : <EyeIcon />}
                </button>
              }
              required
              autoComplete="current-password"
            />

            {/* Remember Me */}
                        {/* Remember Me Checkbox */}
            <div className="flex items-center space-x-2">
              <input
                type="checkbox"
                id="rememberMe"
                checked={rememberMe}
                onChange={(e) => setRememberMe(e.target.checked)}
                className="w-4 h-4 text-primary-600 bg-neutral-100 border-neutral-300 rounded focus:ring-primary-500 focus:ring-2"
              />
              <label htmlFor="rememberMe" className="text-sm text-neutral-700">
                Lembrar de mim
              </label>
            </div>

            {/* Login Error */}
            {loginError && (
              <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-md text-sm">
                {loginError.message || 'Erro ao fazer login. Verifique suas credenciais.'}
              </div>
            )}

            {/* FIDO2 Error */}
            {fido2Error && (
              <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-md text-sm">
                Erro na autenticação biométrica. Tente novamente.
              </div>
            )}

            {/* Login Button */}
            <Button
              type="submit"
              variant="primary"
              size="lg"
              className="w-full"
              loading={isLoggingIn}
              disabled={isLoggingIn || isAuthenticatingFIDO2}
            >
              {isLoggingIn ? 'Entrando...' : 'Entrar'}
            </Button>

            {/* FIDO2 Button */}
            <Button
              type="button"
              variant="outline"
              size="lg"
              className="w-full"
              onClick={handleFIDO2Login}
              loading={isAuthenticatingFIDO2}
              disabled={isLoggingIn || isAuthenticatingFIDO2 || !formData.email}
            >
              {isAuthenticatingFIDO2 ? 'Autenticando...' : '🔐 Login Biométrico (FIDO2)'}
            </Button>
          </form>
        </CardContent>

        <CardFooter className="flex flex-col space-y-2 text-center">
          <Link
            to="/forgot-password"
            className="text-sm text-primary-600 hover:text-primary-700 underline"
          >
            Esqueceu sua senha?
          </Link>

          <p className="text-sm text-neutral-600">
            Não tem uma conta?{' '}
            <Link
              to="/register"
              className="text-primary-600 hover:text-primary-700 font-medium underline"
            >
              Cadastre-se
            </Link>
          </p>
        </CardFooter>
      </Card>
    </div>
  );
};
