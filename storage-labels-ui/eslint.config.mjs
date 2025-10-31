import typescriptEslint from '@typescript-eslint/eslint-plugin';
import typescriptParser from '@typescript-eslint/parser';
import react from 'eslint-plugin-react';
import reactHooks from 'eslint-plugin-react-hooks';

export default [
    {
        files: ['**/*.ts', '**/*.tsx'],
        languageOptions: {
            parser: typescriptParser,
            parserOptions: {
                ecmaVersion: 2018,
                sourceType: 'module',
            },
        },
        plugins: {
            '@typescript-eslint': typescriptEslint,
            'react': react,
            'react-hooks': reactHooks,
        },
        rules: {
            ...typescriptEslint.configs.recommended.rules,
            ...react.configs.recommended.rules,
            '@typescript-eslint/explicit-module-boundary-types': 'off',
            '@typescript-eslint/no-unused-vars': ['warn', { 
                argsIgnorePattern: '^_',
                varsIgnorePattern: '^_'
            }],
            'react-hooks/rules-of-hooks': 'error',
            'react-hooks/exhaustive-deps': 'off',
            'react/prop-types': 'off',
        },
        settings: {
            react: {
                pragma: 'React',
                version: 'detect',
            },
        },
    },
];
