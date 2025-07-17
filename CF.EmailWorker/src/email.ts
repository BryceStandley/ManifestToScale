import FormData from 'form-data';
import { cfLog } from './deepConsoleLog';
import { FreshToGoManifestRecord } from './freshToGo';
import { AcknowledgementEmail, EmailAttachment, ResponseEmail, ErrorEmail} from './emailTemplates';
import { Utils } from './utils';
import { recordManifestToDatabase, checkIfManifestHasPreviouslyProcessedSuccessfully } from './database';
import type { ApiResponse, EmailProcessingResponse } from './types';


async function sendResponseEmailFromMailgun(env, toAddress, apiResponses): Promise<EmailProcessingResponse> {
	const attachments: EmailAttachment[] = [];
	var originalFile = '';
	var manifest: FreshToGoManifestRecord = new FreshToGoManifestRecord();

	for (const result of apiResponses) {
		cfLog('email.ts','Processing result for', result.originalFilename);

		originalFile = result.originalFilename || 'ftgmanifest.pdf';
		manifest.OriginalFilename = originalFile;
		manifest.ManifestDate = result.manifestDate || null;
		manifest._processedDateTime = Utils.CurrentDateTimeAWSTShort;
		cfLog('email.ts',`Detected Manifest Date: ${manifest.ManifestDate}`);

		cfLog('email.ts',`Checking if manifest ${manifest.OriginalFilename} has been processed successfully before...`);
		if(env.SKIP_DB_CHECK === "false") {
			const previouslyProcessed = await checkIfManifestHasPreviouslyProcessedSuccessfully(env, manifest);
			if (previouslyProcessed.status == 500) {
				cfLog('email.ts', `Manifest ${manifest.OriginalFilename} with date ${manifest.ManifestDate} has been processed previously.`);
				return {
					message: 'Manifest date was previously recorded as processed successfully... New files wont be sent',
					status: 500
				};
			}
		}
		else
		{
			cfLog('email.ts',`Skipping database check as SKIP_DB_CHECK is set to true.`);
		}

		if (result.message !== 'success' && result.error) {
			cfLog('email.ts',`❌ ${result.originalFilename}: Error - ${result.error}`);
			manifest._lastError = result.error;
			manifest._status = 2;
			continue;
		}
		else if(result.message == 'success' && result.error)
		{
			// Log the errors/warnings but still process the manifest. Most likely this is a warning about missing data or dulicate shipments.
			cfLog('email.ts',`⚠️ ${result.originalFilename}: Warning - ${result.error}`);
		}

		const responseFiles = convertApiResponseToFiles(result);
		attachments.push(...responseFiles);
		manifest._receiptXml = result.receiptXmlContent || '';
		manifest._shipmentXml = result.shipmentXmlContent || '';
		manifest._totalShipments = result.totalOrders || 0;
		manifest._totalCrates = result.totalCrates || 0;
		manifest._receiptId = 'Receipt-FTG-' + Utils.GetSimpleScaleDateString(manifest.ManifestDate);
		manifest._status = 1;
		manifest._delivered = false;
		manifest._lastError = '';
		manifest._id = (await env.DB.prepare('SELECT COUNT(*) FROM "processed_manifests"').all()).results[0]['COUNT(*)'] + 1;
		manifest.company = result.company || 'PER-CO-FTG'; // Default to FTG if not provided
	}


	// Save the manifest to the database
	const dbRes = await recordManifestToDatabase(env, manifest);
	cfLog('email.ts','Database record result:', dbRes);

	var email = new ResponseEmail();
	if(toAddress === 'bryce@vectorpixel.net')
	{
		email.init([], originalFile, manifest);
	}
	else
	{
		email.init([toAddress], originalFile, manifest);
	}
	email.attachments = attachments;

	const form = new FormData();
	form.append('from', email.from);
	form.append('to', email.to);
	form.append('subject', email.subject);
	form.append('html', email.html);

	for (const attachment of attachments) {
		const blob = new Blob([attachment.content], {
			type: attachment.type || 'application/xml',
		});
		form.append('attachment', blob, attachment.filename);
	}

	var delayed = false;
	if (Utils.CurrentAWSTDateTime < Utils.getEmailDeliveryDT(manifest.ManifestDate)) {
		var delTime = Utils.getEmailDeliveryDateTimeRF2822(manifest.ManifestDate);
		cfLog('email.ts',`Setting delivery time to: ${delTime}`);
		form.append('o:deliverytime', delTime);
		cfLog('email.ts',`Email scheduled for delivery at: ${delTime}`);
		delayed = true;
	} else {
		cfLog('email.ts','Email will be sent immediately');
	}

	try {
		var res = await sendEmailFromMailgun(env, form);
		if (!res || res.status !== 200 || res.message !== 'Email sent successfully') {
			cfLog('email.ts','Error sending email:', res);
			return {message: 'Failed to send email', status: 500 };
		}

		cfLog('email.ts','Email sent successfully:', res);

		if(delayed)
		{
			var res2 = await sendAcknowledgementEmail(env, toAddress, originalFile, manifest.ManifestDate, manifest.company);
			if (!res2 || res2.status !== 200 || res.message !== 'Email sent successfully') {
				cfLog('email.ts','Error sending acknowledgement email:', res2);
				return { message:'Failed to send acknowledgement email', status: 500 };
			}
			cfLog('email.ts','Acknowledgement email sent successfully');
		}
		return { message: 'Email and Acknowledgement sent successfully', status: 200 };
	} catch (error) {
		return {message: error.message, status: 500 };
	}
}

async function sendEmailFromMailgun(env, emailForm, emailType = 'response') {
	try {
		if(env.SKIP_EMAIL_SEND === "true")
		{
			cfLog('email.ts',`Skipping email send as SKIP_EMAIL_SEND is set to true. Email Type: ${emailType}, all logs after this regarding email sending can be ignored.`);
			return { message: 'Email sent successfully', status: 200 };
		}

		const resp = await fetch(`https://api.mailgun.net/v3/ftg.vectorpixel.net/messages`, {
			method: 'POST',
			headers: {
				Authorization: 'Basic ' + Buffer.from('api:' + env.MAILGUN_API_KEY).toString('base64'),
			},
			body: emailForm,
		});
		const data = await resp.text();
		cfLog('email.ts',`Email ${emailType} Mailgun server reply:`, data);
		return { message: 'Email sent successfully', status: 200 };
	} catch (error) {
		return { message: `Failed to send email with error: ${error.message}`, status: 500 };
	}
}

async function sendAcknowledgementEmail(env, toAddress, originalFile, manifestDate, company) {
	var acknowledgementEmails = env.ACKNOWLEDGEMENT_EMAILS?.toLowerCase().split(',') || [];
	var manifest = new FreshToGoManifestRecord();
	manifest.OriginalFilename = originalFile;
	manifest.ManifestDate = manifestDate;
	manifest.company = company;
	var email =  new AcknowledgementEmail();
	if(toAddress === 'bryce@vectorpixel.net')
	{
		email.init([], originalFile, manifest);
	}
	else
	{
		email.init([`${toAddress}`, ...acknowledgementEmails], originalFile, manifest);
	}

	const form = new FormData();
	form.append('from', email.from);
	form.append('to', email.to);
	form.append('subject', email.subject);
	form.append('html', email.html);

	try {
		var res = await sendEmailFromMailgun(env, form, 'Acknowledgement');
		if (!res || res.status !== 200) {
			cfLog('email.ts','Error sending acknowledgement email:', res);
			await sendErrorEmailFromMailgun(env, toAddress, `Failed to send acknowledgement email: ${res.message}`);
			return new Response('Failed to send acknowledgement email', { status: 500 });
		}
		cfLog('email.ts','Acknowledgement email sent successfully');
		return { message: 'Acknowledgement Email sent successfully', status: 200 };
	}
	catch (error)
	{
		cfLog('email.ts','Error sending acknowledgement email:', error);
		return new Response(error.message, { status: 500 });
	}

}

async function sendErrorEmailFromMailgun(env, toAddress, message, originalFilename = 'unknown file') {
	var email = new ErrorEmail();
	if(toAddress === 'bryce@vectorpixel.net')
	{
		email.init([], originalFilename, message);
	}
	else
	{
		email.init([toAddress], originalFilename, message);
	}

	const form = new FormData();
	form.append('from', email.from);
	form.append('to', email.to);
	form.append('subject', email.subject);
	form.append('html', email.html);

	try {
		var res = await sendEmailFromMailgun(env, form);
		if (!res || res.status !== 200) {
			cfLog('email.ts','Error sending error email:', res);
			return new Response('Failed to send error email', { status: 500 });
		}
		return { message: 'Error Email sent successfully', status: 200 };
	} catch (error) {
		return new Response(error.message);
	}
}

function convertApiResponseToFiles(apiResponse: ApiResponse): EmailAttachment[] {
	const attachments: EmailAttachment[] = [];
	const manifestDate = apiResponse.manifestDate;
	const company = apiResponse.company === 'PER-CO-FTG' ? 'FTG' : 'CAF';
	const receiptId = 'Receipt-' + company + '-' + Utils.GetSimpleScaleDateString(manifestDate);
	const shipmentId = 'Shipments-' + company + '-' + Utils.GetSimpleScaleDateString(manifestDate);
	const baseName = 'PER-CO-' + company + '_';
	if (apiResponse.receiptXmlContent) {
		//const receiptBase64 = btoa(apiResponse.receiptXmlContent);
		attachments.push({
			content: apiResponse.receiptXmlContent,
			filename: `${baseName}${receiptId}.rcxml`,
			type: 'application/xml',
			disposition: 'attachment',
		});
	}
	if (apiResponse.shipmentXmlContent) {
		//const shipmentBase64 = btoa(apiResponse.shipmentXmlContent);
		attachments.push({
			content: apiResponse.shipmentXmlContent,
			filename: `${baseName}${shipmentId}.shxml`,
			type: 'application/xml',
			disposition: 'attachment',
		});
	}
	return attachments;
}

export { sendResponseEmailFromMailgun, sendErrorEmailFromMailgun};
