import { from } from "form-data";
import { FreshToGoManifestRecord } from "./freshToGo";
import { DateTime } from "luxon";
import { Utils }  from "./utils";
import { ProcessingMessages, Attachment } from "./types";
import { cfLog } from "./deepConsoleLog";

class BaseEmail {
	from: string = 'Fresh To Go - Scale XML Processor for Receipts and Shipments <noreply@ftg.vectorpixel.net>';
	to: string[] = ['bryce@vectorpixel.net'];
	subject: string | null;
	text: string  | null;
	html: string | null;
	_attachments: Attachment[] | null = null;

	originalFilename: string = 'ftgmanifest.pdf';
	manifest: FreshToGoManifestRecord | null;
	processingMessages?: ProcessingMessages;

	constructor()
	{
		this.subject = null;
		this.text = null;
		this.html = null;
		this._attachments = null;
		this.manifest = null;
	}

	get attachments(): Attachment[] | null
	{
		return this.attachments;
	}

	set attachments(attachments: Attachment[])
	{
		if (attachments.length > 0)
		{
			this._attachments = attachments;
		}
		else
		{
			this._attachments = null;
		}
	}

	addAttachment(filename: string, contentType: string, data: ArrayBuffer | string, disposition: string = 'attachment')
	{
		if (this._attachments === null)
		{
			this._attachments = [];
		}
		this._attachments.push({ filename, type: contentType, content: data, disposition: disposition });

	}

	getProcessingMessagesFromManifest(): ProcessingMessages | null
	{
		if (this.manifest)
		{
			return this.manifest.processingMessages || null;
		}
		return null;
	}

	createProcessingMessagesSummary(): string
	{
		const messages = this.getProcessingMessagesFromManifest();

		if (messages !== null)
		{
			cfLog('emailTemplates.ts', 'Processing messages found', messages);
			let warningsSummary = '';
			let errorsSummary = '';

			if(messages.warnings !== null && messages.warnings !== undefined)
			{
				cfLog('emailTemplates.ts', `${messages.warnings.length} warning message/s found`);
				for(const warn of messages.warnings)
				{
					warningsSummary += `<li>${warn.replace(/⚠/g, "⚠️")}</li>`;
				}
			}

			if(messages.errors !== null && messages.errors !== undefined)
			{
				cfLog('emailTemplates.ts', `${messages.errors.length} error message/s found`);
				for(const err of messages.errors)
				{
					errorsSummary += `<li>${err.replace(/✗/g, "❌")}</li>`;
				}
			}
			const summary = `
				<div style="background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0;">
					<h3 style="margin-top: 0;">Processing Errors/Warnings:</h3>
					<h4 style="margin-top: 0;">⚠️ Warnings:</h4>
					<ul style="list-style-type: none; padding-left: 0;">
						${warningsSummary}
					</ul>
					<hr />
					<h4 style="margin-top: 0;">❌ Errors:</h4>
					<ul style="list-style-type: none; padding-left: 0;">
						${errorsSummary}
					</ul>
				</div>
			`;
			return (warningsSummary || errorsSummary) ? summary : '';
		}

		return '';
	}
}

export class ResponseEmail extends BaseEmail {

	constructor()
	{
		super();
	}

	init(to: string[], originalFilename: string, manifest: FreshToGoManifestRecord)
	{
		this.to = [this.to, ...to].flat();
		this.originalFilename = originalFilename;
		this.manifest = manifest;

		this.subject = `Processed Manifest For Scale - ${originalFilename} - Complete @ ${Utils.CurrentDateTimeAWSTShort}`;

		this.html =
		`
        <div style="font-family: Arial, sans-serif; max-width: 1000px; margin: 0 auto;">
		<h2 style="color: #28a745;">Manifest Processing Complete! ✅</h2>

		<p>The provided Manifest has been successfully processed and converted into the required formats for Scale Interfacing.</p>
		<h3 style="margin-top: 0;">Manifest Date: ${Utils.convertDateToAWSTandFormat(this.manifest.ManifestDate)}</h3>

		<div style="background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0;">
            <h3 style="margin-top: 0;">Processing Details:</h3>
            <ul style="list-style-type: none; padding-left: 0;">
				<li>📄 <strong>Original File:</strong> ${this.originalFilename}</li>
				<li>📅 <strong>Processed At:</strong> ${Utils.CurrentDateTimeAWSTShort}</li>
				<li>📦 <strong>Detected Vendor:</strong> ${this.manifest.vendor}</li>
            </ul>
		</div>

		${this.createProcessingMessagesSummary()}

		<div style="background-color: #e9ecef; padding: 15px; border-radius: 5px; margin: 20px 0;">
            <h3 style="margin-top: 0;">Attached Files:</h3>
            <ol>
				<li><strong>${this.manifest.company}_${this.manifest.vendor}_Receipt-${this.manifest.vendor === 'Azura_Fresh' ? 'CAF' : 'CTG'}-${Utils.GetSimpleScaleDateString(this.manifest.ManifestDate)}.rcxml</strong> - Manhattan Scale Receipt RCXML format</li>
				<li><strong>${this.manifest.company}_${this.manifest.vendor}_Shipments-${this.manifest.vendor === 'Azura_Fresh' ? 'CAF' : 'CTG'}-${Utils.GetSimpleScaleDateString(this.manifest.ManifestDate)}.shxml</strong> - Manhattan Scale Shipment SHXML format</li>
            </ol>
		</div>

        </div>
	`;
	}
}

export class AcknowledgementEmail extends BaseEmail {

	constructor()
	{
		super();
	}

	init(to: string[], originalFilename: string, manifest: FreshToGoManifestRecord)
	{
		this.to = [this.to, ...to].flat();
		this.originalFilename = originalFilename;
		this.manifest = manifest;

		this.subject = `Acknowledgement of Manifest For Scale - ${this.originalFilename} - ${this.manifest.processingMessages !== undefined ? 'With Issues Detected' : 'No Issues Detected'}`;
		this.html = `
        <div style="font-family: Arial, sans-serif; max-width: 1000px; margin: 0 auto;">
		<h2 style="color: #28a745;">Manifest Processing Acknowledgement! ✅</h2>

		<p>The provided Manifest has been successfully received and processed into the required formats for Scale Interfacing.</p>
		<p>The manifest file <strong>${this.originalFilename}</strong> was detected to have the requirement date <strong>${Utils.convertDateToAWSTandFormat(this.manifest.ManifestDate)}</strong>.</p>
		<p>Scale interface files will automatically be delivered the date required</p>
		<h3 style="margin-top: 0;">Interface Files Delivery Date: </h3>
		<p style="font-weight: bold; color: #007bff;">${Utils.getEmailDeliveryDateTime(this.manifest.ManifestDate)} AWST</p>

		<p>If the delivery date is in the past, the files will be sent immediately.</p>

		<div style="background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0;">
            <h3 style="margin-top: 0;">Processing Details:</h3>
            <ul style="list-style-type: none; padding-left: 0;">
				<li>📄 <strong>Original File:</strong> ${this.originalFilename}</li>
				<li>📅 <strong>Processed At:</strong> ${Utils.CurrentDateTimeAWSTShort}</li>
				<li>📦 <strong>Detected Vendor:</strong> ${this.manifest.vendor}</li>
			</ul>
		</div>

		${this.createProcessingMessagesSummary()}

		<div style="background-color: #e9ecef; padding: 15px; border-radius: 5px; margin: 20px 0;">
            <h3 style="margin-top: 0;">Files to be Delivered:</h3>
            <ol>
				<li><strong>${this.manifest.company}_${this.manifest.vendor}_Receipt-${this.manifest.vendor === 'Azura_Fresh' ? 'CAF' : 'CTG'}-${Utils.GetSimpleScaleDateString(this.manifest.ManifestDate)}.rcxml</strong> - Manhattan Scale Receipt RCXML format</li>
				<li><strong>${this.manifest.company}_${this.manifest.vendor}_Shipments-${this.manifest.vendor === 'Azura_Fresh' ? 'CAF' : 'CTG'}-${Utils.GetSimpleScaleDateString(this.manifest.ManifestDate)}.shxml</strong> - Manhattan Scale Shipment SHXML format</li>
            </ol>
		</div>

        </div>
	`;
	}
}


export class ErrorEmail extends BaseEmail {

	constructor()
	{
		super();
	}

 init(to: string[], originalFilename: string, errorMessage: string)
 {
  this.to = [this.to, ...to].flat();
  this.originalFilename = originalFilename;

  this.subject = `Error Processing Manifest For Scale - ${this.originalFilename} - Error @ ${Utils.CurrentDateTimeAWSTShort}`;
  this.html = `
	<div style="font-family: Arial, sans-serif; max-width: 1000px; margin: 0 auto;">
	<h2 style="color: #dc3545;">Error Processing Manifest! ❌</h2>

	<p>There was an error processing the provided Manifest.</p>

	<div style="background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0;">
		<h3 style="margin-top: 0;">Error Details:</h3>
		<p>${errorMessage}</p>
	</div>

	<div style="background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0;">
		<h3 style="margin-top: 0;">Processing Details:</h3>
		<ul style="list-style-type: none; padding-left: 0;">
			<li>📄 <strong>Original File:</strong> ${this.originalFilename}</li>
			<li>📅 <strong>Processed At:</strong> ${Utils.CurrentDateTimeAWSTShort}</li>
		</ul>
	</div>

	${this.createProcessingMessagesSummary()}

		</div>
	`;
	}
}
