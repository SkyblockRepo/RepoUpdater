<script lang="ts">
	import Footer from '$comp/layout/footer.svelte';
	import Header from '$comp/layout/header/header.svelte';
	import ThemeWatcher from '$comp/misc/theme-watcher.svelte';
	import favicon from '$lib/assets/favicon.svg';
	import { initThemeContext } from '$lib/stores/themes.svelte';
	import '../app.css';

	let { children } = $props();

	initThemeContext();
</script>

<svelte:head>
	<link rel="icon" href={favicon} />
</svelte:head>

<svelte:boundary>
	<Header />

	<div class="min-h-80vh @container h-full">
		{@render children?.()}
	</div>

	<Footer />

	<ThemeWatcher />

	{#snippet pending()}
		<!-- Intentionally left blank -->
	{/snippet}
</svelte:boundary>
