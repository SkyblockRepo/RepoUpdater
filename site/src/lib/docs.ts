import { getDoc } from '$lib/utils';

export const getAllDocRoutes = async () => {
	const modules = import.meta.glob('/src/docs/**/*.md');
	const entries = [];

	for (const path of Object.keys(modules)) {
		const slug = path.replace('/src/docs/', '').replace('.md', '').replace('/index', '');
		entries.push({ slug });
	}

	return entries;
};

export const getDocsSidebar = async () => {
	const entries = await getAllDocRoutes();
	const navGroups = {} as Record<
		string,
		{ title: string; href: string; order: number | undefined }[]
	>;

	for (const entry of entries) {
		const doc = await getDoc(entry.slug);
		navGroups[doc.metadata.category] ??= [];
		navGroups[doc.metadata.category].push({
			title: doc.title,
			href: `/docs/${entry.slug}`,
			order: doc.metadata.order
		});
	}

	return Object.entries(navGroups).map(([category, docs]) => ({
		category,
		docs: docs.sort((a, b) => {
			if (a.order !== b.order) {
				return (b.order ?? 0) - (a.order ?? 0);
			}
			return a.title.localeCompare(b.title);
		})
	}));
};
