import { useTheme } from '../context/ThemeContext.tsx';

const ThemeSwitcher = () => {
    const { theme, setTheme } = useTheme();

    return (
        <div className="flex items-center justify-center gap-4">
            <button
                onClick={() => setTheme('light')}
                className={`px-4 py-2 rounded-lg ${theme.id === 'light' ? 'bg-blue-500 text-white' : 'bg-gray-200'}`}
            >
                Light
            </button>
            <button
                onClick={() => setTheme('dark')}
                className={`px-4 py-2 rounded-lg ${theme.id === 'dark' ? 'bg-blue-500 text-white' : 'bg-gray-200'}`}
            >
                Dark
            </button>
            <button
                onClick={() => setTheme('cyberpunk')}
                className={`px-4 py-2 rounded-lg ${theme.id === 'cyberpunk' ? 'bg-pink-500 text-white' : 'bg-gray-200'}`}
            >
                Cyberpunk
            </button>
            <button
                onClick={() => setTheme('minimalist')}
                className={`px-4 py-2 rounded-lg ${theme.id === 'minimalist' ? 'bg-teal-600 text-white' : 'bg-gray-200'}`}
            >
                Minimalist
            </button>
        </div>
    );
};

export default ThemeSwitcher;