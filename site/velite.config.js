// @ts-check
import { defineCollection, defineConfig, s } from 'velite';

const docSchema = s
	.object({
		title: s.string(),
		description: s.string(),
		path: s.path(),
		category: s.string(),
		order: s.number().optional(),
		tags: s.array(s.string()).default([]),
		author: s.string().default(''),
		date: s.string().default(''),
		navLabel: s.string().optional(),
		published: s.boolean().default(false),
		component: s.boolean().default(false)
	})
	.transform((data) => {
		return {
			...data,
			slug: data.path.split('/').slice(1).join('/'),
			slugFull: `/${data.path}`
		};
	});

const docs = defineCollection({
	name: 'Doc',
	pattern: './**/*.md',
	schema: docSchema
});

export default defineConfig({
	root: './src/docs',
	collections: {
		docs
	}
});
