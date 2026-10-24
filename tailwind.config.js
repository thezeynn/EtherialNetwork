/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        cyber: {
          bg: "#08090d",
          card: "#0f1118",
          cardHover: "#161924",
          border: "#1f2438",
          primary: "#00f0ff",
          primaryGlow: "rgba(0, 240, 255, 0.4)",
          accent: "#7928ca",
          accentGlow: "rgba(121, 40, 202, 0.4)",
          success: "#00ff9d",
          warning: "#ffbe0b",
          danger: "#ff0055",
          text: "#f0f4fc",
          muted: "#8e9bb0",
        }
      },
      fontFamily: {
        sans: ['"Inter"', 'system-ui', 'sans-serif'],
        display: ['"Rajdhani"', 'system-ui', 'sans-serif'],
      },
      boxShadow: {
        'neon-cyan': '0 0 25px rgba(0, 240, 255, 0.45)',
        'neon-purple': '0 0 25px rgba(121, 40, 202, 0.45)',
      }
    },
  },
  plugins: [],
}
