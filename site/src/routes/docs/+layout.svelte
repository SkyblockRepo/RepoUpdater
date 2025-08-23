<script lang="ts">
	import Footer from '$comp/layout/footer.svelte';
	import AppSidebar from '$comp/sidebar/app-sidebar.svelte';
	import SidebarHeader from '$comp/sidebar/sidebar-header.svelte';
	import * as Sidebar from '$ui/sidebar';
	import type { LayoutProps } from './$types';

	let { children }: LayoutProps = $props();
</script>

<Sidebar.Provider>
	<Sidebar.Root collapsible="icon" class="z-50">
		<AppSidebar />
	</Sidebar.Root>

	<div class="max-h-screen flex-1 overflow-y-auto">
		<Sidebar.Inset>
			<SidebarHeader />

			<div>
				{@render children?.()}
				<Footer />
			</div>
		</Sidebar.Inset>
	</div>
</Sidebar.Provider>
