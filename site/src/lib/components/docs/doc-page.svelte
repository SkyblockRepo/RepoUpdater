<script lang="ts">
	import { dev } from '$app/environment';
	import { page } from '$app/state';
	import Metadata from '$comp/misc/metadata.svelte';
	import TableOfContents from '$comp/toc/toc.svelte';
	import { type DocData } from '$lib/utils.js';
	import { ScrollArea } from '$ui/scroll-area/index.js';
	import Construction from '@lucide/svelte/icons/construction';

	let { component: Component, metadata }: DocData = $props();
</script>

<Metadata {...metadata} />

<!-- {#if !doc.published && dev}
	<div class="fixed top-16 z-10 w-full">
		<p
			class="flex flex-row items-center justify-center gap-2 bg-amber-700/70 py-2 text-lg font-semibold text-white"
		>
			<TriangleAlert />
			Unpublished Post!
		</p>
	</div>
{/if} -->

<main class="flex w-full flex-row justify-center gap-8 px-2 py-6 lg:px-2">
	<div class="flex w-full max-w-5xl min-w-0 flex-1 flex-col">
		<div class="markdown mx-4 max-w-4xl pt-8 pb-12" id="markdown">
			<div class="max-w-4xl space-y-2">
				<h1 class="scroll-m-20 text-4xl font-bold tracking-tight">
					{metadata.title}
				</h1>
				{#if metadata.description}
					<p class="text-base text-balance text-muted-foreground">
						{metadata.description}
					</p>
				{/if}
			</div>
			{#if metadata.published || dev}
				<Component />
			{:else}
				<div class="container mx-auto rounded-md border-2 bg-card py-8">
					<div class="flex flex-col items-center justify-evenly gap-8 md:flex-row">
						<Construction class="size-24" />
						<div class="flex flex-col items-center">
							<h2 class="text-4xl font-bold">Under Construction!</h2>
							<p class="text-muted-foreground">
								This post is currently being written. Check back later!
							</p>
						</div>
					</div>
				</div>
			{/if}
		</div>
	</div>
	<div class="hidden w-full max-w-64 text-sm xl:block">
		<div class="fixed top-24 -mt-10 h-[calc(100vh-32rem)] py-8">
			<ScrollArea class="h-full">
				{#key page.url.pathname + metadata.title}
					<TableOfContents />
				{/key}
			</ScrollArea>
		</div>
	</div>
</main>
