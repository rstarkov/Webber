module.exports = {
    root: true,
    parser: "@typescript-eslint/parser",
    parserOptions: {
        tsconfigRootDir: __dirname,
        project: ["./tsconfig.json", "./tsconfig.node.json"],
    },
    plugins: ["@typescript-eslint"],
    ignorePatterns: ["dist/", "public/"],
    overrides: [{
        // config files that we still want linted but they're not proper project code
        files: [".eslintrc.js", "vite.config.ts"],
        rules: {
            "no-undef": "off",
            "@typescript-eslint/no-unsafe-call": "off",
            "@typescript-eslint/no-unsafe-assignment": "off",
        },
    }],
    extends: [
        "eslint:recommended",
        "plugin:@typescript-eslint/recommended",
        "plugin:@typescript-eslint/recommended-requiring-type-checking",
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
};
