import { getAllDocRoutes } from '$lib/remote/docs.remote';
import { getDoc } from '$lib/utils';
import type { EntryGenerator, PageLoad } from './$types';

export const prerender = true;

export const entries: EntryGenerator = async () => {
	return getAllDocRoutes();
};

export const load: PageLoad = async ({ params }) => {
	const doc = await getDoc(params.slug || 'default');
	return { ...doc, viewerData: null };
};
