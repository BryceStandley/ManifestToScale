import { Container, getContainer } from "@cloudflare/containers"
import { cfLog } from "./deepConsoleLog"

export class FTG_PDF_API extends Container {
	defaultPort = 8080;
	sleepAfter = '3m';
	envVars : {
		AUTH_SHAREDKEY: env.SECRET_STORE.AUTH_SHAREDKEY,
	}
}


export default {
	async fetch(request, env, ctxy): Promise<Response> {

		if (request.method !== 'POST') {
			return new Response('Method not allowed', { status: 405 });
		}




		try {
				let newRequest = request.clone();
				const formData = await newRequest.formData();
				const pdfFile = formData.get('file'); // assuming the form field is named 'file'


				if (!(pdfFile instanceof File) || pdfFile.type !== 'application/pdf') {
					return new Response('Invalid PDF file', { status: 400 });
				}

				// Convert file to array buffer
				const pdfBuffer = await pdfFile.arrayBuffer();

				// Generate filename (use original name or create new one)
				const filename = pdfFile.name || `pdf-${Date.now()}.pdf`;

				// Upload to R2
				await env.PDF_BUCKET.put(filename, pdfBuffer, {
						httpMetadata: {
						contentType: 'application/pdf',
						},
					});

		}
		catch (error) {
			return new Response(
				JSON.stringify({
				success: false,
				error: error instanceof Error ? error.message : String(error)
				}),
				{
					headers: { 'Content-Type': 'application/json' },
					status: 500,
				}
			);
		}

		const url = new URL(request.url);

		if (url.pathname.startsWith("/api")) {
			const containerInstance = getContainer(env.FTG_PDF_API);
			return await containerInstance.fetch(request);
		}

		return new Response("Not Found", { status: 404 });
	},
} satisfies ExportedHandler<Env>;
