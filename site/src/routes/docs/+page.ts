import { getDoc } from '$lib/utils';
import type { PageLoad } from './$types';

export const prerender = true;

export const load: PageLoad = async () => {
	const doc = await getDoc('index');
	return { ...doc, viewerData: null };
};
