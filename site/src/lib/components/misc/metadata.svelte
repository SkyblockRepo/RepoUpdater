<script lang="ts">
	import { page } from '$app/state';
	import { useSiteConfig, type SiteConfig } from '$lib/hooks/use-site-config.svelte.js';

	const siteConfig = useSiteConfig();

	let {
		title = siteConfig.current.name,
		ogImage = siteConfig.current.ogImage,
		description = siteConfig.current.description,
		keywords = siteConfig.current.keywords,
		author = siteConfig.current.author || 'Skyblock Repo'
	}: {
		title?: string;
		ogImage?: SiteConfig['ogImage'];
		description?: string;
		keywords?: string[];
		author?: string;
	} = $props();

	const trueTitle = $derived(
		title === siteConfig.current.name
			? siteConfig.current.name
			: `${title} - ${siteConfig.current.name}`
	);
</script>

<svelte:head>
	<title>{trueTitle}</title>
	<meta name="description" content={description} />
	<meta name="keywords" content={keywords?.join(',')} />
	<meta name="author" content={author} />
	<meta name="twitter:card" content="summary_large_image" />
	<meta name="twitter:site" content={siteConfig.current.url} />
	<meta name="twitter:title" content={title} />
	<meta name="twitter:description" content={description} />
	<meta name="twitter:image:alt" content={title} />
	<meta name="twitter:creator" content={author} />
	{#if ogImage}
		<meta name="twitter:image" content={ogImage?.url} />
		<meta property="og:image" content={ogImage?.url} />
		<meta property="og:image:width" content={ogImage?.width} />
		<meta property="og:image:height" content={ogImage?.height} />
	{/if}
	<meta property="og:title" content={title} />
	<meta property="og:type" content="website" />
	<meta property="og:url" content={siteConfig.current.url + page.url.pathname} />
	<meta property="og:image:alt" content={title} />
	<meta property="og:description" content={description} />
	<meta property="og:site_name" content={siteConfig.current.name} />
	<meta property="og:locale" content="EN_US" />
</svelte:head>
