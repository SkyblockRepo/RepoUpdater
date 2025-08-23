import { defineSiteConfig } from '$lib/hooks/use-site-config.svelte.js';

export const siteConfig = defineSiteConfig({
	name: 'Skyblock Repo',
	url: 'https://skyblockrepo.com',
	description: 'Skyblock Repo is a comprehensive resource for Skyblock developers!',
	// ogImage: {
	// 	url: 'https://skyblockrepo.com/og.png',
	// 	height: '630',
	// 	width: '1200'
	// },
	author: 'Skyblock Repo',
	keywords: ['skyblock', 'skyblockrepo', 'repository', 'development', 'hypixel']
});

export type SiteConfig = typeof siteConfig;
