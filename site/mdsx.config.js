import { defineConfig } from 'mdsx';
import rehypeSlug from 'rehype-slug';
import remarkGfm from 'remark-gfm';
import rehypePrettyCode from 'rehype-pretty-code';
import { fileURLToPath } from 'node:url';
import { resolve } from 'node:path';

const __dirname = fileURLToPath(new URL('.', import.meta.url));

export const mdsxConfig = defineConfig({
	extensions: ['.md'],
	remarkPlugins: [remarkGfm],
	rehypePlugins: [rehypeSlug, rehypePrettyCode],
	blueprints: {
		default: {
			path: resolve(__dirname, './src/lib/components/markdown/blueprint.svelte')
		}
	}
});
