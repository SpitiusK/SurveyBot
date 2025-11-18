# SurveyBot Admin Panel - Frontend

React + TypeScript + Vite frontend application for managing SurveyBot surveys.

**For comprehensive documentation, see [CLAUDE.md](./CLAUDE.md)**

## Quick Start

### Prerequisites
- Node.js 18+ and npm
- Backend API running at http://localhost:5000

### Installation & Development

```bash
npm install
npm run dev
```

The application will open at http://localhost:3000

### Available npm Scripts

```bash
npm run dev       # Start development server (port 3000)
npm run build     # Build for production
npm run preview   # Preview production build locally
npm run lint      # Run ESLint
```

### Configuration

The frontend uses a centralized ngrok configuration system:

**Primary Configuration**: `src/config/ngrok.config.ts`
- Contains all ngrok URLs used in the application
- Update this file when your ngrok session expires
- All references automatically use these URLs

**Environment Files** (`.env.*`):
- `.env.development` - Development settings (gitignored)
- `.env.production` - Production settings (gitignored)

**Learn More**: See [NGROK_SETUP.md](./docs/NGROK_SETUP.md) for detailed ngrok configuration guide

### Production Build

```bash
npm run build
```

Output: `frontend/dist/` (ready for deployment)

## Project Structure

```
frontend/
├── src/
│   ├── components/    # Reusable UI components
│   ├── pages/         # Page components
│   ├── services/      # API integration (Axios)
│   ├── types/         # TypeScript definitions
│   ├── App.tsx        # Root component
│   └── main.tsx       # Entry point
├── CLAUDE.md          # Comprehensive documentation
├── .env.example       # Configuration template
├── vite.config.ts     # Vite configuration
└── package.json
```

## Documentation

- **[CLAUDE.md](./CLAUDE.md)** - Complete frontend documentation
  - Component architecture
  - API integration patterns
  - Authentication & state management
  - Form handling & validation
  - Development workflow
  - Troubleshooting guide

- **[AUTHENTICATION.md](./AUTHENTICATION.md)** - Authentication flow details
- **[NAVIGATION.md](./NAVIGATION.md)** - Routes and navigation structure
- **[UI_COMPONENTS_GUIDE.md](./UI_COMPONENTS_GUIDE.md)** - Component library guide

## Technology Stack

- React 19 | TypeScript | Vite
- React Router 7 | Axios | Material-UI
- ESLint | Vite env support
