/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      colors: {
        page: 'var(--color-page)',
        surface: 'var(--color-surface)',
        'surface-secondary': 'var(--color-surface-secondary)',
        sidebar: 'var(--color-sidebar)',
        'sidebar-elevated': 'var(--color-sidebar-elevated)',
        border: 'var(--color-border)',
        'border-strong': 'var(--color-border-strong)',
        brand: 'var(--color-brand)',
        'brand-hover': 'var(--color-brand-hover)',
        'brand-subtle': 'var(--color-brand-subtle)',
        destructive: 'var(--color-destructive)',
        text: {
          primary: 'var(--color-text-primary)',
          secondary: 'var(--color-text-secondary)',
          muted: 'var(--color-text-muted)',
          inverse: 'var(--color-text-inverse)',
        },
        success: 'var(--color-success)',
        warning: 'var(--color-warning)',
        info: 'var(--color-info)',
        danger: 'var(--color-danger)',
      },
      borderRadius: {
        sm: 'var(--radius-sm)',
        DEFAULT: 'var(--radius-md)',
        lg: 'var(--radius-lg)',
      },
      boxShadow: {
        elevated: 'var(--shadow-elevated)',
      },
      zIndex: {
        base: '0',
        sticky: '20',
        dropdown: '40',
        popover: '40',
        mobileNav: '50',
        drawer: '50',
        dialog: '50',
        commandPalette: '60',
        toast: '60',
      },
      fontFamily: {
        sans: ['Inter', 'ui-sans-serif', 'system-ui', '-apple-system', 'BlinkMacSystemFont', 'Segoe UI', 'sans-serif'],
      },
    },
  },
  plugins: [],
}
