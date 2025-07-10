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
				const file = formData.get('file'); // assuming the form field is named 'file'



				if (!(file instanceof File)) {
					return new Response('Invalid file', { status: 400 });
				}
				cfLog("Received file:", file.name, "of type:", file.type);

				const fname = file.name;
				var fileType = '';
				var mimeType = file.type;
				if(fname.endsWith('.pdf')) {
					fileType = 'pdf';
				} else if(fname.endsWith('.csv')) {
					fileType = 'csv';
				} else if(fname.endsWith('.xlsx')) {
					fileType = 'xlsx';
				}


				// Convert file to array buffer
				const buffer = await file.arrayBuffer();

				// Generate filename (use original name or create new one)
				const filename = file.name || `${Date.now()}.${fileType}`;

				// Upload to R2
				await env.PDF_BUCKET.put(filename, buffer, {
						httpMetadata: {
						contentType: `${mimeType}`,
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

		if(env.IS_DEV == "false")
		{
			const url = new URL(request.url);

			if (url.pathname.startsWith("/api")) {
				const containerInstance = getContainer(env.FTG_PDF_API);
				return await containerInstance.fetch(request);
		}
		}


		return new Response("Not Found", { status: 404 });
	},
} satisfies ExportedHandler<Env>;
