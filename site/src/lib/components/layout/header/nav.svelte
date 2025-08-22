<script lang="ts">
	import ModeToggle from '$comp/misc/mode-toggle.svelte';
</script>

<nav
	class="flex h-16 w-full max-w-4xl items-center justify-between rounded-lg border-2 bg-background px-4 md:m-1"
>
	<div class="flex flex-row gap-4">
		<a href="/" class="text-muted-foreground">Home</a>
		<a href="/about" class="text-muted-foreground">About</a>
		<a href="/contact" class="text-muted-foreground">Contact</a>
	</div>
	<ModeToggle />
</nav>
