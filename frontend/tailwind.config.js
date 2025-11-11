/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        primary: {
          main: '#1976d2',
          light: '#42a5f5',
          dark: '#1565c0',
        },
        secondary: {
          main: '#dc004e',
          light: '#f33371',
          dark: '#9a0036',
        },
      },
    },
  },
  plugins: [],
  corePlugins: {
    // Disable Tailwind's preflight to avoid conflicts with MUI
    preflight: false,
  },
}
