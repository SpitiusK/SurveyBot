import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { RouterProvider } from 'react-router-dom';
import { router } from './routes';
import { ThemeProvider } from './theme/ThemeProvider';
import { GlobalStyles } from './theme/globalStyles.tsx';
import { AuthProvider } from './context/AuthContext';
import './index.css';
import 'leaflet/dist/leaflet.css';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ThemeProvider>
      <GlobalStyles />
      <AuthProvider>
        <RouterProvider router={router} />
      </AuthProvider>
    </ThemeProvider>
  </StrictMode>
);
