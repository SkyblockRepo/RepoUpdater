<script lang="ts">
	import * as Sidebar from '$lib/components/ui/sidebar/index.js';
	import { getDocsSidebar } from '$lib/docs';
	import { ScrollArea } from '$ui/scroll-area';
	import type { Snippet } from 'svelte';
	import NavMain from './nav-main.svelte';

	let { children }: { children?: Snippet } = $props();

	const nav = await getDocsSidebar();
</script>

<Sidebar.Root>
	<Sidebar.Header />
	<div class="flex h-full flex-col overflow-hidden">
		<ScrollArea class="h-full" orientation="vertical">
			<Sidebar.Content class="gap-0">
				{#each nav as item (item.category)}
					<NavMain items={item.docs} title={item.category} />
				{/each}
				{@render children?.()}
			</Sidebar.Content>
		</ScrollArea>
	</div>
	<Sidebar.Footer />
</Sidebar.Root>
