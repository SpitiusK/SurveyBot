import React, { useState, useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useForm, Controller } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import {
  Box,
  Container,
  Paper,
  TextField,
  Button,
  Typography,
  Alert,
  InputAdornment,
  Link,
  Divider,
  Chip,
} from '@mui/material';
import {
  Telegram as TelegramIcon,
  Login as LoginIcon,
} from '@mui/icons-material';
import { useAuth } from '@/hooks/useAuth';
import { loginSchema, type LoginFormData } from '@/schemas/authSchemas';

const Login: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const { login, isAuthenticated } = useAuth();

  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const {
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormData>({
    resolver: yupResolver(loginSchema) as any,
    defaultValues: {
      telegramId: 0,
      username: undefined,
      firstName: undefined,
      lastName: undefined,
    },
  });

  // Debug: Log API Base URL
  console.log('API Base URL:', import.meta.env.VITE_API_BASE_URL);

  // Redirect if already authenticated
  useEffect(() => {
    if (isAuthenticated) {
      const from = (location.state as any)?.from?.pathname || '/dashboard';
      navigate(from, { replace: true });
    }
  }, [isAuthenticated, navigate, location]);

  const onSubmit = async (data: LoginFormData) => {
    setIsLoading(true);
    setError(null);

    try {
      await login(data);
      // Navigation will happen via useEffect above
    } catch (err: any) {
      console.error('Login error:', err);
      setError(
        err.response?.data?.message ||
          err.message ||
          'Failed to login. Please check your credentials.'
      );
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
        py: 4,
      }}
    >
      <Container maxWidth="sm">
        <Paper
          elevation={24}
          sx={{
            p: { xs: 3, sm: 4, md: 5 },
            borderRadius: 3,
          }}
        >
          <Box
            sx={{
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              mb: 3,
            }}
          >
            <Box
              sx={{
                width: 64,
                height: 64,
                borderRadius: '50%',
                backgroundColor: 'primary.main',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                mb: 2,
              }}
            >
              <LoginIcon sx={{ fontSize: 32, color: 'white' }} />
            </Box>
            <Typography component="h1" variant="h4" fontWeight="bold" gutterBottom>
              Welcome Back
            </Typography>
            <Typography variant="body2" color="text.secondary" align="center">
              Login to your SurveyBot admin panel
            </Typography>
          </Box>

          <Divider sx={{ mb: 3 }}>
            <Chip
              icon={<TelegramIcon />}
              label="Telegram Authentication"
              size="small"
              color="primary"
              variant="outlined"
            />
          </Divider>

          {error && (
            <Alert severity="error" sx={{ mb: 3 }} onClose={() => setError(null)}>
              {error}
            </Alert>
          )}

          <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
            <Controller
              name="telegramId"
              control={control}
              render={({ field }) => (
                <TextField
                  {...field}
                  label="Telegram ID"
                  type="number"
                  fullWidth
                  margin="normal"
                  required
                  autoFocus
                  error={!!errors.telegramId}
                  helperText={errors.telegramId?.message}
                  disabled={isLoading}
                  InputProps={{
                    startAdornment: (
                      <InputAdornment position="start">
                        <TelegramIcon color="action" />
                      </InputAdornment>
                    ),
                  }}
                />
              )}
            />

            <Controller
              name="username"
              control={control}
              render={({ field }) => (
                <TextField
                  {...field}
                  label="Username (Optional)"
                  fullWidth
                  margin="normal"
                  error={!!errors.username}
                  helperText={errors.username?.message || 'Your Telegram username'}
                  disabled={isLoading}
                />
              )}
            />

            <Controller
              name="firstName"
              control={control}
              render={({ field }) => (
                <TextField
                  {...field}
                  label="First Name (Optional)"
                  fullWidth
                  margin="normal"
                  error={!!errors.firstName}
                  helperText={errors.firstName?.message}
                  disabled={isLoading}
                />
              )}
            />

            <Controller
              name="lastName"
              control={control}
              render={({ field }) => (
                <TextField
                  {...field}
                  label="Last Name (Optional)"
                  fullWidth
                  margin="normal"
                  error={!!errors.lastName}
                  helperText={errors.lastName?.message}
                  disabled={isLoading}
                />
              )}
            />

            <Button
              type="submit"
              fullWidth
              variant="contained"
              size="large"
              disabled={isLoading}
              sx={{ mt: 3, mb: 2, py: 1.5 }}
            >
              {isLoading ? 'Logging in...' : 'Login'}
            </Button>

            <Box sx={{ mt: 2, textAlign: 'center' }}>
              <Typography variant="body2" color="text.secondary">
                Note: This is a Telegram-based authentication system. Use your Telegram ID to
                login.
              </Typography>
            </Box>
          </Box>
        </Paper>

        <Box sx={{ mt: 3, textAlign: 'center' }}>
          <Typography variant="body2" sx={{ color: 'white' }}>
            Don't know your Telegram ID?{' '}
            <Link
              href="https://t.me/userinfobot"
              target="_blank"
              rel="noopener noreferrer"
              sx={{ color: 'white', fontWeight: 'bold', textDecoration: 'underline' }}
            >
              Find it here
            </Link>
          </Typography>
        </Box>
      </Container>
    </Box>
  );
};

export default Login;
