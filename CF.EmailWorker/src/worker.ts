import PostalMime from 'postal-mime';
import { cfLog } from './deepConsoleLog.js';
import { sendErrorEmailFromMailgun, sendResponseEmailFromMailgun } from './email.js';
import type { DotnetManifest, DotnetOrder, ApiResponse, EmailAttachment, EmailProcessingResponse } from './types';

// Helper function to encode ArrayBuffer to Base64
async function arrayBufferToBase64(buffer) {
	const bytes = new Uint8Array(buffer);
	let binary = '';
	for (let i = 0; i < bytes.byteLength; i++) {
		binary += String.fromCharCode(bytes[i]);
	}
	return btoa(binary);
}

function generateBoundary() {
	return `----=_Part_${Math.random().toString(36).substring(2, 15) + Math.random().toString(36).substring(2, 15)}`;
}


export default {
	async email(message, env, ctx) {
		var originalFilename = 'unknown file name';
		try {
			// Check if the email is from an allowed sender
			const allowedSenders = env.ALLOWED_SENDERS?.toLowerCase().split(',') || [];
			const fromAddress = message.from.toLowerCase().trim();

			if (!allowedSenders.includes(fromAddress)) {
				cfLog('worker.ts',`Email from unauthorized sender: ${fromAddress}`);
				return;
			}
			cfLog('worker.ts',`Processing email from: ${fromAddress}`);
			const parsedEmail = await PostalMime.parse(message.raw);

			if(parsedEmail?.to[0].address === "dev_noreply@ftg.vectorpixel.net")
			{
				cfLog('worker.ts', 'Email sent to dev worker, forwarding to dev worker');
				await sendToDevWorker(env, message);
				return;
			}

			if(parsedEmail.subject.includes("config=skip_db"))
			{
				cfLog('worker.ts', 'Email subject contains config=skip_db, skipping DB processing');
				env.SKIP_DB_CHECK = "true";
			}

			const attachments: EmailAttachment[] = [];
			if (parsedEmail.attachments.length === 0) {
				cfLog('worker.ts','No attachments found');
				await sendErrorEmailFromMailgun(env, fromAddress, 'No attachments found in your email.');
				return;
			} else {
				parsedEmail.attachments.forEach((attachment) => {
					if (attachment.filename !== null && (attachment.filename.toLowerCase().includes('.pdf') || attachment.filename.toLowerCase().includes('.csv') || attachment.filename.toLowerCase().includes('.xlsx'))) {
						var filename = attachment.filename.toLowerCase().split('.');
						switch (filename[filename.length - 1]) {
							case 'pdf':
								cfLog('worker.ts',`Found PDF attachment: ${attachment.filename}`);
								var attachmentData = attachment.content;
									attachments.push({
										filename: attachment.filename,
										fileType: 'pdf',
										data: attachmentData,
										contentType: attachment.mimeType || 'application/pdf',
									});
								break;
							case 'csv':
								cfLog('worker.ts',`Found CSV attachment: ${attachment.filename}`);
								var attachmentData = attachment.content;
									attachments.push({
										filename: attachment.filename,
										fileType: 'csv',
										data: attachmentData,
										contentType: attachment.mimeType || 'text/csv',
									});
								break;
							case 'xlsx':
								cfLog('worker.ts',`Found XLSX attachment: ${attachment.filename}`);
								var attachmentData = attachment.content;
									attachments.push({
										filename: attachment.filename,
										fileType: 'xlsx',
										data: attachmentData,
										contentType: attachment.mimeType || 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
									});
								break;
							default:
								cfLog('worker.ts',`Found attachment with unsupported file type: ${attachment.filename}`);
								return; // Skip unsupported file types
						}

					}
				});
			}
			cfLog('worker.ts',`Found ${attachments.length} attachment(s)`);
			const apiResponses: ApiResponse[] = [];
			for (const attachment of attachments) {
				try {
					const apiResponse = await sendToApi(env, attachment);
					cfLog('worker.ts',`API response for ${attachment.filename}:`, apiResponse.message);
					if (!apiResponse || apiResponse.message !== 'success') {
						originalFilename = attachment.filename;
						throw new Error(`API call failed for ${attachment.filename}: ${apiResponse ? apiResponse.error : 'No response'}`);
					}
					apiResponse.originalFilename = attachment.filename;
					apiResponses.push(apiResponse);
					originalFilename = attachment.filename;
				}
				catch (error) {
					cfLog('worker.ts',`Failed to process ${attachment.filename}:`, error);
					apiResponses.push({
						message: 'Unable to process attachment',
						error: error.message,
						originalFilename: attachment.filename,
					});
					throw new Error(`Error processing attachment: ${error.message}`);
				}
			}
			cfLog('worker.ts',`Processed ${apiResponses.length} attachment(s)`);
			var res = await sendResponseEmailFromMailgun(env, fromAddress, apiResponses);
			if (res.status === 200) {
				cfLog('worker.ts',`Response email sent to ${fromAddress} with status: ${res.status} and message: ${res.message}`);
			}
			else
			{
				throw new Error (`Failed to send response email: ${res.message}`);
			}
		} catch (error) {
			cfLog('worker.ts','Error processing email:', error);
			if (message.from) {
				await sendErrorEmailFromMailgun(env, message.from,  `Error processing your email: ${error.message}`, originalFilename);
			}
		}
	},

	// Simulating a email sent to the worker via fetch
	async fetch(request, env, ctx) {
		if (request.method !== 'POST') {
		return new Response('Method Not Allowed', { status: 405 });
		}

		const contentType = request.headers.get('content-type') || '';
		let fromAddress;
		let toAddress = env.FROM_EMAIL; // Default to FROM_EMAIL from environment
		let subject = 'Simulated Email'; // Default subject
		let textBody = ''; // Default text body
		let htmlBody = ''; // Default HTML body
		let attachments: any[] = []; // Array of {filename, contentType, data (ArrayBuffer)}
		let rawEmailContent;

		try {
			if (contentType.includes('application/json')) {
				const body = await request.json();
				fromAddress = body.from;

				rawEmailContent = body.rawEmail;

				if (!fromAddress || !rawEmailContent) {
				return new Response(
					JSON.stringify({
					error: 'Missing "from" or "rawEmail" in JSON body.',
					}),
					{ status: 400, headers: { 'Content-Type': 'application/json' } },
				);
				}
			} else if (contentType.includes('multipart/form-data')) {
				const formData = await request.formData();
				toAddress = formData.get('to') || toAddress; // Optional 'to' field
				fromAddress = formData.get('from');
				subject = formData.get('subject') || subject;
				textBody = formData.get('text') || textBody;
				htmlBody = formData.get('html') || htmlBody;
				cfLog('worker.ts',`Received FormData with from: ${fromAddress}, subject: ${subject}, text: ${textBody}, html: ${htmlBody}, attachments: ${formData.getAll('file').length}`);
				if (!fromAddress) {
					cfLog('worker.ts','Missing "from" in FormData.');
					return new Response(
						JSON.stringify({
						error: 'Missing "from" in FormData.',
						}),
						{ status: 400, headers: { 'Content-Type': 'application/json' } },
						);
				}
					// Collect all file attachments
					for (const [key, value] of formData.entries()) {
					if (value instanceof File) {
						attachments.push({
							filename: value.name,
							contentType: value.type || 'application/octet-stream',
							data: await value.arrayBuffer(),
						});
					}
				}

				cfLog('worker.ts',
				`Simulating email from ${fromAddress} with ${attachments.length} attachments via fetch (FormData).`,
				);
			} else {
				return new Response('Unsupported Content-Type', { status: 415 });
			}

			// --- Construct the raw email content with attachments ---
			const boundary = generateBoundary();
			let rawEmailParts = [
			`From: ${fromAddress}`,
			`To: ${toAddress}`, // Or a configurable 'to' address
			`Subject: ${subject}`,
			`MIME-Version: 1.0`,
			`Content-Type: multipart/mixed; boundary="${boundary}"`,
			'', // Empty line after headers
			];

			// Add text body part
			if (textBody) {
			rawEmailParts.push(`--${boundary}`);
			rawEmailParts.push(`Content-Type: text/plain; charset="utf-8"`);
			rawEmailParts.push(`Content-Transfer-Encoding: quoted-printable`); // Or 7bit/8bit if simpler content
			rawEmailParts.push('');
			rawEmailParts.push(textBody);
			}

			// Add HTML body part (if available, generally a multipart/alternative would be used)
			if (htmlBody) {
			rawEmailParts.push(`--${boundary}`);
			rawEmailParts.push(`Content-Type: text/html; charset="utf-8"`);
			rawEmailParts.push(`Content-Transfer-Encoding: quoted-printable`);
			rawEmailParts.push('');
			rawEmailParts.push(htmlBody);
			}

        	// Add attachments
			for (const attach of attachments) {
			const base64Data = await arrayBufferToBase64(attach.data);
			rawEmailParts.push(`--${boundary}`);
			rawEmailParts.push(`Content-Type: ${attach.contentType}; name="${attach.filename}"`);
			rawEmailParts.push(`Content-Transfer-Encoding: base64`);
			rawEmailParts.push(`Content-Disposition: attachment; filename="${attach.filename}"`);
			rawEmailParts.push('');
			rawEmailParts.push(base64Data);
			}

			// End boundary
			rawEmailParts.push(`--${boundary}--`);

			rawEmailContent = rawEmailParts.join('\r\n'); // Join with CRLF

			const simulatedMessage = {
				from: fromAddress,
				raw: rawEmailContent,
			};

			await this.email(simulatedMessage, env, ctx);

			return new Response(
				JSON.stringify({
				message: `Email processing simulated for ${fromAddress}. Check logs for details.`,
				}),
				{ status: 200, headers: { 'Content-Type': 'application/json' } },
			);
		} catch (error) {
		cfLog('worker.ts','Error in fetch handler:', error);
		return new Response(
			JSON.stringify({
			error: `Internal server error: ${error.message}`,
			}),
			{ status: 500, headers: { 'Content-Type': 'application/json' } },
		);
		}
	},
};

async function sendToApi(env, attachment: EmailAttachment) : Promise<ApiResponse> {
	const formData = new FormData();
	const blob = new Blob([attachment.data], { type: attachment.contentType || 'application/pdf' });
	formData.append('file', blob, attachment.filename);
	var endpoint = (env.IS_LOCAL === "true" ? env.DEV_API_ENDPOINT : env.API_ENDPOINT) + (attachment.fileType === 'pdf' ? '/ftg/upload' : '/caf/upload');
	cfLog('worker.ts',`Sending attachment ${attachment.filename} to API endpoint: ${endpoint}`);
	const response = await fetch(endpoint, {
		method: 'POST',
		headers: {
			Authorization: `Bearer ${env.API_TOKEN}`,
			// Do not set Content-Type header; fetch will set it automatically for FormData
		},
		body: formData as BodyInit, // Type assertion for TypeScript compatibility
	});
	if (!response.ok) {
		cfLog('worker.ts',`API request failed: ${response.status} ${response.statusText}`);
	}
	return await response.json();
}

async function sendToDevWorker(env, message)
{
	const devWorkerUrl = "ftg-email-worker.bryce1020.workers.dev";
	const testEmailURL = `https://${devWorkerUrl}/test-email`;
	let pdfAttachments = [];
	const parsedEmail = await PostalMime.parse(message.raw);
	parsedEmail.attachments.forEach((attachment) => {
		if (attachment.filename !== null && attachment.filename.toLowerCase().includes('.pdf')) {
			var attachmentData = attachment.content;
			pdfAttachments.push({
				filename: attachment.filename,
				data: attachmentData,
				contentType: attachment.mimeType || 'application/pdf',
			});
		}
	});
	let formData = new FormData();
	for (const pdf of pdfAttachments) {
		const pdfBlob = new Blob([pdf.data], { type: 'application/pdf' });
		formData.append('file', pdfBlob, pdf.filename);
	}
	formData.append('from', message.from);
	formData.append('subject', parsedEmail.subject || 'No Subject');
	formData.append('text', parsedEmail.text || 'No text body');

	const response = await fetch(testEmailURL, {
		method: 'POST',
		headers: {
			'Content-Type': 'multipart/form-data',
		},
		body: formData,
	});
	cfLog('worker.ts',`Forwarded email to dev worker at ${devWorkerUrl} with response status: ${response.status}`);

	return;
}
