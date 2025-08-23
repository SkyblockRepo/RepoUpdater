<script lang="ts">
	import favicon from '$lib/assets/favicon.svg';
	import { siteConfig } from '$lib/config';
	import { initPageContext } from '$lib/hooks/page.svelte';
	import { useSiteConfig } from '$lib/hooks/use-site-config.svelte';
	import { initThemeContext } from '$lib/stores/themes.svelte';
	import '../app.css';

	let { children } = $props();

	initThemeContext();
	initPageContext();

	useSiteConfig(() => siteConfig);
</script>

<svelte:head>
	<link rel="icon" href={favicon} />
</svelte:head>

<svelte:boundary>
	{@render children?.()}

	{#snippet pending()}
		<!-- Intentionally left blank -->
	{/snippet}
</svelte:boundary>
