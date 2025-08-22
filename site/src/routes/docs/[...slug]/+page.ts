import { getDoc } from '$lib/utils';
import type { EntryGenerator, PageLoad } from './$types';

export const prerender = true;

export const entries: EntryGenerator = () => {
	console.info('Prerendering /docs');
	const modules = import.meta.glob('/content/**/*.md');
	const entries = [];

	for (const path of Object.keys(modules)) {
		const slug = path.replace('/content/', '').replace('.md', '').replace('/index', '');
		entries.push({ slug });
	}

	return entries;
};

export const load: PageLoad = async ({ params }) => {
	const doc = await getDoc(params.slug);
	return { ...doc, viewerData: null };
};
