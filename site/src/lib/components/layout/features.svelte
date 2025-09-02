<script lang="ts">
	import { Card, CardContent, CardHeader } from '$ui/card';
	import Settings2 from '@lucide/svelte/icons/settings-2';
	import Sparkles from '@lucide/svelte/icons/sparkles';
	import Zap from '@lucide/svelte/icons/zap';
	import CardDecorator from './card-decorator.svelte';
</script>

<section class="my-8 bg-card/50 py-24">
	<div class="@container mx-auto max-w-5xl px-6">
		<div class="text-center">
			<h2 class="text-4xl font-semibold text-balance lg:text-5xl">Built to cover your needs</h2>
			<p class="mt-4">Bring the latest data to your application with our powerful API and SDKs.</p>
		</div>
		<div
			class="mx-auto mt-8 grid max-w-sm gap-6 *:text-center md:mt-16 @min-4xl:max-w-full @min-4xl:grid-cols-3"
		>
			<Card class="group shadow-black/5">
				<CardHeader>
					<CardDecorator>
						<Zap class="size-6" aria-hidden />
					</CardDecorator>

					<h3 class="mt-6 text-lg font-semibold">Automated</h3>
				</CardHeader>

				<CardContent>
					<p class="text-sm">
						Extensive automation behind the scenes, ensuring up to date and accurate data.
					</p>
				</CardContent>
			</Card>

			<Card class="group shadow-black/5">
				<CardHeader>
					<CardDecorator>
						<Settings2 class="size-6" aria-hidden />
					</CardDecorator>

					<h3 class="mt-6 text-lg font-semibold">Comprehensive</h3>
				</CardHeader>

				<CardContent>
					<p class="text-sm">Should contain all data you need for your application!</p>
				</CardContent>
			</Card>

			<Card class="group shadow-black/5">
				<CardHeader>
					<CardDecorator>
						<Sparkles class="size-6" aria-hidden />
					</CardDecorator>

					<h3 class="mt-6 text-lg font-semibold">Not Powered By AI</h3>
				</CardHeader>

				<CardContent>
					<p class="text-sm">You're welcome!</p>
				</CardContent>
			</Card>
		</div>
	</div>
</section>
