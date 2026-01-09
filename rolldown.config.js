import { defineConfig } from 'rolldown'

export default defineConfig([
    transformTypeScript('./src/WebFormsCore/Scripts/form.ts'),
    transformTypeScript('./src/WebFormsCore/Scripts/webforms-polyfill.ts'),
    transformTypeScript('./src/WebFormsCore.Extensions.Choices/Scripts/choices.ts'),
    transformTypeScript('./src/WebFormsCore.Extensions.TinyMCE/Scripts/tiny.ts')
])

function transformTypeScript(path) {
    return {
        input: path,
        resolve: {
            conditionNames: ['import', 'default']
        },
        moduleTypes: {
            '.css': 'text'
        },
        output: [
            {
                file: path.replace('.ts', '.js'),
                format: 'iife',
            },
            {
                file: path.replace('.ts', '.min.js'),
                format: 'iife',
                minify: true
            }
        ]
    }
}
