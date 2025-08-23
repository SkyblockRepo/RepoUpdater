<script lang="ts">
	import { PUBLIC_COMMIT_HASH } from '$env/static/public';
</script>

<footer class="border-t border-border/50 bg-background/50 py-8 backdrop-blur-sm">
	<div class="flex w-full flex-wrap items-start justify-center gap-4 px-4 md:gap-8 md:px-8">
		{#if PUBLIC_COMMIT_HASH !== ''}
			<p class="flex-1 text-xs text-muted-foreground/50">
				Running version <a
					href="https://github.com/EliteFarmers/Website/commit/{PUBLIC_COMMIT_HASH}"
					target="_blank"
					class="rounded-sm border bg-card p-0.5 text-muted-foreground hover:underline"
					>{PUBLIC_COMMIT_HASH.slice(0, 7)}</a
				>
			</p>
		{/if}
		<p class="text-xs text-muted-foreground/50">Not affiliated with Hypixel, Inc.</p>
		<p class="text-xs text-muted-foreground/50">
			NOT AN OFFICIAL MINECRAFT PRODUCT. NOT APPROVED BY OR ASSOCIATED WITH MOJANG OR MICROSOFT.
		</p>
	</div>
</footer>
