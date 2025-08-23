import { getDoc } from '$lib/utils';
import type { EntryGenerator, PageLoad } from './$types';

export const prerender = true;

export const entries: EntryGenerator = async () => {
	const modules = import.meta.glob('/src/docs/**/*.md');
	const entries = [];

	for (const path of Object.keys(modules)) {
		const slug = path.replace('/src/docs/', '').replace('.md', '').replace('/index', '');
		entries.push({ slug });
	}

	return entries;
};

export const load: PageLoad = async ({ params }) => {
	const doc = await getDoc(params.slug || 'index');
	return { ...doc, viewerData: null };
};
