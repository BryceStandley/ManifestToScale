import type { FreshToGoManifestRecord } from './freshToGo';
import { cfLog } from './deepConsoleLog';

export async function recordManifestToDatabase(env, manifest: FreshToGoManifestRecord) {
	try {
		const db = env.DB;
		if (!db) {
			throw new Error('Database connection is not available');
		}

		const query = `INSERT INTO processed_manifests (Id, ProcessedDateTime, OriginalFilename, ManifestDate, TotalCrates, Status, LastError, ReceiptId, TotalShipments, ReceiptXml, ShipmentXml, Delivered)
			VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
			ON CONFLICT (Id) DO UPDATE
			SET ProcessedDateTime = EXCLUDED.ProcessedDateTime,
				OriginalFilename = EXCLUDED.OriginalFilename,
				ManifestDate = EXCLUDED.ManifestDate,
				TotalCrates = EXCLUDED.TotalCrates,
				Status = EXCLUDED.Status,
				LastError = EXCLUDED.LastError,
				ReceiptId = EXCLUDED.ReceiptId,
				TotalShipments = EXCLUDED.TotalShipments,
				ReceiptXml = EXCLUDED.ReceiptXml,
				ShipmentXml = EXCLUDED.ShipmentXml,
				Delivered = EXCLUDED.Delivered,
				Vendor = EXCLUDED.Vendor;`;

		const result  = await env.DB.prepare(query).bind(
			manifest._id,
			manifest._processedDateTime,
			manifest.OriginalFilename,
			manifest.ManifestDate,
			manifest._totalCrates,
			manifest._status,
			manifest._lastError,
			manifest._receiptId,
			manifest._totalShipments,
			manifest._receiptXml,
			manifest._shipmentXml,
			manifest._delivered ? 1 : 0,
			manifest.vendor
		).all();
		if (result.success) {
			cfLog('database.ts',`Manifest ${manifest._id} recorded successfully.`);
			return {message: `Manifest ${manifest._id} recorded successfully.`, status: 200};
		} else {
			cfLog('database.ts',`Failed to record manifest ${manifest._id}`, result);
			return {message: `Failed to record manifest ${manifest._id}: ${result}`, status: 500};
		}

	} catch (error) {
		cfLog('database.ts','Error recording manifest to database:', error);
		return {message: `Error recording manifest: ${error.message}`, status: 500};
	}
}

export async function checkIfManifestHasPreviouslyProcessedSuccessfully(env, manifest: FreshToGoManifestRecord) {
	try {
		const db = env.DB;
		if (!db) {
			throw new Error('Database connection is not available');
		}

		const query = `SELECT * FROM processed_manifests WHERE ManifestDate = ? AND Status = 1 AND Vendor = ?;`; // Status 1 means processed successfully
		const result  = await env.DB.prepare(query).bind(manifest.ManifestDate, manifest.vendor).all();

		if(result.success && result.results.length > 0) {
			cfLog('database.ts',`Manifest date ${manifest.ManifestDate} for Vendor ${manifest.vendor} has been previously processed successfully.`);
			return {message: `Manifest date for Vendor ${manifest.vendor} has a processed db record... Skipping`, status: 500 }// Manifest has been processed successfully before
		}
		else
		{
			cfLog('database.ts',`Manifest date ${manifest.ManifestDate} for Vendor ${manifest.vendor} has not been processed successfully before.`);
			return {message: `Manifest date for Vendor ${manifest.vendor} has not been processed db record... Continuing`, status: 200 } // Manifest has not been processed successfully before
		}

	} catch (error) {
		cfLog('database.ts','Error querying the database for manifest date:', error);
		return {message: `Error querying the database for manifest date: ${error.message}`, status: 500};
	}
}
