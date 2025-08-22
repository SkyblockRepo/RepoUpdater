<script lang="ts">
	import { dev } from '$app/environment';
	import { page } from '$app/state';
	import Metadata from '$comp/misc/metadata.svelte';
	import TableOfContents from '$comp/toc/toc.svelte';
	import { cn } from '$lib/utils.js';
	import { ScrollArea } from '$ui/scroll-area/index.js';
	import Construction from '@lucide/svelte/icons/construction';
	import TriangleAlert from '@lucide/svelte/icons/triangle-alert';
	import type { PageData } from './$types.js';

	let { data }: { data: PageData } = $props();

	const Markdown = $derived(data.component);
	const doc = $derived(data.metadata);
</script>

<Metadata {...doc} />

{#if !doc.published && dev}
	<div class="fixed top-16 z-10 w-full">
		<p
			class="flex flex-row items-center justify-center gap-2 bg-amber-700/70 py-2 text-lg font-semibold text-white"
		>
			<TriangleAlert />
			Unpublished Post!
		</p>
	</div>
{/if}

<main class="flex flex-row gap-8 px-2 py-6 lg:px-2">
	<div class="flex w-full min-w-0 flex-1 flex-col items-center justify-center">
		<div class="markdown mx-4 max-w-4xl pt-8 pb-12" id="markdown">
			<div class="max-w-4xl space-y-2">
				<h1 class={cn('scroll-m-20 text-4xl font-bold tracking-tight')}>
					{doc.title}
				</h1>
				{#if doc.description}
					<p class="text-base text-balance text-muted-foreground">
						{doc.description}
					</p>
				{/if}
			</div>
			{#if doc.published || dev}
				<Markdown />
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
				{#key page.url.pathname}
					<TableOfContents />
				{/key}
			</ScrollArea>
		</div>
	</div>
</main>
