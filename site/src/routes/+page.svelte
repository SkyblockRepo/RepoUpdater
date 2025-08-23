<script lang="ts">
	import Features from '$comp/layout/features.svelte';
	import Footer from '$comp/layout/footer.svelte';
	import Header from '$comp/layout/header/header.svelte';
	import ThemeWatcher from '$comp/misc/theme-watcher.svelte';
	import Hero from './hero.svelte';
</script>

<Header />

<div class="min-h-80vh @container h-full">
	<Hero />

	<Features />
</div>

<Footer />

<ThemeWatcher />
