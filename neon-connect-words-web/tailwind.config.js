export default {
  content: ['./index.html', './src/**/*.{js,jsx}'],
  theme: {
    extend: {
      fontFamily: {
        display: ['Orbitron', 'sans-serif'],
        ui: ['Rajdhani', 'sans-serif'],
        mono: ['Share Tech Mono', 'monospace'],
      },
      animation: {
        'grid-pan': 'gridPan 12s linear infinite',
        'tile-drop': 'tileDrop 520ms cubic-bezier(.18,.76,.24,1)',
        'tile-clear': 'tileClear 580ms ease-in forwards',
        'gold-pulse': 'goldPulse 580ms ease-out',
        'logo-flicker': 'logoFlicker 3s ease-in-out infinite',
      },
      keyframes: {
        gridPan: {
          '0%': { backgroundPosition: '0 0' },
          '100%': { backgroundPosition: '56px 56px' },
        },
        tileDrop: {
          '0%': { transform: 'translateY(-140%) scale(.96)', opacity: '.55' },
          '58%': { transform: 'translateY(-18%) scale(.99)', opacity: '.92' },
          '86%': { transform: 'translateY(5%) scale(1.015)', opacity: '1' },
          '100%': { transform: 'translateY(0) scale(1)', opacity: '1' },
        },
        tileClear: {
          '0%': { transform: 'scale(1)', opacity: '1' },
          '35%': { transform: 'scale(1.12)', opacity: '1' },
          '100%': { transform: 'scale(0)', opacity: '0' },
        },
        goldPulse: {
          '0%, 100%': { filter: 'brightness(1)' },
          '50%': { filter: 'brightness(1.8)' },
        },
        logoFlicker: {
          '0%, 100%': { opacity: '1' },
          '48%': { opacity: '.9' },
          '50%': { opacity: '.55' },
          '52%': { opacity: '1' },
        },
      },
    },
  },
  plugins: [],
};
