import js from "@eslint/js";
import tsParser from "@typescript-eslint/parser";
import globals from "globals";
import path from "node:path";
import { fileURLToPath } from "node:url";
import tseslint from "typescript-eslint";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

export default tseslint.config(
    {
        ignores: ["dist", "public"],
    },
    {
        files: ["**/*.{ts,tsx}"],
        languageOptions: {
            ecmaVersion: 5,
            globals: globals.browser,
            parser: tsParser,
            parserOptions: {
                tsconfigRootDir: __dirname,
                project: ["./tsconfig.json", "./tsconfig.node.json"],
            },
        },
        extends: [
            js.configs.recommended,
            ...tseslint.configs.recommended,
            ...tseslint.configs.recommendedTypeChecked,
        ],
        rules: {
            // all auto-fixable issues are "warn" style and the build auto-fixes them instead of failing
            "quotes": ["warn", "double", { avoidEscape: true }],
            "jsx-quotes": ["warn", "prefer-double"],
            "quote-props": ["warn", "consistent-as-needed"],
            "indent": ["warn", 4],
            "comma-dangle": ["warn", "always-multiline"],
            "prefer-const": "warn",
            "sort-imports": ["warn", { ignoreCase: true, ignoreDeclarationSort: true }], // auto-sorting import statements is not safe :(
            "eol-last": ["warn", "always"],
            "no-trailing-spaces": "warn",
            "@typescript-eslint/no-unused-vars": "off",
            "@typescript-eslint/no-empty-function": "off",
            "@typescript-eslint/no-explicit-any": "off",
            "@typescript-eslint/no-unsafe-argument": "off",
            "@typescript-eslint/no-inferrable-types": "off",
            "@typescript-eslint/restrict-plus-operands": "off",
            "@typescript-eslint/no-non-null-assertion": "off",
        },
    },
    {
        files: ["**/eslint.config.js", "**/vite.config.ts"],
        extends: [
            js.configs.recommended,
        ],
        rules: {
            "quotes": ["warn", "double", { avoidEscape: true }],
            "indent": ["warn", 4],
            "comma-dangle": ["warn", "always-multiline"],
            "prefer-const": "warn",
            "no-undef": "off",
            "eol-last": ["warn", "always"],
            "no-trailing-spaces": "warn",
            "@typescript-eslint/no-unsafe-call": "off",
            "@typescript-eslint/no-unsafe-assignment": "off",
        },
    },
);
