
type DotnetOrder = {
	orderDate: string;
	storeNumber: string;
	storeName: string;
	poNumber: string;
	customerNumber: string;
	orderNumber: string;
	inventoryNumber: string;
	quantity: number;
	crateQuantity: number;
}

type DotnetManifest = {
	orders: DotnetOrder[];
	totalOrders: number;
	totalCrates: number;
	manifestDate: string;
	company: DotnetCompany;
	processingMessages?: string;
}

type DotnetCompany = {
	company: string;
	vendorNumber: string;
	vendorName: string;
	vendorReceiptPrefix: string;
	vendorSkuNumber: string;
}

type ApiResponse = {
	message: string;
	error: string
	originalFilename?: string;
	manifestDate?: string;
	totalOrders?: number;
	totalCrates?: number;
	company?: string;
	manifest?: DotnetManifest;
	xmlContent?: XmlContent;
	storeCorrections?: StoreCorrection[];
	correctionMessages?: ProcessingMessages;
};

type XmlContent = {
	receiptContent: string;
	shipmentContent: string;
}
type StoreCorrection = {
	orderNumber: string;
	storeName: string;
	originalStoreNumber: string;
	correctedStoreNumber: string;
	confidence: number;
	message?: string;
}

type EmailAttachment = {
	filename: string;
	contentType: string;
	fileType: string; // 'pdf', 'csv', 'xlsx', etc.
	data: ArrayBuffer | string; // Use ArrayBuffer for binary data
};

type EmailProcessingResponse = {
	message: string;
	status: number;
}

type ProcessingMessages = {
	warnings: string[] | null;
	errors: string[] | null;
}

type Attachment = {
	filename: string;
	type: string | null;
	content: ArrayBuffer | string; // Use ArrayBuffer for binary data
	disposition: string; // e.g., 'attachment', 'inline'
};

export { DotnetOrder, DotnetManifest, ApiResponse, EmailAttachment, EmailProcessingResponse, ProcessingMessages, Attachment };
