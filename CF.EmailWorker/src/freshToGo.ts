import { DateTime } from "luxon";
import { ProcessingMessages } from "./types";

export class FreshToGoManifestRecord  {
	_id: Number | null | undefined =  null;
	_processedDateTime: string | null | undefined  = null;
	OriginalFilename: string | null | undefined  = null;
	_manifestDate: DateTime | string | null | undefined  = null;
	_totalCrates: Number | null | undefined  = null;
	_status: Number | null | undefined   = null; // 0 = Not Processed, 1 = Processed, 2 = Error,
	_lastError: string | null | undefined   = null;
	_receiptId: string | null | undefined   = null;
	_totalShipments: Number | null | undefined   = null;
	_receiptXml: string | null | undefined = null;
	_shipmentXml: string | null | undefined   = null;
	_delivered: Boolean | null | undefined  = false;
	company: string = 'PER-CO-FTG'; // Default company name, can be overridden
	vendor: string = 'Azura Fresh';
	processingMessages?: ProcessingMessages;

	constructor(id?: number,  processedDateTime?: string , originalFilename?: string, totalCrates?: Number, status?: Number, lastError?: string, receiptId?: string, totalShipments?: Number, receiptXml?: string, shipmentXml?: string, delivered?: Boolean) {
		this._id = id || null;
		this._processedDateTime = processedDateTime || null;
		this.OriginalFilename = originalFilename || null;
		this._manifestDate = null;
		this._totalCrates = totalCrates || null;
		this._status = status || null;
		this._lastError = lastError || null;
		this._receiptId = receiptId || null;
		this._totalShipments = totalShipments || null;
		this._receiptXml = receiptXml || null;
		this._shipmentXml = shipmentXml || null;
		this._delivered = delivered || false;
	}

	get ManifestDate() : string
	{
		if(this._manifestDate !== null && this._manifestDate !== undefined)
		{
			if (typeof this._manifestDate === 'string')
			{
				return this._manifestDate;
			}
			else if (this._manifestDate instanceof DateTime)
			{
				return this._manifestDate.toFormat('yyyy-MM-dd');
			}
			else
			{
				return "Undefined Date";
			}
		}
		return "Undefined Date";
	}

	set ManifestDate(value: string | DateTime | null | undefined)
	{
		if (value instanceof DateTime) {
			this._manifestDate = value;
		} else if (typeof value === 'string') {
			this._manifestDate = DateTime.fromISO(value, { zone: 'Australia/Perth' });
		} else {
			this._manifestDate = null;
		}
	}

};

export type FreshToGoReceiptRecord = {
	ReceiptId: string | null | undefined,
	ReceiptDate: string | Date | null | undefined,
	TotalCrates: Number | null | undefined,
}

export type FreshToGoShipmentRecord = {
	ShipmentId: string | null | undefined,
	ShipmentDate: string | Date | null | undefined,
	TotalCrates: Number | null | undefined,
};
