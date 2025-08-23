<script lang="ts">
	import Button from '$ui/button/button.svelte';
</script>

<!--
@component

- Hero component with a navigation header, main hero content with call-to-action buttons, and a customer showcase section. The component includes smooth scroll-based animations and a responsive mobile menu.

@example
```svelte
<HeroOne />
```
 -->

<div class="overflow-hidden">
	<div
		class="pointer-events-none absolute inset-0 isolate hidden opacity-65 contain-strict lg:block"
	>
		<div
			class="absolute top-0 left-0 h-320 w-140 -translate-y-87.5 -rotate-45 rounded-full bg-[radial-gradient(68.54%_68.72%_at_55.02%_31.46%,hsla(0,0%,85%,.08)_0,hsla(0,0%,55%,.02)_50%,hsla(0,0%,45%,0)_80%)]"
		></div>
		<div
			class="absolute top-0 left-0 h-320 w-60 [translate:5%_-50%] -rotate-45 rounded-full bg-[radial-gradient(50%_50%_at_50%_50%,hsla(0,0%,85%,.06)_0,hsla(0,0%,45%,.02)_80%,transparent_100%)]"
		></div>
		<div
			class="absolute top-0 left-0 h-320 w-60 -translate-y-87.5 -rotate-45 bg-[radial-gradient(50%_50%_at_50%_50%,hsla(0,0%,85%,.04)_0,hsla(0,0%,45%,.02)_80%,transparent_100%)]"
		></div>
	</div>
	<section>
		<div class="my:pt-24 relative py-12">
			<div
				class="absolute inset-0 -z-10 size-full [background:radial-gradient(125%_125%_at_50%_100%,transparent_0%,var(--color-background)_75%)]"
			></div>
			<div class="mx-auto max-w-7xl px-6">
				<div class="text-center sm:mx-auto lg:mt-0 lg:mr-auto">
					<h1 class="mt-8 text-6xl text-balance md:text-7xl lg:mt-16 xl:text-[5.25rem]">
						Skyblock Repo
					</h1>
					<p class="mx-auto mt-8 max-w-2xl text-lg text-balance">
						A unified repository for Hypixel Skyblock development
					</p>

					<div class="mt-12 flex flex-col items-center justify-center gap-2 md:flex-row">
						<div class="rounded-xl border bg-foreground/10 p-1">
							<Button href="/docs" size="lg" class="rounded-lg px-5 text-base">
								<span class="text-nowrap">Start Building</span>
							</Button>
						</div>
						<!-- <Button size="lg" variant="ghost" class="rounded-xl px-5">Request a demo</Button> -->
					</div>
				</div>
			</div>
		</div>
	</section>
</div>
