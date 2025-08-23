<script lang="ts">
	import { navigating } from '$app/state';
	import ModeToggle from '$comp/misc/mode-toggle.svelte';
	import NavBreadcrumb from '$comp/sidebar/nav-breadcrumb.svelte';
	import { Separator } from '$ui/separator';
	import * as Sidebar from '$ui/sidebar';
	import { quadInOut } from 'svelte/easing';
	import { slide } from 'svelte/transition';

	const sidebar = Sidebar.useSidebar();
	// let searchOpen = $state(false);
</script>

<header
	class="sticky top-0 z-40 flex h-16 shrink items-center justify-between gap-2 border-b bg-background px-4"
>
	<div class="flex flex-row items-center gap-2">
		<Sidebar.Trigger class="-my-2 -ml-2 size-10" />
		<Separator orientation="vertical" class="mr-2 h-4" />
		<NavBreadcrumb />
	</div>
	<div class="flex items-center justify-between gap-2 md:justify-end">
		{#if sidebar.isMobile}
			<!-- <Button
				onclick={() => (searchOpen = true)}
				class="order-4 border bg-card px-3 py-4"
				variant="outline"
				size="sm"
			>
				<Search />
			</Button> -->
			<!-- <SearchMenu bind:open={searchOpen} useButton={false} {leaderboards} /> -->
		{:else}
			<div class="order-2 w-full flex-1 md:w-auto md:flex-none">
				<!-- <SearchMenu {leaderboards} /> -->
			</div>
		{/if}
		<div class="order-3 flex items-center gap-2">
			<ModeToggle />
		</div>
	</div>
</header>

{#await navigating.complete}
	<div class="sticky top-16 z-40 w-full">
		<div class="relative">
			<div
				class="bg-progress absolute top-0 h-1.5 w-full"
				transition:slide={{ delay: 100, duration: 500, easing: quadInOut }}
			></div>
		</div>
	</div>
{/await}
