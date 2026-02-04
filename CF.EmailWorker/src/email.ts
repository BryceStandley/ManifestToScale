import FormData from 'form-data';
import { cfLog } from './deepConsoleLog';
import { FreshToGoManifestRecord } from './freshToGo';
import { AcknowledgementEmail, ResponseEmail, ErrorEmail} from './emailTemplates';
import { Utils } from './utils';
import { recordManifestToDatabase, checkIfManifestHasPreviouslyProcessedSuccessfully } from './database';
import type { ApiResponse, EmailProcessingResponse, Attachment } from './types';
import { Guid } from "ez-guid";


async function sendResponseEmailFromMailgun(env, toAddress, apiResponses): Promise<EmailProcessingResponse> {
	const attachments: Attachment[] = [];
	var originalFile = '';
	var manifest: FreshToGoManifestRecord = new FreshToGoManifestRecord();

	for (const result of apiResponses) {
		cfLog('email.ts','Processing result for', result.originalFilename);

		const company = result.company === 'PER-CO-FTG' ? 'FTG' : (result.manifest.company.vendorNumber === '856946' ? 'CAF' : 'CTG');

		const responseFiles = convertApiResponseToFiles(result);
		originalFile = result.originalFilename || 'ftgmanifest.pdf';
		manifest.OriginalFilename = originalFile;
		manifest.ManifestDate = result.manifestDate || null;
		manifest._processedDateTime = Utils.CurrentDateTimeAWSTShort;
		manifest._receiptXml = result.xmlContent.receiptContent || '';
		manifest._shipmentXml = result.xmlContent.shipmentContent || '';
		manifest._totalShipments = result.totalOrders || 0;
		manifest._totalCrates = result.totalCrates || 0;
		const receiptId = 'Receipt-' + company + '-' + Utils.GetSimpleScaleDateString(manifest.ManifestDate);
		manifest._receiptId = receiptId;
		manifest._status = 1;
		manifest._delivered = false;
		manifest._lastError = '';
		manifest._id = 0; // Default to 0 for new record
		manifest.company = result.company || 'PER-CO-FTG'; // Default to FTG if not provided
		manifest.vendor = env.MANIFEST_VENDOR  === '856946' ? 'Azura_Fresh' : 'Theme_Group' ;

		manifest.processingMessages = result.correctionMessages || null;

		cfLog('email.ts',`Detected Manifest Date: ${manifest.ManifestDate}`);

		cfLog('email.ts',`Checking if manifest ${manifest.OriginalFilename} has been processed successfully before...`);
		if(env.SKIP_DB_CHECK === "false") {
			manifest._id = (await env.DB.prepare('SELECT COUNT(*) FROM "processed_manifests"').all()).results[0]['COUNT(*)'] + 1; // generate new ID based on count of existing records only if not skipping DB check
			const previouslyProcessed = await checkIfManifestHasPreviouslyProcessedSuccessfully(env, manifest);
			if (previouslyProcessed.status == 500) {
				cfLog('email.ts', `Manifest ${manifest.OriginalFilename} with date ${manifest.ManifestDate} has been processed previously with total crates: ${previouslyProcessed.totalCrates} and total orders: ${previouslyProcessed.totalShipments}.`);
				if(previouslyProcessed.totalCrates !== manifest._totalCrates || previouslyProcessed.totalShipments !== manifest._totalShipments)
				{
					cfLog('email.ts', `Manifest was previously processed successfully but the total crates or total orders have changed. Sending new files...`);
				}
				else
				{
					return {
						message: 'Manifest date was previously recorded as processed successfully... New files wont be sent',
						status: 500
					};
				}
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
		
		if(result.message == 'success')
		{
			// Log any errors/warnings but still process the manifest.
			if(result.error)
			{
				cfLog('email.ts',`⚠️ ${result.originalFilename}: Warning - ${result.error}`);
			}

			if(manifest.processingMessages?.warnings !== undefined && manifest.processingMessages?.errors !== undefined)
			{
				if(manifest.processingMessages?.warnings.length > 0 || manifest.processingMessages?.errors.length > 0)
				{
					cfLog('email.ts',`⚠️ ${result.originalFilename}: Processing Messages detected - see below logs for details.`);
					for(const warnMsg of manifest.processingMessages?.warnings || [])
					{
						cfLog('email.ts',`⚠️ Warning Message: ${warnMsg}`);
					}
					for(const errMsg of manifest.processingMessages?.errors || [])
					{
						cfLog('email.ts',`❌ Error Message: ${errMsg}`);
					}
				}
			}
			
		}

		attachments.push(...responseFiles);

	}

	if(env.SKIP_DB_CHECK === "false") {
		// Save the manifest to the database
		const dbRes = await recordManifestToDatabase(env, manifest);
		cfLog('email.ts','Database record result:', dbRes);
	}
	else
	{
		cfLog('email.ts',`Skipping recording manifest to database as SKIP_DB_CHECK is set to true.`);
	}

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

		if(delayed || env.FORCE_SEND_ACK === "true")
		{
			var res2 = await sendAcknowledgementEmail(env, toAddress, originalFile, manifest);
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

async function sendAcknowledgementEmail(env, toAddress, originalFile, manifest) {
	var acknowledgementEmails = env.ACKNOWLEDGEMENT_EMAILS?.toLowerCase().split(',') || [];
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

function convertApiResponseToFiles(apiResponse: ApiResponse): Attachment[] {
	const attachments: Attachment[] = [];
	const manifestDate = apiResponse.manifestDate;
	const company = apiResponse.company === 'PER-CO-FTG' ? 'FTG' : (apiResponse.manifest.company.vendorNumber === '856946' ? 'CAF' : 'CTG');
	const receiptId = 'Receipt-' + company + '-' + Utils.GetSimpleScaleDateString(manifestDate);
	const shipmentId = 'Shipments-' + company + '-' + Utils.GetSimpleScaleDateString(manifestDate);
	const baseName = 'PER-CO-' + company + '_' + (apiResponse.manifest.company.vendorNumber === '856946' ? 'Azura_Fresh' : 'Theme_Group') + '_';
	if (apiResponse.xmlContent?.receiptContent) {
		//const receiptBase64 = btoa(apiResponse.receiptXmlContent);
		attachments.push({
			content: apiResponse.xmlContent.receiptContent,
			filename: `${baseName}${receiptId}.rcxml`,
			type: 'application/xml',
			disposition: 'attachment',
		});
	}
	if (apiResponse.xmlContent?.shipmentContent) {
		//const shipmentBase64 = btoa(apiResponse.shipmentXmlContent);
		attachments.push({
			content: apiResponse.xmlContent.shipmentContent,
			filename: `${baseName}${shipmentId}.shxml`,
			type: 'application/xml',
			disposition: 'attachment',
		});
	}
	return attachments;
}

export { sendResponseEmailFromMailgun, sendErrorEmailFromMailgun};
