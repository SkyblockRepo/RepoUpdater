<script lang="ts">
	import ModeToggle from '$comp/misc/mode-toggle.svelte';
	import Button from '$ui/button/button.svelte';
	import { SiGithub } from '@icons-pack/svelte-simple-icons';
</script>

<nav
	class="flex h-16 w-full max-w-4xl items-center justify-between rounded-lg border-2 bg-background px-4 md:m-1"
>
	<div class="flex flex-row gap-4">
		<a href="/" class="text-muted-foreground">Home</a>
		<a href="/docs" class="text-muted-foreground">Docs</a>
	</div>
	<div class="flex flex-row items-center gap-1">
		<Button href="https://github.com/SkyblockRepo/Repo" variant="ghost">
			<SiGithub />
		</Button>
		<ModeToggle />
	</div>
</nav>
