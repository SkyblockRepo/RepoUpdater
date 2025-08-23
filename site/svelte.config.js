import adapter from '@sveltejs/adapter-static';
import { vitePreprocess } from '@sveltejs/vite-plugin-svelte';
import { mdsx } from 'mdsx';
import { mdsxConfig } from './mdsx.config.js';

/** @type {import('@sveltejs/kit').Config} */
const config = {
	preprocess: [mdsx(mdsxConfig), vitePreprocess()],

	compilerOptions: {
		experimental: {
			async: true
		}
	},

	kit: {
		adapter: adapter({
			fallback: '404.html'
		}),
		alias: {
			$ui: './src/lib/components/ui',
			$comp: './src/lib/components',
			$lib: './src/lib',
			$params: './src/lib/params',
			'$docs/*': '.velite/*'
		}
	},

	extensions: ['.svelte', '.md'],

	env: {
		publicPrefix: 'PUBLIC_'
	}
};

export default config;
