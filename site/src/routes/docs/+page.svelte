<script lang="ts">
	import DocPage from '$comp/docs/doc-page.svelte';
	import type { PageData } from './$types.js';

	let { data }: { data: PageData } = $props();
</script>

<DocPage {...data} />
